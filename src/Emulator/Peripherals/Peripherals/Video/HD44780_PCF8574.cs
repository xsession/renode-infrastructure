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
    // Hitachi HD44780 character LCD via PCF8574 I2C backpack
    // Typical 16x2 or 20x4 LCD. Data sent as nibbles via I2C expander.
    public class HD44780_PCF8574 : II2CPeripheral, IPeripheral
    {
        public HD44780_PCF8574(int columns = 16, int rows = 2)
        {
            this.columns = columns;
            this.rows = rows;
            ddram = new byte[columns * rows];
            Reset();
        }

        public void Reset()
        {
            Array.Clear(ddram, 0, ddram.Length);
            cursorPos = 0;
            displayOn = false;
            lastNibble = 0;
            highNibbleNext = true;
        }

        public void Write(byte[] data)
        {
            foreach(var b in data)
            {
                // PCF8574 bit layout: D7 D6 D5 D4 BL EN RW RS
                var rs = (b & 0x01) != 0;    // Register Select
                var en = (b & 0x04) != 0;    // Enable
                var nibble = (b >> 4) & 0x0F;

                if(en) // Latch on EN high
                {
                    if(highNibbleNext)
                    {
                        lastNibble = (byte)(nibble << 4);
                        highNibbleNext = false;
                    }
                    else
                    {
                        var fullByte = (byte)(lastNibble | nibble);
                        highNibbleNext = true;

                        if(rs) // Data
                        {
                            if(cursorPos < ddram.Length)
                                ddram[cursorPos++] = fullByte;
                        }
                        else // Command
                        {
                            ProcessCommand(fullByte);
                        }
                    }
                }
            }
        }

        public byte[] Read(int count) => new byte[count];
        public void FinishTransmission() { }

        private void ProcessCommand(byte cmd)
        {
            if(cmd == 0x01) { Array.Clear(ddram, 0, ddram.Length); cursorPos = 0; } // Clear
            else if(cmd == 0x02) { cursorPos = 0; } // Home
            else if((cmd & 0x80) != 0) { cursorPos = cmd & 0x7F; } // Set DDRAM address
            else if((cmd & 0x08) != 0) { displayOn = (cmd & 0x04) != 0; } // Display on/off
        }

        public string GetText()
        {
            var text = "";
            for(var r = 0; r < rows; r++)
            {
                for(var c = 0; c < columns; c++)
                {
                    var ch = ddram[r * columns + c];
                    text += ch >= 0x20 && ch < 0x7F ? (char)ch : ' ';
                }
                if(r < rows - 1) text += "\n";
            }
            return text;
        }

        public bool DisplayOn => displayOn;
        private readonly int columns, rows;
        private byte[] ddram;
        private int cursorPos;
        private bool displayOn;
        private byte lastNibble;
        private bool highNibbleNext;
    }
}
