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
    // Sino Wealth SH1106 - 132x64 monochrome OLED controller (I2C)
    public class SH1106 : II2CPeripheral, IPeripheral
    {
        public SH1106()
        {
            buffer = new byte[132 * 64 / 8]; // 1056 bytes
            Reset();
        }

        public void Reset()
        {
            displayOn = false;
            pageAddress = 0;
            columnAddress = 0;
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
                    var addr = pageAddress * 132 + columnAddress;
                    if(addr < buffer.Length) buffer[addr] = data[i];
                    columnAddress = Math.Min(columnAddress + 1, 131);
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
                var addr = pageAddress * 132 + columnAddress;
                result[i] = addr < buffer.Length ? buffer[addr] : (byte)0;
                columnAddress = Math.Min(columnAddress + 1, 131);
            }
            return result;
        }

        public void FinishTransmission() { }

        private void ProcessCommand(byte cmd)
        {
            if(cmd <= 0x0F) columnAddress = (columnAddress & 0xF0) | (cmd & 0x0F);
            else if(cmd >= 0x10 && cmd <= 0x1F) columnAddress = (columnAddress & 0x0F) | ((cmd & 0x0F) << 4);
            else if(cmd >= 0xB0 && cmd <= 0xB7) pageAddress = cmd & 0x07;
            else if(cmd == 0xAE) displayOn = false;
            else if(cmd == 0xAF) displayOn = true;
        }

        public bool DisplayOn => displayOn;
        private byte[] buffer;
        private bool displayOn;
        private int pageAddress, columnAddress;
    }
}
