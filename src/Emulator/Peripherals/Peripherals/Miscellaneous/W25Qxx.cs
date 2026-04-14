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

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    // Winbond W25Qxx - SPI NOR flash (supports W25Q16 to W25Q256)
    public class W25Qxx : ISPIPeripheral, IPeripheral
    {
        public W25Qxx(int sizeMbits = 128)
        {
            var sizeBytes = sizeMbits * 1024 * 1024 / 8;
            storage = new byte[sizeBytes];
            // Fill with 0xFF (erased state)
            for(var i = 0; i < storage.Length; i++) storage[i] = 0xFF;
            // JEDEC ID: Winbond (0xEF), memory type (0x40), capacity
            byte cap;
            switch(sizeMbits)
            {
                case 16: cap = 0x15; break;
                case 32: cap = 0x16; break;
                case 64: cap = 0x17; break;
                case 128: cap = 0x18; break;
                case 256: cap = 0x19; break;
                default: cap = 0x18; break;
            }
            jedecId = new byte[] { 0xEF, 0x40, cap };
            Reset();
        }

        public void Reset()
        {
            state = State.Idle;
            address = 0;
            byteCount = 0;
            writeEnabled = false;
        }

        public void FinishTransmission()
        {
            state = State.Idle;
            byteCount = 0;
        }

        public byte Transmit(byte data)
        {
            switch(state)
            {
                case State.Idle:
                    return ProcessCommand(data);
                case State.ReadAddress:
                    address = (address << 8) | data;
                    byteCount++;
                    if(byteCount >= 3) { state = State.ReadData; }
                    return 0;
                case State.ReadData:
                    var val = address < storage.Length ? storage[address] : (byte)0xFF;
                    address++;
                    return val;
                case State.WriteAddress:
                    address = (address << 8) | data;
                    byteCount++;
                    if(byteCount >= 3) { state = State.WriteData; }
                    return 0;
                case State.WriteData:
                    if(writeEnabled && address < storage.Length)
                    {
                        storage[address] &= data; // flash can only clear bits
                        address++;
                    }
                    return 0;
                case State.ReadJedecId:
                    return byteCount < jedecId.Length ? jedecId[byteCount++] : (byte)0;
                case State.ReadStatus:
                    return (byte)(writeEnabled ? 0x02 : 0x00);
                default:
                    return 0;
            }
        }

        private byte ProcessCommand(byte cmd)
        {
            switch(cmd)
            {
                case 0x06: writeEnabled = true; break;   // WREN
                case 0x04: writeEnabled = false; break;   // WRDI
                case 0x03: // Read Data
                    state = State.ReadAddress; address = 0; byteCount = 0;
                    break;
                case 0x02: // Page Program
                    state = State.WriteAddress; address = 0; byteCount = 0;
                    break;
                case 0x9F: // JEDEC ID
                    state = State.ReadJedecId; byteCount = 0;
                    break;
                case 0x05: // Read Status Register 1
                    state = State.ReadStatus;
                    break;
                case 0x20: // Sector Erase (4KB)
                    state = State.ReadAddress; address = 0; byteCount = 0;
                    // Erase handled on CS deassert
                    break;
                case 0xC7: // Chip Erase
                    if(writeEnabled)
                    {
                        for(var i = 0; i < storage.Length; i++) storage[i] = 0xFF;
                        this.Log(LogLevel.Debug, "Chip erased");
                    }
                    break;
                case 0xAB: break; // Release Power Down
                case 0xB9: break; // Power Down
            }
            return 0;
        }

        private byte[] storage;
        private byte[] jedecId;
        private State state;
        private int address;
        private int byteCount;
        private bool writeEnabled;

        private enum State
        {
            Idle,
            ReadAddress,
            ReadData,
            WriteAddress,
            WriteData,
            ReadJedecId,
            ReadStatus
        }
    }
}
