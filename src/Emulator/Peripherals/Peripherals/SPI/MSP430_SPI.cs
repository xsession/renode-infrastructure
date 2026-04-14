//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
// MSP430 USCI_B SPI mode.
// Registers: UCBxCTL0(0x00), UCBxCTL1(0x01), UCBxBR0(0x02), UCBxBR1(0x03),
//            UCBxSTAT(0x04), UCBxRXBUF(0x05), UCBxTXBUF(0x06).
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.SPI
{
    public class MSP430_SPI : IBytePeripheral, IKnownSize
    {
        public MSP430_SPI(IMachine machine)
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
                case 0x05:
                    stat &= unchecked((byte)~UCRXIFG_BIT);
                    return rxBuf;
                case 0x06: return txBuf;
            }
            this.Log(LogLevel.Warning, "MSP430_SPI: Read 0x{0:X}", offset);
            return 0;
        }

        public void WriteByte(long offset, byte value)
        {
            switch(offset)
            {
                case 0x00: ctl0 = value; return;
                case 0x01: ctl1 = value; return;
                case 0x02: br0 = value; return;
                case 0x03: br1 = value; return;
                case 0x04: stat = value; return;
                case 0x06:
                    txBuf = value;
                    // Loopback: no slave → rx = tx
                    rxBuf = value;
                    stat |= UCTXIFG_BIT;
                    stat |= UCRXIFG_BIT;
                    IRQ.Set();
                    IRQ.Unset();
                    return;
            }
        }

        public void Reset()
        {
            ctl0 = 0x01; // UCSWRST set at reset
            ctl1 = 0x01;
            br0 = 0; br1 = 0;
            stat = 0;
            rxBuf = 0; txBuf = 0;
        }

        public long Size => 0x07;
        public GPIO IRQ { get; }

        private const byte UCTXIFG_BIT = 0x02;
        private const byte UCRXIFG_BIT = 0x01;

        private byte ctl0, ctl1, br0, br1, stat;
        private byte rxBuf, txBuf;
    }
}
