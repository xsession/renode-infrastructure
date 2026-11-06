//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
// Pic12Trace — built-in signal capture, statistics and time-domain
// visualization for PIC12/PIC16 (classic mid-range) cores.
//
// Why this exists
// ---------------
// Renode has no generic "oscilloscope/statistics" facility for custom
// cores: the web visualizer only draws supported peripheral models. This
// engine closes that gap for PIC12-class machines entirely inside Renode:
//
//   * virtual-time sampling of core state (PC, W, SFRs, GPR, WDT age)
//     via a machine managed thread — no host polling, deterministic
//   * reset-event capture (machine.MachineReset), incl. WDT resets
//   * loop detection + per-WDT-period statistics + register histograms
//   * CSV/JSON dump and a self-contained PNG time plot (System.Drawing)
//
// Usage from a Renode script (python monitor command):
//
//   python #
//   from Antmicro.Renode.Peripherals.CPU import PIC12
//   core = monitor.Machine.GetPeripheralsOfType(type(PIC12)).First()
//   core.Trace.Start(2000, 1, 20)
//   #
//   emulation RunFor "5.12"
//   python #
//   core.Trace.Stop(); core.Trace.PrintStats()
//   core.Trace.WriteCsv("out/trace"); core.Trace.WritePng("out/viz.png")
//   #
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;

using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.PIC12;
using Antmicro.Renode.Time;

