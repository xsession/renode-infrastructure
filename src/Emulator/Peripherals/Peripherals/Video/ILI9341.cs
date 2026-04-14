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
    // Ilitek ILI9341 - 240x320 RGB565 TFT LCD controller (SPI)
    public class ILI9341 : ISPIPeripheral, IPeripheral
    {
        public ILI9341()
        {
            buffer = new byte[240 * 320 * 2];
            Reset();
        }

        public void Reset()
        {
            dataMode = false;
            currentCommand = 0;
            pixelIndex = 0;
            displayOn = false;
            Array.Clear(buffer, 0, buffer.Length);
        }

        public void FinishTransmission()
        {
            // CS deasserted
        }

        public byte Transmit(byte data)
        {
            if(!dataMode)
            {
                // Command byte
                currentCommand = data;
                switch(data)
                {
                    case 0x01: Reset(); break; // Software reset
                    case 0x11: sleepMode = false; break; // Sleep out
                    case 0x10: sleepMode = true; break;  // Sleep in
                    case 0x29: displayOn = true; this.Log(LogLevel.Debug, "Display ON"); break;
                    case 0x28: displayOn = false; break;
                    case 0x2C: pixelIndex = 0; dataMode = true; break; // Memory write
                    case 0x36: dataMode = true; break; // MADCTL
                    case 0x3A: dataMode = true; break; // COLMOD (pixel format)
                }
            }
            else
            {
                if(currentCommand == 0x2C)
                {
                    if(pixelIndex < buffer.Length)
                        buffer[pixelIndex++] = data;
                }
                else
                {
                    // Parameter byte for other commands
                    dataMode = false;
                }
            }
            return 0;
        }

        public bool DisplayOn => displayOn;
        public int Width => 240;
        public int Height => 320;

        private byte[] buffer;
        private bool dataMode;
        private byte currentCommand;
        private int pixelIndex;
        private bool displayOn;
        private bool sleepMode = true;

    }
}
