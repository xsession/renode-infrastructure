//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
// PIC12F629 data space (SFR + GPR) device.
//
// The pic16 tlib backend models every file-register access that is not a
// core register (PCL, STATUS, FSR, INDF live in CPUState) as a byte
// load/store at PIC16_DATA_BASE + f (0x200000 + f). This device maps the
// flat 0x200000-0x20009F window and models the PIC12F629 special function
// registers (DS41190) plus the GPR banks:
//
//   Bank 0: 0x06-0x1F (GPR), 0x20-0x5F (GPR)
//   Bank 1: 0x88-0x9F (GPR)
//   SFRs:   0x01 TMR0, 0x05 GPIO, 0x0A PCLATH, 0x0B INTCON, 0x0C PIR1,
//           0x0E/0x0F TMR1L/H, 0x10 T1CON, 0x19 CMCON, 0x81 OPTION_REG,
//           0x85 TRISIO, 0x8C PIE1, 0x8E PCON, 0x90 OSCCAL, 0x95 WPU
//
// Notes:
//   - TMR0/TMR1 are static registers: no autonomous counting is modelled
//     (the firmware under test never reads their counters autonomously).
//   - GPIO pin I/O is latch-only (no input pin sourcing in this platform).
//   - 0x02/0x03/0x04 (PCL/STATUS/FSR) are core registers; writes through
//     the data space are rejected with a warning.
//
using System;

using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.PIC12
{
    public class PIC12_DataSpace : IBytePeripheral, IKnownSize
    {
        public PIC12_DataSpace(IMachine machine)
        {
            this.machine = machine;
            gpr = new byte[0xA0];
            Reset();
        }

        public byte ReadByte(long offset)
        {
            switch(offset)
            {
                case 0x00: return indf;
                case 0x01: return tmr0;
                case 0x02: return 0;  /* PCL — core register */
                case 0x03: return 0;  /* STATUS — core register */
                case 0x04: return 0;  /* FSR — core register */
                case 0x05: return gpioLatch;
                case 0x0A: return pclath;
                case 0x0B: return intcon;
                case 0x0C: return pir1;
                case 0x0E: return (byte)(tmr1 & 0xFF);
                case 0x0F: return (byte)(tmr1 >> 8);
                case 0x10: return t1con;
                case 0x19: return cmcon;
                case 0x81: return optionReg;
                case 0x85: return (byte)(trisio & 0x3F);
                case 0x8C: return pie1;
                case 0x8E: return pcon;
                case 0x90: return osccal;
                case 0x95: return (byte)(wpu & 0x3F);
            }
            if(IsGpr(offset)) return gpr[offset];
            this.Log(LogLevel.Warning, "PIC12F629: read of unmapped data space 0x{0:X}", offset);
            return 0;
        }

        public void WriteByte(long offset, byte value)
        {
            switch(offset)
            {
                case 0x00: indf = value; return;
                case 0x01: tmr0 = value; return;
                case 0x02:
                case 0x03:
                case 0x04:
                    this.Log(LogLevel.Warning,
                        "PIC12F629: write to core SFR 0x{0:X} via data space ignored", offset);
                    return;
                case 0x05: gpioLatch = (byte)(value & 0x3F); return;
                case 0x0A: pclath = (byte)(value & 0x1F); return;
                case 0x0B: intcon = value; return;
                case 0x0C: pir1 = value; return;
                case 0x0E: tmr1 = (ushort)((tmr1 & 0xFF00) | value); return;
                case 0x0F: tmr1 = (ushort)((tmr1 & 0x00FF) | (ushort)(value << 8)); return;
                case 0x10: t1con = value; return;
                case 0x19: cmcon = value; return;
                case 0x81: optionReg = value; return;
                case 0x85: trisio = (byte)(value & 0x3F); return;
                case 0x8C: pie1 = value; return;
                case 0x8E: pcon = (byte)(value | 0x0C); return;  /* POR/PF stay set */
                case 0x90: osccal = value; return;
                case 0x95: wpu = (byte)(value & 0x3F); return;
            }
            if(IsGpr(offset))
            {
                gpr[offset] = value;
                return;
            }
            this.Log(LogLevel.Warning,
                "PIC12F629: write of unmapped data space 0x{0:X} = 0x{1:X2} ignored", offset, value);
        }

        public void Reset()
        {
            // WDT (or MCLR) reset: RAM and SFRs are RETAINED, only PCON
            // changes (POR/PORTE/BOR/VREGD stay set, WDT bit cleared) —
            // DS41190 Table 29-1. Flagged by the PIC12 core before the
            // machine reset.
            if(nextResetIsWdt)
            {
                nextResetIsWdt = false;
                pcon = 0xF8;
                return;
            }
            Array.Clear(gpr, 0, gpr.Length);
            indf = 0;
            tmr0 = 0;
            pclath = 0;
            intcon = 0;
            pir1 = 0;
            tmr1 = 0;
            t1con = 0;
            cmcon = 0;
            optionReg = 0xFF;  /* POR: GPPU,INTEDG,T0CS,T0SE,PSA,PS2:PS0 all 1 */
            trisio = 0x3F;     /* POR: all pins inputs */
            pie1 = 0;
            pcon = 0x0C;       /* PF + POR set */
            osccal = 0;
            wpu = 0x3F;
            gpioLatch = 0;
        }

        /// <summary>
        /// Mark the next machine reset as a watchdog reset so that
        /// <see cref="Reset"/> retains data memory (hardware behavior).
        /// </summary>
        public void MarkWdtResetComing()
        {
            nextResetIsWdt = true;
        }

        public long Size => 0xA0;

        private static bool IsGpr(long offset)
        {
            return (offset >= 0x06 && offset <= 0x1F)
                || (offset >= 0x20 && offset <= 0x5F)
                || (offset >= 0x88 && offset <= 0x9F);
        }

        private readonly IMachine machine;
        private readonly byte[] gpr;
        private bool nextResetIsWdt;

        private byte indf;
        private byte tmr0;
        private byte pclath;
        private byte intcon;
        private byte pir1;
        private ushort tmr1;
        private byte t1con;
        private byte cmcon;
        private byte optionReg;
        private byte trisio;
        private byte pie1;
        private byte pcon;
        private byte osccal;
        private byte wpu;
        private byte gpioLatch;
    }
}
