//
// Copyright (c) 2010-2025 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.SPI;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    // NXP MFRC522 - contactless RFID reader (SPI)
    public class MFRC522 : ISPIPeripheral, IPeripheral
    {
        public MFRC522()
        {
            Reset();
        }

        public void Reset()
        {
            registerAddress = 0;
            fifoBuffer = new byte[64];
            fifoIndex = 0;
            fifoLevel = 0;
        }

        public void FinishTransmission()
        {
            byteCount = 0;
        }

        public byte Transmit(byte data)
        {
            if(byteCount == 0)
            {
                registerAddress = (byte)((data >> 1) & 0x3F);
                isRead = (data & 0x80) != 0;
                byteCount++;
                if(isRead)
                {
                    return ReadRegister(registerAddress);
                }
                return 0;
            }
            else
            {
                byteCount++;
                if(isRead)
                {
                    // Continued read - address from first byte
                    return ReadRegister(registerAddress);
                }
                else
                {
                    WriteRegister(registerAddress, data);
                    return 0;
                }
            }
        }

        private byte ReadRegister(byte addr)
        {
            switch(addr)
            {
                case 0x04: return (byte)fifoLevel; // FIFOLevelReg
                case 0x09: return fifoLevel > 0 ? fifoBuffer[--fifoLevel] : (byte)0; // FIFODataReg
                case 0x37: return 0x92; // VersionReg (MFRC522 v2.0)
                default: return 0;
            }
        }

        private void WriteRegister(byte addr, byte val)
        {
            switch(addr)
            {
                case 0x01: // CommandReg
                    if(val == 0x0C) // Transceive
                    {
                        // Simulate card response with TagUID
                        if(TagUID != null)
                        {
                            fifoLevel = Math.Min(TagUID.Length, 64);
                            Array.Copy(TagUID, fifoBuffer, fifoLevel);
                        }
                    }
                    break;
                case 0x09: // FIFODataReg write
                    if(fifoLevel < 64) fifoBuffer[fifoLevel++] = val;
                    break;
            }
        }

        public byte[] TagUID { get; set; } = new byte[] { 0x04, 0x11, 0x22, 0x33 };

        private byte registerAddress;
        private bool isRead;
        private int byteCount;
        private byte[] fifoBuffer;
        private int fifoIndex;
        private int fifoLevel;
    }
}
