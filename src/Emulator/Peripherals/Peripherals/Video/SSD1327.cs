//
// Copyright (c) 2010-2025 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;

namespace Antmicro.Renode.Peripherals.Video
{
    // Solomon Systech SSD1327 - 128x128 16-level grayscale OLED (I2C)
    public class SSD1327 : II2CPeripheral, IPeripheral
    {
        public SSD1327()
        {
            buffer = new byte[128 * 128 / 2]; // 4-bit per pixel
            Reset();
        }

        public void Reset()
        {
            displayOn = false;
            columnAddress = 0;
            rowAddress = 0;
            Array.Clear(buffer, 0, buffer.Length);
        }

        public void Write(byte[] data)
        {
            if(data.Length < 2) return;
            var isData = (data[0] & 0x40) != 0;
            for(var i = 1; i < data.Length; i++)
            {
                if(isData)
                {
                    var addr = rowAddress * 64 + columnAddress;
                    if(addr < buffer.Length) buffer[addr] = data[i];
                    columnAddress++;
                    if(columnAddress >= 64) { columnAddress = 0; rowAddress++; }
                }
                else
                {
                    if(data[i] == 0xAE) displayOn = false;
                    else if(data[i] == 0xAF) displayOn = true;
                }
            }
        }

        public byte[] Read(int count) => new byte[count];
        public void FinishTransmission() { }

        public bool DisplayOn => displayOn;
        private byte[] buffer;
        private bool displayOn;
        private int columnAddress, rowAddress;
    }
}
