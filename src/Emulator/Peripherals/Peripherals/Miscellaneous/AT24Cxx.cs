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

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    // Microchip AT24Cxx - I2C EEPROM (supports AT24C02 to AT24C256)
    public class AT24Cxx : II2CPeripheral, IPeripheral
    {
        public AT24Cxx(int sizeKbits = 256)
        {
            var sizeBytes = sizeKbits * 1024 / 8;
            storage = new byte[sizeBytes];
            twoByteAddress = sizeKbits > 16;
            pageSizeMask = sizeKbits <= 16 ? 0x0F : 0x3F; // 16 or 64 byte pages
            Reset();
        }

        public void Reset()
        {
            address = 0;
        }

        public void Write(byte[] data)
        {
            if(data.Length == 0) return;

            int dataStart;
            if(twoByteAddress)
            {
                if(data.Length < 2) return;
                address = (data[0] << 8) | data[1];
                dataStart = 2;
            }
            else
            {
                address = data[0];
                dataStart = 1;
            }

            for(var i = dataStart; i < data.Length; i++)
            {
                if(address < storage.Length)
                    storage[address] = data[i];
                // Page wrap: only lower bits increment
                address = (address & ~pageSizeMask) | ((address + 1) & pageSizeMask);
            }
        }

        public byte[] Read(int count)
        {
            var result = new byte[count];
            for(var i = 0; i < count; i++)
            {
                result[i] = address < storage.Length ? storage[address] : (byte)0xFF;
                address = (address + 1) % storage.Length;
            }
            return result;
        }

        public void FinishTransmission() { }

        private byte[] storage;
        private int address;
        private bool twoByteAddress;
        private int pageSizeMask;
    }
}