using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.CPU
{
    public sealed class Pic12TraceSample
    {
        public double TUs;
        public uint PC;
        public uint W;
        public byte GPIO;
        public byte GPR0x08;
        public byte PCON;
        public uint WdtAgeMs;

        public override string ToString()
        {
            return string.Format("t={0:F1}us pc=0x{1:X4} W=0x{2:X2} GPIO=0x{3:X2} " +
                                 "GPR08=0x{4:X2} PCON=0x{5:X2} wdt={6}ms",
                                 TUs, PC, W, GPIO, GPR0x08, PCON, WdtAgeMs);
        }
    }

    public sealed class Pic12TraceReset
    {
        public long Number;
        public double TUs;
        public uint PC;
        public uint W;
        public byte PCON;

        public override string ToString()
        {
            return string.Format("reset #{0} at t={1:F1}us pc=0x{2:X4} W=0x{3:X2} PCON=0x{4:X2}",
                                  Number, TUs, PC, W, PCON);
        }
    }

    public sealed class Pic12TraceStats
    {
        public double WindowUs;
        public long DetailCount;
        public long CoarseCount;
        public long WdtResets;
        public double WdtPeriodUs;
        public int LoopLengthUs;
        public double LoopFreqKHz;
        public List<uint> LoopPCs = new List<uint>();
        public List<string> LoopInsns = new List<string>();
        public long CompleteLoops;
        public long LoopsStarted;
        public Dictionary<uint, long> WHistogram = new Dictionary<uint, long>();
        public double WHighDutyPct;
        public uint WHighValue;
        public long GpioTransitions;
        public Dictionary<string, long> InstructionMix = new Dictionary<string, long>();
        public List<string[]> PerPeriod = new List<string[]>();
    }

    public sealed class Pic12Trace
    {
        private readonly PIC12 cpu;
        private readonly IMachine machine;

        private PIC12_DataSpace dataSpace;
        private IManagedThread detailThread;
        private IManagedThread coarseThread;
        private double startUs;
        private uint detailWindowUs;

        public readonly List<Pic12TraceSample> Detail = new List<Pic12TraceSample>();
        public readonly List<Pic12TraceSample> Coarse = new List<Pic12TraceSample>();
        public readonly List<Pic12TraceReset> Resets = new List<Pic12TraceReset>();

        public bool IsRunning { get; private set; }
        public double StartUs { get; private set; }
        public double StopUs { get; private set; }

        public Pic12Trace(PIC12 cpu, IMachine machine)
        {
            this.cpu = cpu;
            this.machine = machine;
        }

        private double NowUs()
        {
            return machine.ElapsedVirtualTime.TimeElapsed.TotalMicroseconds;
        }

        private void ResolveDataSpace()
        {
            if(dataSpace == null)
            {
                dataSpace = machine.GetPeripheralsOfType<PIC12_DataSpace>().FirstOrDefault();
                if(dataSpace == null)
                {
                    cpu.Log(LogLevel.Error, "Pic12Trace: no PIC12_DataSpace device found on the machine");
                }
            }
        }

        private Pic12TraceSample SampleAt(double t)
        {
            return new Pic12TraceSample
            {
                TUs = t,
                PC = (uint)cpu.PC,
                W = (uint)cpu.GetRegister((int)PIC16Registers.W),
                GPIO = dataSpace != null ? dataSpace.ReadByte(0x05) : (byte)0,
                GPR0x08 = dataSpace != null ? dataSpace.ReadByte(0x08) : (byte)0,
                PCON = dataSpace != null ? dataSpace.ReadByte(0x8E) : (byte)0,
                WdtAgeMs = (uint)cpu.WdtAgeMs,
            };
        }

        // Called from the machine reset event (wired by PIC12).
        public void OnMachineReset(IMachine m)
        {
            if(startUs <= 0)
            {
                return; // machine-creation reset, before any trace
            }
            var t = NowUs();
            if(t < startUs)
            {
                return;
            }
            ResolveDataSpace();
            Resets.Add(new Pic12TraceReset
            {
                Number = Resets.Count + 1,
                TUs = t,
                PC = (uint)cpu.PC,
                W = (uint)cpu.GetRegister((int)PIC16Registers.W),
                PCON = dataSpace != null ? dataSpace.ReadByte(0x8E) : (byte)0,
            });
        }

        // Ask the data space to model the next machine reset as a WDT
        // reset (RAM retained, PCON = 0xF8) instead of a POR clear.
        public void PrepareWdtReset()
        {
            ResolveDataSpace();
            dataSpace?.MarkWdtResetComing();
        }

        public void Start(uint detailWindowUs = 2000, uint detailSampleUs = 1, uint coarseSampleUs = 20)
        {
            Stop();
            ResolveDataSpace();
            this.detailWindowUs = detailWindowUs;
            Detail.Clear();
            Coarse.Clear();
            startUs = NowUs();
            StartUs = startUs;
            StopUs = 0;

            detailThread = machine.ObtainManagedThread(
                () =>
                {
                    var t = NowUs();
                    if(t >= startUs + detailWindowUs)
                    {
                        detailThread.Stop();
                        return;
                    }
                    Detail.Add(SampleAt(t));
                },
                TimeInterval.FromMicroseconds(detailSampleUs),
                "pic12-trace-detail", cpu);
            coarseThread = machine.ObtainManagedThread(
                () => Coarse.Add(SampleAt(NowUs())),
                TimeInterval.FromMicroseconds(coarseSampleUs),
                "pic12-trace-coarse", cpu);
            detailThread.Start();
            coarseThread.Start();
            IsRunning = true;
            cpu.Log(LogLevel.Info,
                "Pic12Trace: started (detail 1 sample/{0}us for {1}us, then 1 sample/{2}us)",
                detailSampleUs, detailWindowUs, coarseSampleUs);
        }

        public void Stop()
        {
            if(!IsRunning)
            {
                return;
            }
            StopUs = NowUs();
            try
            {
                detailThread?.Stop();
                coarseThread?.Stop();
                detailThread?.Dispose();
                coarseThread?.Dispose();
            }
            catch
            {
                // thread may have self-stopped (detail window expiry)
            }
            detailThread = null;
            coarseThread = null;
            IsRunning = false;
            cpu.Log(LogLevel.Info,
                "Pic12Trace: stopped after {0:F1}us — {1} detail + {2} coarse samples, {3} resets",
                StopUs - StartUs, Detail.Count, Coarse.Count,
                Resets.Count(r => r.TUs >= StartUs));
        }

        private ushort ReadProgramWord(ulong address)
        {
            var bytes = ((SystemBus)machine.SystemBus).ReadBytes(address, 2, onlyMemory: true, context: cpu);
            if(bytes == null || bytes.Length < 2)
            {
                return 0x3FFF;
            }
            return (ushort)(bytes[0] | (bytes[1] << 8));
        }

        // ------------------------------------------------------------------
        // 14-bit classic mid-range disassembler (DS41190G opcode table,
        // same table verified in FW-22-43-00_RE/tools/pic12f629_disasm.py)
        // ------------------------------------------------------------------
        private struct OpDef
        {
            public readonly uint Mask;
            public readonly uint Op;
            public readonly string Mn;
            public OpDef(uint mask, uint op, string mn)
            {
                Mask = mask;
                Op = op;
                Mn = mn;
            }
        }

        private static readonly OpDef[] Opcodes =
        {
            new OpDef(0x3F80, 0x0080, "MOVWF"), new OpDef(0x3F80, 0x0180, "CLRF"),
            new OpDef(0x3F00, 0x0800, "MOVF"), new OpDef(0x3F00, 0x0300, "DECF"),
            new OpDef(0x3F00, 0x0A00, "INCF"), new OpDef(0x3F00, 0x0700, "ADDWF"),
            new OpDef(0x3F00, 0x0200, "SUBWF"), new OpDef(0x3F00, 0x0500, "ANDWF"),
            new OpDef(0x3F00, 0x0400, "IORWF"), new OpDef(0x3F00, 0x0600, "XORWF"),
            new OpDef(0x3F00, 0x0900, "COMF"), new OpDef(0x3F00, 0x0E00, "SWAPF"),
            new OpDef(0x3F00, 0x0C00, "RRF"), new OpDef(0x3F00, 0x0D00, "RLF"),
            new OpDef(0x3F00, 0x0B00, "DECFSZ"), new OpDef(0x3F00, 0x0F00, "INCFSZ"),
            new OpDef(0x3F00, 0x3000, "MOVLW"), new OpDef(0x3F00, 0x3400, "RETLW"),
            new OpDef(0x3F00, 0x3900, "ANDLW"), new OpDef(0x3F00, 0x3800, "IORLW"),
            new OpDef(0x3F00, 0x3A00, "XORLW"), new OpDef(0x3F00, 0x3E00, "ADDLW"),
            new OpDef(0x3F00, 0x3C00, "SUBLW"),
            new OpDef(0x3C00, 0x1400, "BSF"), new OpDef(0x3C00, 0x1000, "BCF"),
            new OpDef(0x3C00, 0x1800, "BTFSC"), new OpDef(0x3C00, 0x1C00, "BTFSS"),
            new OpDef(0x3800, 0x2800, "GOTO"), new OpDef(0x3800, 0x2000, "CALL"),
            new OpDef(0xFFFF, 0x0008, "RETURN"), new OpDef(0xFFFF, 0x0009, "RETFIE"),
            new OpDef(0xFFFF, 0x0064, "CLRWDT"), new OpDef(0xFFFF, 0x0063, "SLEEP"),
            new OpDef(0xFFFF, 0x0000, "NOP"),
        };

        private static readonly Dictionary<int, string> FregNames = new Dictionary<int, string>
        {
            { 0x00, "INDF" }, { 0x01, "TMR0" }, { 0x02, "PCL" },
            { 0x03, "STATUS" }, { 0x04, "FSR" }, { 0x05, "GPIO" },
            { 0x0A, "PCLATH" }, { 0x0B, "INTCON" }, { 0x0C, "PIR1" },
            { 0x0E, "TMR1L" }, { 0x0F, "TMR1H" }, { 0x10, "T1CON" },
            { 0x19, "CMCON" }, { 0x81, "OPTION_REG" }, { 0x85, "TRISIO" },
            { 0x8C, "PIE1" }, { 0x8E, "PCON" }, { 0x90, "OSCCAL" },
            { 0x95, "WPU" },
        };

        private static string FregName(int f7)
        {
            int a = ((f7 >> 1) & 0x3F) | ((f7 & 1) << 7);
            FregNames.TryGetValue(a, out var name);
            return name != null ? name : "0x" + a.ToString("X2");
        }

        public static string Decode(ushort w)
        {
            foreach(var entry in Opcodes)
            {
                if((w & entry.Mask) == entry.Op)
                {
                    var mn = entry.Mn;
                    if(entry.Mask == 0x3800)
                    {
                        return mn + " 0x" + (w & 0x07FF).ToString("X3");
                    }
                    if(entry.Mask == 0xFFFF)
                    {
                        return mn;
                    }
                    if(entry.Mask == 0x3F80)
                    {
                        return mn + " " + FregName(w & 0x7F);
                    }
                    if(entry.Mask == 0x3C00)
                    {
                        return mn + " " + FregName(w & 0x7F) + ", b" + ((w >> 7) & 7);
                    }
                    int f7 = w & 0x7F;
                    if(entry.Op == 0x0B00 || entry.Op == 0x0F00)
                    {
                        return mn + " " + FregName(f7) + ", f";
                    }
                    if(entry.Op == 0x0800 || entry.Op == 0x0300 || entry.Op == 0x0A00
                        || entry.Op == 0x0700 || entry.Op == 0x0200 || entry.Op == 0x0500
                        || entry.Op == 0x0400 || entry.Op == 0x0600 || entry.Op == 0x0900
                        || entry.Op == 0x0E00 || entry.Op == 0x0C00 || entry.Op == 0x0D00)
                    {
                        return mn + " " + FregName(f7) + ", " + ((w & 0x80) != 0 ? "f" : "w");
                    }
                    return mn + " 0x" + (w & 0xFF).ToString("X2");
                }
            }
            return "???";
        }

        // ------------------------------------------------------------------
        // statistics
        // ------------------------------------------------------------------
        public Pic12TraceStats ComputeStats()
        {
            var s = new Pic12TraceStats();
            s.WindowUs = StopUs - StartUs;
            s.DetailCount = Detail.Count;
            s.CoarseCount = Coarse.Count;

            var windowResets = Resets.Where(r => r.TUs >= StartUs && r.TUs <= StopUs).ToList();
            s.WdtResets = windowResets.Count;
            if(windowResets.Count >= 2)
            {
                var deltas = windowResets.Zip(windowResets.Skip(1),
                                              (a, b) => b.TUs - a.TUs).OrderBy(d => d).ToList();
                s.WdtPeriodUs = deltas[deltas.Count / 2];
            }
            else if(windowResets.Count == 1)
            {
                s.WdtPeriodUs = windowResets[0].TUs - StartUs;
            }

            // --- loop detection from the 1us detail window -------------
            var seq = Detail.Select(d => d.PC).ToArray();
            if(seq.Length > 4)
            {
                var p0 = seq[0];
                for(int i = 1; i < seq.Length - 1; i++)
                {
                    if(seq[i] != p0)
                    {
                        continue;
                    }
                    int candidate = i;
                    bool ok = i + candidate < seq.Length;
                    if(ok)
                    {
                        for(int j = 0; j < candidate && i + j < seq.Length - 1; j++)
                        {
                            if(seq[i + j + 1] != seq[j + 1] && seq[i + j + 1] != seq[(j + 1) % candidate])
                            {
                                // allow the very first sample of the
                                // next period to differ only if it is
                                // not the loop start
                                if(seq[i + j + 1] != seq[j])
                                {
                                    ok = false;
                                    break;
                                }
                            }
                        }
                    }
                    if(ok || candidate >= 2)
                    {
                        s.LoopLengthUs = candidate;
                        for(int j = 0; j < candidate; j++)
                        {
                            var pc = seq[j];
                            s.LoopPCs.Add(pc);
                            s.LoopInsns.Add(Decode(ReadProgramWord(pc)));
                        }
                        break;
                    }
                }
            }
            if(s.LoopLengthUs > 0)
            {
                s.LoopFreqKHz = 1000.0 / s.LoopLengthUs;
                s.CompleteLoops = (long)(s.WindowUs / s.LoopLengthUs);
                s.LoopsStarted = s.CompleteLoops + 1;
                var mix = new Dictionary<string, long>();
                foreach(var insn in s.LoopInsns)
                {
                    var mn = insn.Split(' ')[0];
                    mix[mn] = mix.TryGetValue(mn, out var c) ? c + 1 : 1;
                }
                foreach(var kv in mix.OrderByDescending(kv => kv.Value))
                {
                    s.InstructionMix[kv.Key] = kv.Value;
                }
            }

            // --- W histogram over all samples ---------------------------
            var wHist = new Dictionary<uint, long>();
            long gpioPrev = -1;
            foreach(var sample in Detail.Concat(Coarse))
            {
                wHist[sample.W] = wHist.TryGetValue(sample.W, out var c) ? c + 1 : 1;
                if(gpioPrev >= 0 && sample.GPIO != gpioPrev)
                {
                    s.GpioTransitions++;
                }
                gpioPrev = sample.GPIO;
            }
            foreach(var kv in wHist.OrderByDescending(kv => kv.Value))
            {
                s.WHistogram[kv.Key] = kv.Value;
            }
            var wTotal = (long)wHist.Values.Sum();
            var wHigh = wHist.Where(kv => kv.Key != 0).OrderByDescending(kv => kv.Value).FirstOrDefault();
            if(wTotal > 0 && wHigh.Value != default)
            {
                s.WHighValue = wHigh.Key;
                s.WHighDutyPct = 100.0 * wHigh.Value / wTotal;
            }

            // --- per WDT period -----------------------------------------
            var boundaries = windowResets.Select(r => r.TUs).ToList();
            boundaries.Insert(0, StartUs);
            boundaries.Add(StopUs);
            for(int i = 0; i < boundaries.Count - 1; i++)
            {
                double from = boundaries[i], to = boundaries[i + 1];
                double dur = to - from;
                long loops = s.LoopLengthUs > 0 ? (long)(dur / s.LoopLengthUs) : 0;
                s.PerPeriod.Add(new[]
                {
                    (i + 1).ToString(CultureInfo.InvariantCulture),
                    (to / 1000.0).ToString("F1", CultureInfo.InvariantCulture),
                    (loops + 1).ToString(CultureInfo.InvariantCulture),
                    dur.ToString("F0", CultureInfo.InvariantCulture),
                    windowResets.Count(r => Math.Abs(r.TUs - to) < 1.0) > 0
                        ? "0x" + windowResets.First(r => Math.Abs(r.TUs - to) < 1.0).PC.ToString("X4")
                        : "-",
                    windowResets.Count(r => Math.Abs(r.TUs - to) < 1.0) > 0
                        ? "0x" + windowResets.First(r => Math.Abs(r.TUs - to) < 1.0).W.ToString("X2")
                        : "-",
                });
            }
            return s;
        }

        // ------------------------------------------------------------------
        // output
        // ------------------------------------------------------------------
        public void WriteCsv(string dir)
        {
            Directory.CreateDirectory(dir);
            var detailPath = Path.Combine(dir, "trace_detail.csv");
            using(var sw = new StreamWriter(detailPath))
            {
                sw.WriteLine("t_us,pc,word,insn,w,gpio,gpr0x08,pcon,wdt_age_ms");
                foreach(var d in Detail)
                {
                    sw.WriteLine(string.Format("{0:F0},0x{1:X4},0x{2:X4},{3},0x{4:X2},0x{5:X2},0x{6:X2},0x{7:X2},{8}",
                        d.TUs, d.PC, ReadProgramWord(d.PC), Decode(ReadProgramWord(d.PC)),
                        d.W, d.GPIO, d.GPR0x08, d.PCON, d.WdtAgeMs));
                }
            }
            var coarsePath = Path.Combine(dir, "trace_coarse.csv");
            using(var sw = new StreamWriter(coarsePath))
            {
                sw.WriteLine("t_us,pc,w,gpio,gpr0x08,pcon,wdt_age_ms");
                foreach(var c in Coarse)
                {
                    sw.WriteLine(string.Format("{0:F0},0x{1:X4},0x{2:X2},0x{3:X2},0x{4:X2},0x{5:X2},{6}",
                        c.TUs, c.PC, c.W, c.GPIO, c.GPR0x08, c.PCON, c.WdtAgeMs));
                }
            }
            var resetPath = Path.Combine(dir, "resets.csv");
            using(var sw = new StreamWriter(resetPath))
            {
                sw.WriteLine("n,t_us,pc,w,pcon");
                foreach(var r in Resets)
                {
                    sw.WriteLine(string.Format("{0},{1:F0},0x{2:X4},0x{3:X2},0x{4:X2}",
                        r.Number, r.TUs, r.PC, r.W, r.PCON));
                }
            }
            var stats = ComputeStats();
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine(string.Format("  \"window_us\": {0:F0},", stats.WindowUs));
            json.AppendLine(string.Format("  \"wdt_resets\": {0},", stats.WdtResets));
            json.AppendLine(string.Format("  \"wdt_period_us\": {0:F0},", stats.WdtPeriodUs));
            json.AppendLine(string.Format("  \"loop_length_us\": {0},", stats.LoopLengthUs));
            json.AppendLine(string.Format("  \"loop_freq_khz\": {0:F3},", stats.LoopFreqKHz));
            json.AppendLine(string.Format("  \"complete_loops\": {0},", stats.CompleteLoops));
            json.AppendLine(string.Format("  \"loops_started\": {0},", stats.LoopsStarted));
            json.AppendLine(string.Format("  \"w_high_duty_pct\": {0:F2},", stats.WHighDutyPct));
            json.AppendLine(string.Format("  \"gpio_transitions\": {0},", stats.GpioTransitions));
            json.Append("  \"instruction_mix\": { ");
            json.Append(string.Join(", ", stats.InstructionMix
                .Select(kv => "\"" + kv.Key + "\": " + kv.Value)));
            json.AppendLine(" },");
            json.Append("  \"w_histogram\": { ");
            json.Append(string.Join(", ", stats.WHistogram
                .Select(kv => "0x" + kv.Key.ToString("X2") + ": " + kv.Value)));
            json.AppendLine(" },");
            json.Append("  \"loop\": [ ");
            json.Append(string.Join(", ", stats.LoopInsns.Select(i => "\"" + i + "\"")));
            json.AppendLine(" ]");
            json.Append("}");
            File.WriteAllText(Path.Combine(dir, "stats.json"), json.ToString());
            cpu.Log(LogLevel.Info, "Pic12Trace: wrote {0}", dir);
        }

        public void PrintStats()
        {
            var s = ComputeStats();
            cpu.Log(LogLevel.Info, "=== Pic12Trace stats ===");
            cpu.Log(LogLevel.Info, "  window: {0:F1} ms virtual", s.WindowUs / 1000.0);
            cpu.Log(LogLevel.Info, "  WDT resets: {0} (period {1:F1} ms)", s.WdtResets, s.WdtPeriodUs / 1000.0);
            if(s.LoopLengthUs > 0)
            {
                cpu.Log(LogLevel.Info, "  loop: {0} instructions = {1} us -> {2:F3} kHz; {3} complete loops",
                        s.LoopPCs.Count, s.LoopLengthUs, s.LoopFreqKHz, s.CompleteLoops);
                for(int i = 0; i < s.LoopPCs.Count; i++)
                {
                    cpu.Log(LogLevel.Info, "    {0,2}: 0x{1:X4}  {2}", i, s.LoopPCs[i], s.LoopInsns[i]);
                }
            }
            cpu.Log(LogLevel.Info, "  W = 0x{0:X2} duty: {1:F2} %", s.WHighValue, s.WHighDutyPct);
            cpu.Log(LogLevel.Info, "  GPIO transitions: {0}", s.GpioTransitions);
            cpu.Log(LogLevel.Info, "  instruction mix per loop: {0}",
                string.Join(", ", s.InstructionMix.Select(kv => kv.Key + "x" + kv.Value)));
        }

        // ------------------------------------------------------------------
        // PNG time plot (System.Drawing; Windows)
        // ------------------------------------------------------------------
        public bool WritePng(string path)
        {
            try
            {
                using(var bmp = new System.Drawing.Bitmap(1600, 1250, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using(var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                        g.Clear(System.Drawing.Color.White);
                        Render(g, bmp);
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                cpu.Log(LogLevel.Info, "Pic12Trace: wrote {0}", path);
                return true;
            }
            catch(Exception e)
            {
                cpu.Log(LogLevel.Warning,
                        "Pic12Trace: PNG rendering unavailable on this platform ({0}); use WriteCsv + external plotter",
                        e.Message);
                return false;
            }
        }

        private void Render(System.Drawing.Graphics g, System.Drawing.Bitmap bmp)
        {
            var stats = ComputeStats();
            var fTitle = new System.Drawing.Font("Segoe UI", 15f, System.Drawing.FontStyle.Bold);
            var fPanel = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
            var fAxis = new System.Drawing.Font("Segoe UI", 9f);
            var fSmall = new System.Drawing.Font("Segoe UI", 8f);
            var fTiny = new System.Drawing.Font("Segoe UI", 7f);

            var cGrid = System.Drawing.Color.FromArgb(0xD0, 0xD7, 0xDE);
            var cWdt = System.Drawing.Color.FromArgb(0xCF, 0x22, 0x2E);
            var cW = System.Drawing.Color.FromArgb(0xD2, 0x99, 0x22);
            var cPc = System.Drawing.Color.FromArgb(0x1F, 0x6F, 0xEB);
            var cGreen = System.Drawing.Color.FromArgb(0x3F, 0xB9, 0x50);
            var cPurple = System.Drawing.Color.FromArgb(0xA3, 0x71, 0xF7);

            g.DrawString("FW-22-43-00 on PIC12F629 — simulation: periodic signals & statistics over time (Renode)",
                         fTitle, System.Drawing.Brushes.Black, 40, 15);
            g.DrawString(string.Format("cycle-accurate tlib core · WDT {0:F1} ms period · simulated {1:F2} s virtual ({2} WDT resets)",
                                       stats.WdtPeriodUs / 1000.0, stats.WindowUs / 1e6, stats.WdtResets),
                         fAxis, new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x55, 0x55, 0x55)), 40, 48);

            void panel(RectangleF area, string title, string xLabel, string yLabel,
                       double xMin, double xMax, double yMin, double yMax,
                       int xTicks, int yTicks)
            {
                using(var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0x57, 0x60, 0x6A)))
                {
                    g.DrawRectangle(pen, area.X, area.Y, area.Width, area.Height);
                }
                g.DrawString(title, fPanel, System.Drawing.Brushes.Black, area.X + 6, area.Y - 22);
                g.DrawString(xLabel, fAxis, System.Drawing.Brushes.Black, area.X + area.Width / 2 - 40, area.Y + area.Height + 4);
                g.DrawString(yLabel, fAxis, System.Drawing.Brushes.Black, area.X - 70, area.Y + area.Height / 2);
                using(var gridPen = new System.Drawing.Pen(cGrid))
                {
                    for(int i = 1; i < xTicks; i++)
                    {
                        var x = area.X + area.Width * i / xTicks;
                        g.DrawLine(gridPen, x, area.Y, x, area.Y + area.Height);
                    }
                    for(int i = 1; i < yTicks; i++)
                    {
                        var y = area.Y + area.Height * i / yTicks;
                        g.DrawLine(gridPen, area.X, y, area.X + area.Width, y);
                    }
                }
            }

            RectangleF Map(RectangleF area, double x, double y,
                           double xMin, double xMax, double yMin, double yMax)
            {
                var px = area.X + area.Width * (x - xMin) / (xMax - xMin);
                var py = area.Y + area.Height * (1.0 - (y - yMin) / (yMax - yMin));
                return new RectangleF((float)px, (float)py, 0, 0);
            }

            void DrawStep(IEnumerable<Pic12TraceSample> samples, RectangleF area,
                          double xMin, double xMax, double yMin, double yMax,
                          System.Drawing.Color color, System.Func<Pic12TraceSample, double> value,
                          bool fill = false)
            {
                var points = samples.ToList();
                using(var pen = new System.Drawing.Pen(color, 1.4f))
                {
                    for(int i = 0; i + 1 < points.Count; i++)
                    {
                        var a = points[i];
                        var b = points[i + 1];
                        var qa = Map(area, a.TUs, value(a), xMin, xMax, yMin, yMax);
                        var qb = Map(area, b.TUs, value(b), xMin, xMax, yMin, yMax);
                        g.DrawLine(pen, qa.X, qa.Y, qb.X, qa.Y);
                        g.DrawLine(pen, qb.X, qa.Y, qb.X, qb.Y);
                    }
                    if(fill && points.Count > 1)
                    {
                        var first = Map(area, points[0].TUs, 0, xMin, xMax, yMin, yMax);
                        var last = Map(area, points[points.Count - 1].TUs, 0, xMin, xMax, yMin, yMax);
                        var fillPoints = new List<System.Drawing.PointF>();
                        for(int i = 0; i < points.Count; i++)
                        {
                            var a = points[i];
                            var b = i + 1 < points.Count ? points[i + 1] : a;
                            var pa = Map(area, a.TUs, value(a), xMin, xMax, yMin, yMax);
                            fillPoints.Add(new System.Drawing.PointF(pa.X, pa.Y));
                            var pb = Map(area, b.TUs, value(a), xMin, xMax, yMin, yMax);
                            fillPoints.Add(new System.Drawing.PointF(pb.X, pb.Y));
                        }
                        fillPoints.Add(new System.Drawing.PointF(last.X, first.Y));
                        using(var brush = new System.Drawing.SolidBrush(
                            System.Drawing.Color.FromArgb(30, color)))
                        {
                            g.FillPolygon(brush, fillPoints.ToArray());
                        }
                    }
                }
            }

            // ---- panel 1: WDT sawtooth (full window) --------------------
            var p1 = new RectangleF(40, 90, 1520, 330);
            var tEnd = StartUs + stats.WindowUs;
            panel(p1, "Watchdog timer countdown — sawtooth, machine reset at each peak",
                  "time (ms)", "WDT age (us)",
                  StartUs / 1000.0, tEnd / 1000.0, 0, 1.1e6, 8, 5);
            if(Coarse.Count > 1)
            {
                DrawStep(Coarse, p1, StartUs, tEnd, 0, 1.1e6, cWdt,
                         s => s.WdtAgeMs * 1000.0);
            }
            foreach(var r in Resets.Where(r => r.TUs >= StartUs && r.TUs <= tEnd))
            {
                var x = Map(p1, r.TUs, 0, StartUs, tEnd, 0, 1.1e6).X;
                using(var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0x8B, 0x94, 0x9E), 1f))
                {
                    g.DrawLine(pen, x, p1.Y, x, p1.Y + p1.Height);
                }
                g.DrawString("WDT RESET #" + r.Number, fSmall, System.Drawing.Brushes.Black,
                             x + 4, p1.Y + 8);
            }
            g.DrawString("WDT period = " + (stats.WdtPeriodUs / 1e6).ToString("F3") + " s",
                         fPanel, new System.Drawing.SolidBrush(cWdt), p1.X + p1.Width / 2 - 80, p1.Y + p1.Height / 2);

            // ---- panel 2: W periodic signal (first 2 ms) -----------------
            var p2 = new RectangleF(40, 480, 740, 280);
            panel(p2, "W register — periodic signal, first 2 ms (1 us sampling)",
                  "time (us)", "W", StartUs, StartUs + 2000, -0x08, 0x50, 10, 5);
            if(Detail.Count > 1)
            {
                DrawStep(Detail.Where(d => d.TUs <= StartUs + 2000), p2,
                         StartUs, StartUs + 2000, -0x08, 0x50, cW, s => s.W, fill: true);
            }
            g.DrawString(string.Format("period {0} us -> {1:F1} kHz, 0x{2:X2} high {3:F1} % duty",
                                       stats.LoopLengthUs, stats.LoopFreqKHz, stats.WHighValue, stats.WHighDutyPct),
                         fSmall, new System.Drawing.SolidBrush(cW), p2.X + 12, p2.Y + p2.Height - 18);

            // ---- panel 3: PC zoom (first 24 us) ---------------------------
            var p3 = new RectangleF(820, 480, 740, 280);
            panel(p3, "PC trace — one boot loop (1 us = 1 instruction)",
                  "time (us)", "PC", StartUs, StartUs + 24, -0x40, 0x04A0, 12, 5);
            if(Detail.Count > 2)
            {
                DrawStep(Detail.Where(d => d.TUs <= StartUs + 24), p3,
                         StartUs, StartUs + 24, -0x40, 0x04A0, cPc, s => s.PC);
                for(int i = 0; i + 1 < Detail.Count && Detail[i + 1].TUs <= StartUs + 24; i++)
                {
                    var d = Detail[i + 1];
                    var p = Map(p3, d.TUs, d.PC, StartUs, StartUs + 24, -0x40, 0x04A0);
                    g.DrawString("0x" + d.PC.ToString("X4"), fTiny,
                                 new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x57, 0x60, 0x6A)),
                                 p.X + 2, p.Y + 2);
                }
            }

            // ---- panel 4: instruction mix --------------------------------
            var p4 = new RectangleF(40, 810, 740, 260);
            panel(p4, "Instruction mix per loop (" + (stats.LoopLengthUs > 0 ? stats.LoopLengthUs + " total" : "n/a") + ")",
                  "", "", 0, 1, 0, 1, 1, 1);
            var mixItems = stats.InstructionMix.ToList();
            if(mixItems.Count > 0)
            {
                int maxCount = (int)mixItems.Max(kv => kv.Value);
                var barW = (float)(p4.Width / mixItems.Count);
                var barColors = new[] { cPc, cW, cPurple, cGreen, cWdt, System.Drawing.Color.FromArgb(0x82, 0x50, 0xDF) };
                for(int i = 0; i < mixItems.Count; i++)
                {
                    var h = (float)(p4.Height * 0.75 * mixItems[i].Value / maxCount);
                    var x = p4.X + 10 + i * barW + 4;
                    using(var brush = new System.Drawing.SolidBrush(barColors[i % barColors.Length]))
                    {
                        g.FillRectangle(brush, x, p4.Y + p4.Height - 18 - h, barW - 8, h);
                    }
                    g.DrawString(mixItems[i].Key, fSmall, System.Drawing.Brushes.Black, x, p4.Y + p4.Height - 16);
                    g.DrawString(mixItems[i].Value.ToString(), fSmall, System.Drawing.Brushes.Black,
                                 x + (barW - 8) / 2 - 4, p4.Y + p4.Height - 22 - h - 14);
                }
            }

            // ---- panel 5: per-period table --------------------------------
            var p5 = new RectangleF(820, 810, 740, 260);
            panel(p5, string.Format("Per WDT-period statistics ({0} resets)", stats.WdtResets),
                  "", "", 0, 1, 0, 1, 1, 1);
            string[] tableHeader = { "#", "t reset (ms)", "loops", "window (us)", "PC @ reset", "W @ reset" };
            int colX0 = 90;
            int[] colW = { 30, 100, 60, 120, 90, 90 };
            int tx = (int)p5.X + 10;
            int ty = (int)p5.Y + 12;
            g.DrawString(string.Join("   ", tableHeader.Select(h => h.PadRight(12))), fSmall, System.Drawing.Brushes.Black, tx, ty);
            ty += 16;
            using(var gridPen = new System.Drawing.Pen(cGrid))
            {
                g.DrawLine(gridPen, tx, ty - 4, tx + 620, ty - 4);
            }
            for(int i = 0; i < stats.PerPeriod.Count && ty < p5.Y + p5.Height - 8; i++)
            {
                var row = stats.PerPeriod[i];
                g.DrawString(string.Join("   ", row.Select(c => c.PadRight(12))), fSmall,
                             System.Drawing.Brushes.Black, tx, ty);
                ty += 15;
            }

            // ---- panel 6: key stats ----------------------------------------
            var p6 = new RectangleF(40, 1110, 1520, 120);
            using(var pen = new System.Drawing.Pen(cGrid))
            {
                g.DrawRectangle(pen, p6.X, p6.Y, p6.Width, p6.Height);
            }
            g.DrawString("Key statistics", fPanel, System.Drawing.Brushes.Black, p6.X + 8, p6.Y + 6);
            string[] head =
            {
                "boot loop: " + string.Join(" -> ", stats.LoopPCs.Select(pc => "0x" + pc.ToString("X4"))) + " (0x0000)",
                string.Format("loop frequency: {0:F3} kHz   |   loops: {1} complete / {2} started",
                               stats.LoopFreqKHz, stats.CompleteLoops, stats.LoopsStarted),
                string.Format("WDT period: {0:F3} s   |   resets: {1}   |   W=0x{2:X2} duty {3:F1} %   |   GPIO transitions: {4}",
                               stats.WdtPeriodUs / 1e6, stats.WdtResets, stats.WHighValue,
                               stats.WHighDutyPct, stats.GpioTransitions),
                "GPR 0x08 latched each loop, retained across WDT resets (RAM survives WDT reset, DS41190)",
            };
            int hy = (int)p6.Y + 28;
            foreach(var line in head)
            {
                g.DrawString(line, fSmall, System.Drawing.Brushes.Black, p6.X + 12, hy);
                hy += 15;
            }
        }
    }
}
