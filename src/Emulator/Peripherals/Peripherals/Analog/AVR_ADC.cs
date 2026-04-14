//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
//
// AVR 10-bit Analog-to-Digital Converter peripheral.
//
// Register map (byte-wide I/O):
//   0x00  ADMUX  — ADC Multiplexer Selection (REFS1:0, ADLAR, MUX4:0)
//   0x01  ADCSRA — ADC Control & Status A (ADEN, ADSC, ADATE, ADIF, ADIE, ADPS2:0)
//   0x02  ADCL   — ADC Data Low
//   0x03  ADCH   — ADC Data High
//   0x04  ADCSRB — ADC Control & Status B (trigger source)
//   0x05  DIDR0  — Digital Input Disable Register 0
//
using System;

using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Analog
{
    public class AVR_ADC : IBytePeripheral, IKnownSize
    {
        public AVR_ADC(IMachine machine, int channelCount = 8)
        {
            this.channelCount = channelCount;
            channelValues = new ushort[channelCount];
            IRQ = new GPIO();
            Reset();
        }

        public byte ReadByte(long offset)
        {
            switch((Registers)offset)
            {
            case Registers.ADMUX:  return admux;
            case Registers.ADCSRA: return adcsra;
            case Registers.ADCL:   return (byte)(adcResult & 0xFF);
            case Registers.ADCH:   return (byte)((adcResult >> 8) & 0x03);
            case Registers.ADCSRB: return adcsrb;
            case Registers.DIDR0:  return didr0;
            default:
                this.Log(LogLevel.Warning,
                         "AVR_ADC: Read from unknown offset 0x{0:X}", offset);
                return 0;
            }
        }

        public void WriteByte(long offset, byte value)
        {
            switch((Registers)offset)
            {
            case Registers.ADMUX:
                admux = value;
                break;
            case Registers.ADCSRA:
                adcsra = value;
                /* Clear ADIF by writing 1 */
                if((value & ADCSRA_ADIF) != 0)
                {
                    adcsra &= unchecked((byte)~ADCSRA_ADIF);
                }
                /* Start conversion if ADSC set */
                if((value & ADCSRA_ADSC) != 0 && (value & ADCSRA_ADEN) != 0)
                {
                    PerformConversion();
                }
                break;
            case Registers.ADCSRB:
                adcsrb = value;
                break;
            case Registers.DIDR0:
                didr0 = value;
                break;
            default:
                this.Log(LogLevel.Warning,
                         "AVR_ADC: Write 0x{0:X} to unknown offset 0x{1:X}", value, offset);
                break;
            }
        }

        public void Reset()
        {
            admux     = 0;
            adcsra    = 0;
            adcsrb    = 0;
            didr0     = 0;
            adcResult = 0;
            for(int i = 0; i < channelCount; i++)
                channelValues[i] = 0;
            IRQ.Unset();
        }

        /// <summary>Set the voltage level for a given ADC channel (0-1023).</summary>
        public void SetChannelValue(int channel, ushort value)
        {
            if(channel >= 0 && channel < channelCount)
            {
                channelValues[channel] = (ushort)(value & 0x3FF);
            }
        }

        public long Size => 0x06;

        public GPIO IRQ { get; }

        private void PerformConversion()
        {
            int mux = admux & 0x0F;
            if(mux < channelCount)
            {
                adcResult = channelValues[mux];
            }
            else
            {
                adcResult = 0;
            }

            /* ADLAR: left-adjust result */
            if((admux & ADMUX_ADLAR) != 0)
            {
                adcResult = (ushort)((adcResult << 6) & 0xFFC0);
            }

            adcsra |= ADCSRA_ADIF;
            adcsra &= unchecked((byte)~ADCSRA_ADSC); /* Conversion complete */

            if((adcsra & ADCSRA_ADIE) != 0)
            {
                IRQ.Set();
                IRQ.Unset();
            }
        }

        private enum Registers : long
        {
            ADMUX  = 0x00,
            ADCSRA = 0x01,
            ADCL   = 0x02,
            ADCH   = 0x03,
            ADCSRB = 0x04,
            DIDR0  = 0x05,
        }

        private const byte ADMUX_ADLAR  = (1 << 5);

        private const byte ADCSRA_ADPS  = 0x07;
        private const byte ADCSRA_ADIE  = (1 << 3);
        private const byte ADCSRA_ADIF  = (1 << 4);
        private const byte ADCSRA_ADATE = (1 << 5);
        private const byte ADCSRA_ADSC  = (1 << 6);
        private const byte ADCSRA_ADEN  = (byte)(1 << 7);

        private byte   admux;
        private byte   adcsra;
        private byte   adcsrb;
        private byte   didr0;
        private ushort adcResult;

        private readonly int channelCount;
        private readonly ushort[] channelValues;
    }
}
