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
    // UltraChip UC1701 - 102x64 monochrome LCD controller (SPI)
    public class UC1701 : ISPIPeripheral, IPeripheral
    {
        public UC1701()
        {
            buffer = new byte[102 * 64 / 8];
            Reset();
        }

        public void Reset()
        {
            pageAddress = 0;
            columnAddress = 0;
            displayOn = false;
            Array.Clear(buffer, 0, buffer.Length);
        }

        public void FinishTransmission() { }

        public byte Transmit(byte data)
        {
            if(dcPin)
            {
                var addr = pageAddress * 102 + columnAddress;
                if(addr < buffer.Length) buffer[addr] = data;
                columnAddress = Math.Min(columnAddress + 1, 101);
            }
            else
            {
                if(data <= 0x0F) columnAddress = (columnAddress & 0xF0) | (data & 0x0F);
                else if(data >= 0x10 && data <= 0x1F) columnAddress = (columnAddress & 0x0F) | ((data & 0x0F) << 4);
                else if(data >= 0xB0 && data <= 0xBF) pageAddress = data & 0x0F;
                else if(data == 0xAE) displayOn = false;
                else if(data == 0xAF) displayOn = true;
                else if(data == 0xE2) Reset();
            }
            return 0;
        }

        public bool DCPin { set { dcPin = value; } }
        public bool DisplayOn => displayOn;
        private byte[] buffer;
        private bool displayOn;
        private bool dcPin;
        private int pageAddress, columnAddress;
    }
}
