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
    // Philips PCD8544 - 84x48 monochrome LCD controller (Nokia 5110) (SPI)
    public class PCD8544 : ISPIPeripheral, IPeripheral
    {
        public PCD8544()
        {
            buffer = new byte[84 * 48 / 8]; // 504 bytes
            Reset();
        }

        public void Reset()
        {
            xAddress = 0;
            yAddress = 0;
            displayOn = false;
            Array.Clear(buffer, 0, buffer.Length);
        }

        public void FinishTransmission() { }

        public byte Transmit(byte data)
        {
            if(dcPin) // Data mode
            {
                var addr = yAddress * 84 + xAddress;
                if(addr < buffer.Length) buffer[addr] = data;
                xAddress++;
                if(xAddress >= 84) { xAddress = 0; yAddress = (yAddress + 1) % 6; }
            }
            else // Command mode
            {
                if((data & 0x80) != 0) { xAddress = data & 0x7F; } // Set X
                else if((data & 0x40) != 0) { yAddress = data & 0x07; } // Set Y
                else if(data == 0x20) { /* Function set: basic */ }
                else if(data == 0x21) { /* Function set: extended */ }
                else if(data == 0x0C) { displayOn = true; }
                else if(data == 0x08) { displayOn = false; }
            }
            return 0;
        }

        // DC pin state must be set by the platform using GPIO
        public bool DCPin { set { dcPin = value; } }

        public bool DisplayOn => displayOn;
        private byte[] buffer;
        private bool displayOn;
        private bool dcPin;
        private int xAddress, yAddress;
    }
}
