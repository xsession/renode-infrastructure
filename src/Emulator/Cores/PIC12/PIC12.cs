//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
// Microchip PIC12 (classic mid-range, 14-bit instruction set) CPU core.
//
// Reuses the "pic16" tlib translation backend: PIC12Fxxx and classic
// PIC16Fxxx share the same 14-bit opcode family (per DS41190 the PIC12
// simply lacks the extended mid-range instructions, which the backend
// raises EXCP_UNDEF for anyway).
//
// Adds the Watchdog Timer (WDT) as a virtual LimitTimer (1 tick = 1 ms):
//   - default period 1024 ms (POR OPTION_REG = 0xFF: PSA=1, PS2:PS0=111
//     -> 4.096 ms x 256 per DS41190),
//   - on timeout a machine-level reset is requested (WDT reset),
//   - the CLRWDT instruction reaches this core through the tlib callback
//     (tlib_wdt_clrwdt -> [Export] WdtClrwdt) which restarts the timer.
//
using System;

using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Timers;
using Antmicro.Renode.Utilities.Binding;

namespace Antmicro.Renode.Peripherals.CPU
{
    public class PIC12 : PIC16
    {
        public PIC12(string cpuType, IMachine machine, uint wdtPeriodMs = 1024)
            : base(cpuType, machine)
        {
            this.wdtPeriodMs = wdtPeriodMs;

            wdt = new LimitTimer(machine.ClockSource, 1000, this, "wdt",
                                 limit: wdtPeriodMs,
                                 enabled: true,
                                 eventEnabled: true);
            wdt.LimitReached += OnWdtLimitReached;

            Trace = new Pic12Trace(this, machine);
            machine.MachineReset += Trace.OnMachineReset;
        }

        public ulong WdtResetCount { get; private set; }

        /// <summary>
        /// Built-in signal capture / statistics / time-plot engine
        /// (see Pic12Trace.cs). Drive it from a script:
        /// core.Trace.Start(...); emulation RunFor "..."; core.Trace.Stop();
        /// core.Trace.WriteCsv("out"); core.Trace.WritePng("out/viz.png");
        /// </summary>
        public Pic12Trace Trace { get; }

        /// <summary>Watchdog age in ms since the last feed/reset (0..wdtPeriodMs).</summary>
        public ulong WdtAgeMs => wdtPeriodMs - wdt.Value;

        public override void Reset()
        {
            base.Reset();
            wdt.Reset();
        }

        [Export]
        private void WdtClrwdt()
        {
            wdt.ResetValue();
        }

        private void OnWdtLimitReached()
        {
            WdtResetCount++;
            // Model the upcoming machine reset as a WDT reset, not a POR:
            // RAM/SFRs are retained and PCON = 0xF8 (DS41190 Table 29-1).
            Trace.PrepareWdtReset();
            this.Log(LogLevel.Info,
                "Watchdog timer timeout #{0} (period {1} ms) - resetting machine",
                WdtResetCount, wdtPeriodMs);
            machine.RequestReset();
        }

        private readonly uint wdtPeriodMs;
        private readonly LimitTimer wdt;
    }
}
