//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
// MSP430 ADC12 (12-bit SAR ADC).
// Registers: ADC12CTL0(0x00-0x01), ADC12CTL1(0x02-0x03), ADC12IFG(0x04-0x05),
//            ADC12IE(0x06-0x07), ADC12IV(0x08-0x09),
//            ADC12MCTL0-15(0x10-0x1F), ADC12MEM0-15(0x20-0x3F).
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Analog
{
    public class MSP430_ADC : IWordPeripheral, IKnownSize
    {
        public MSP430_ADC(IMachine machine, int channels = 16)
        {
            this.channels = channels;
            channelValues = new ushort[channels];
            mem = new ushort[channels];
            mctl = new byte[channels];
            IRQ = new GPIO();
            Reset();
        }

        public ushort ReadWord(long offset)
        {
            switch(offset)
            {
                case 0x00: return ctl0;
                case 0x02: return ctl1;
                case 0x04: return ifg;
                case 0x06: return ie;
                case 0x08: return iv;
            }

            // MCTL registers (byte-sized, but word-accessible)
            if(offset >= 0x10 && offset < 0x10 + channels)
            {
                return mctl[offset - 0x10];
            }

            // MEM registers
            if(offset >= 0x20 && offset < 0x20 + channels * 2)
            {
                int idx = (int)(offset - 0x20) / 2;
                if(idx < channels) return mem[idx];
            }

            this.Log(LogLevel.Warning, "MSP430_ADC: Read 0x{0:X}", offset);
            return 0;
        }

        public void WriteWord(long offset, ushort value)
        {
            switch(offset)
            {
                case 0x00:
                    ctl0 = value;
                    if((ctl0 & ADC12SC) != 0 && (ctl0 & ADC12ON) != 0)
                        PerformConversion();
                    return;
                case 0x02: ctl1 = value; return;
                case 0x04: ifg = value; return;
                case 0x06: ie = value; return;
                case 0x08: return; // IV read-only
            }

            if(offset >= 0x10 && offset < 0x10 + channels)
            {
                mctl[offset - 0x10] = (byte)value;
                return;
            }
        }

        public void SetChannelValue(int channel, ushort value)
        {
            if(channel >= 0 && channel < channels)
                channelValues[channel] = (ushort)(value & 0x0FFF);
        }

        public void Reset()
        {
            ctl0 = 0; ctl1 = 0; ifg = 0; ie = 0; iv = 0;
            Array.Clear(mem, 0, mem.Length);
            Array.Clear(mctl, 0, mctl.Length);
        }

        public long Size => 0x40;
        public GPIO IRQ { get; }

        private void PerformConversion()
        {
            int startCh = (ctl1 >> 12) & 0x0F; // CSTARTADDx
            int inch = mctl[startCh] & 0x0F;
            ushort result = (inch < channels) ? channelValues[inch] : (ushort)0;
            mem[startCh] = result;
            ifg |= (ushort)(1 << startCh);

            ctl0 &= unchecked((ushort)~ADC12SC); // auto-clear

            if((ie & (1 << startCh)) != 0)
            {
                IRQ.Set();
                IRQ.Unset();
            }
        }

        private const ushort ADC12ON = 0x0010;
        private const ushort ADC12SC = 0x0001;

        private ushort ctl0, ctl1, ifg, ie, iv;
        private readonly int channels;
        private readonly ushort[] channelValues;
        private readonly ushort[] mem;
        private readonly byte[] mctl;
    }
}
