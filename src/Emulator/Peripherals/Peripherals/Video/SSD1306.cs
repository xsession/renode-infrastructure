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
    // Solomon Systech SSD1306 - 128x64 monochrome OLED controller (I2C)
    public class SSD1306 : II2CPeripheral, IPeripheral
    {
        public SSD1306()
        {
            buffer = new byte[128 * 64 / 8]; // 1024 bytes GDDRAM
            Reset();
        }

        public void Reset()
        {
            displayOn = false;
            contrast = 0x7F;
            pageAddress = 0;
            columnAddress = 0;
            Array.Clear(buffer, 0, buffer.Length);
        }

        public void Write(byte[] data)
        {
            if(data.Length < 2) return;
            var controlByte = data[0];
            var isContinuation = (controlByte & 0x80) != 0;
            var isData = (controlByte & 0x40) != 0;

            for(var i = 1; i < data.Length; i++)
            {
                if(isData)
                {
                    // Write to GDDRAM
                    var addr = pageAddress * 128 + columnAddress;
                    if(addr < buffer.Length)
                        buffer[addr] = data[i];
                    columnAddress = (columnAddress + 1) & 0x7F;
                }
                else
                {
                    ProcessCommand(data[i]);
                }
            }
        }

        public byte[] Read(int count)
        {
            var result = new byte[count];
            for(var i = 0; i < count; i++)
            {
                var addr = pageAddress * 128 + columnAddress;
                result[i] = addr < buffer.Length ? buffer[addr] : (byte)0;
                columnAddress = (columnAddress + 1) & 0x7F;
            }
            return result;
        }

        public void FinishTransmission() { }

        private void ProcessCommand(byte cmd)
        {
            if(cmd <= 0x0F) { columnAddress = (columnAddress & 0xF0) | (cmd & 0x0F); } // lower column
            else if(cmd >= 0x10 && cmd <= 0x1F) { columnAddress = (columnAddress & 0x0F) | ((cmd & 0x0F) << 4); } // upper column
            else if(cmd >= 0xB0 && cmd <= 0xB7) { pageAddress = cmd & 0x07; }
            else if(cmd == 0xAE) { displayOn = false; this.Log(LogLevel.Debug, "Display OFF"); }
            else if(cmd == 0xAF) { displayOn = true; this.Log(LogLevel.Debug, "Display ON"); }
            else if(cmd == 0x81) { nextIsContrast = true; }
            else if(nextIsContrast) { contrast = cmd; nextIsContrast = false; }
        }

        public bool DisplayOn => displayOn;
        public byte Contrast => contrast;

        private byte[] buffer;
        private bool displayOn;
        private byte contrast;
        private int pageAddress;
        private int columnAddress;
        private bool nextIsContrast;
    }
}
