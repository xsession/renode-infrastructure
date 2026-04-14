//
// Copyright (c) 2010-2025 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.SPI;

namespace Antmicro.Renode.Peripherals.Video
{
    // Maxim MAX7219 - 8-digit LED / 8x8 LED matrix driver (SPI)
    public class MAX7219 : ISPIPeripheral, IPeripheral
    {
        public MAX7219()
        {
            digits = new byte[8];
            Reset();
        }

        public void Reset()
        {
            Array.Clear(digits, 0, 8);
            highByte = 0;
            receivingHigh = true;
            shutdown = true;
            intensity = 0;
            scanLimit = 7;
        }

        public void FinishTransmission()
        {
            receivingHigh = true;
        }

        public byte Transmit(byte data)
        {
            if(receivingHigh)
            {
                highByte = data;
                receivingHigh = false;
            }
            else
            {
                receivingHigh = true;
                var reg = highByte & 0x0F;
                switch(highByte)
                {
                    case 0x09: decodeMode = data; break;
                    case 0x0A: intensity = data; break;
                    case 0x0B: scanLimit = data & 0x07; break;
                    case 0x0C: shutdown = data == 0; break;
                    case 0x0F: displayTest = data != 0; break;
                    default:
                        if(highByte >= 0x01 && highByte <= 0x08)
                            digits[highByte - 1] = data;
                        break;
                }
            }
            return 0;
        }

        public byte[] Digits => digits;
        public bool Shutdown => shutdown;
        public byte Intensity => intensity;

        private byte[] digits;
        private byte highByte;
        private bool receivingHigh;
        private bool shutdown;
        private byte intensity;
        private int scanLimit;
        private byte decodeMode;
        private bool displayTest;
    }
}
