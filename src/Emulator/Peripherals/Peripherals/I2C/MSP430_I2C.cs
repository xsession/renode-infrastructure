//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
// MSP430 USCI_B I2C mode.
// Registers: UCBxCTL0(0x00), UCBxCTL1(0x01), UCBxBR0(0x02), UCBxBR1(0x03),
//            UCBxSTAT(0x04), UCBxRXBUF(0x05), UCBxTXBUF(0x06),
//            UCBxI2COA(0x08-0x09), UCBxI2CSA(0x0A-0x0B).
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.I2C
{
    public class MSP430_I2C : IBytePeripheral, IKnownSize
    {
        public MSP430_I2C(IMachine machine)
        {
            IRQ = new GPIO();
            Reset();
        }

        public byte ReadByte(long offset)
        {
            switch(offset)
            {
                case 0x00: return ctl0;
                case 0x01: return ctl1;
                case 0x02: return br0;
                case 0x03: return br1;
                case 0x04: return stat;
                case 0x05: return rxBuf;
                case 0x06: return txBuf;
                case 0x08: return (byte)(i2coa & 0xFF);
                case 0x09: return (byte)((i2coa >> 8) & 0x03);
                case 0x0A: return (byte)(i2csa & 0xFF);
                case 0x0B: return (byte)((i2csa >> 8) & 0x03);
            }
            this.Log(LogLevel.Warning, "MSP430_I2C: Read 0x{0:X}", offset);
            return 0;
        }

        public void WriteByte(long offset, byte value)
        {
            switch(offset)
            {
                case 0x00: ctl0 = value; return;
                case 0x01:
                    ctl1 = value;
                    if((ctl1 & UCTXSTT) != 0)
                    {
                        // generate START
                        stat |= UCBUSY;
                    }
                    if((ctl1 & UCTXSTP) != 0)
                    {
                        stat &= unchecked((byte)~UCBUSY);
                    }
                    return;
                case 0x02: br0 = value; return;
                case 0x03: br1 = value; return;
                case 0x04: stat = value; return;
                case 0x06:
                    txBuf = value;
                    IRQ.Set();
                    IRQ.Unset();
                    return;
                case 0x08: i2coa = (ushort)((i2coa & 0xFF00) | value); return;
                case 0x09: i2coa = (ushort)((i2coa & 0x00FF) | ((value & 0x03) << 8)); return;
                case 0x0A: i2csa = (ushort)((i2csa & 0xFF00) | value); return;
                case 0x0B: i2csa = (ushort)((i2csa & 0x00FF) | ((value & 0x03) << 8)); return;
            }
        }

        public void Reset()
        {
            ctl0 = 0x01; // UCSWRST
            ctl1 = 0x01;
            br0 = 0; br1 = 0;
            stat = 0;
            rxBuf = 0; txBuf = 0;
            i2coa = 0; i2csa = 0;
        }

        public long Size => 0x0C;
        public GPIO IRQ { get; }

        private const byte UCTXSTT = 0x02;
        private const byte UCTXSTP = 0x04;
        private const byte UCBUSY  = 0x01;

        private byte ctl0, ctl1, br0, br1, stat;
        private byte rxBuf, txBuf;
        private ushort i2coa, i2csa;
    }
}
