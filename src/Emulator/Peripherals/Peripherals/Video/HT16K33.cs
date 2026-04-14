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
    // Holtek HT16K33 - 16x8 LED matrix / key scan driver (I2C)
    public class HT16K33 : II2CPeripheral, IPeripheral
    {
        public HT16K33()
        {
            displayRam = new byte[16]; // 16 bytes of display RAM
            Reset();
        }

        public void Reset()
        {
            Array.Clear(displayRam, 0, 16);
            systemOn = false;
            displayOn = false;
            brightness = 15;
            blinkRate = 0;
        }

        public void Write(byte[] data)
        {
            if(data.Length == 0) return;
            var cmd = data[0];

            if(cmd <= 0x0F && data.Length >= 2) // Display RAM write
            {
                for(var i = 1; i < data.Length && (cmd + i - 1) < 16; i++)
                    displayRam[cmd + i - 1] = data[i];
            }
            else if((cmd & 0xF0) == 0x20) { systemOn = (cmd & 0x01) != 0; } // System setup
            else if((cmd & 0xF0) == 0x80) // Display setup
            {
                displayOn = (cmd & 0x01) != 0;
                blinkRate = (cmd >> 1) & 0x03;
            }
            else if((cmd & 0xF0) == 0xE0) { brightness = cmd & 0x0F; } // Dimming
        }

        public byte[] Read(int count)
        {
            var result = new byte[count];
            Array.Copy(displayRam, 0, result, 0, Math.Min(count, 16));
            return result;
        }

        public void FinishTransmission() { }

        public byte[] DisplayRam => displayRam;
        public bool DisplayOn => displayOn;
        public int Brightness => brightness;

        private byte[] displayRam;
        private bool systemOn, displayOn;
        private int brightness, blinkRate;
    }
}
