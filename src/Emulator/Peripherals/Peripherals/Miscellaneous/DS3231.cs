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
using Antmicro.Renode.Peripherals.I2C;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    // Maxim DS3231 - high-precision I2C RTC with temperature compensation
    public class DS3231 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public DS3231()
        {
            RegistersCollection = new ByteRegisterCollection(this);
            DefineRegisters();
        }

        public void Reset()
        {
            RegistersCollection.Reset();
            registerAddress = 0;
        }

        public void Write(byte[] data)
        {
            if(data.Length == 0) return;
            registerAddress = data[0];
            for(var i = 1; i < data.Length; i++)
            {
                RegistersCollection.Write(registerAddress, data[i]);
                registerAddress++;
            }
        }

        public byte[] Read(int count)
        {
            UpdateTime();
            var result = new byte[count];
            for(var i = 0; i < count; i++)
            {
                result[i] = RegistersCollection.Read(registerAddress);
                registerAddress++;
            }
            return result;
        }

        public void FinishTransmission() { }

        public ByteRegisterCollection RegistersCollection { get; }

        private void DefineRegisters()
        {
            RegistersCollection.DefineRegister(0x00); // Seconds (BCD)
            RegistersCollection.DefineRegister(0x01); // Minutes (BCD)
            RegistersCollection.DefineRegister(0x02); // Hours (BCD)
            RegistersCollection.DefineRegister(0x03, 0x01); // Day of week
            RegistersCollection.DefineRegister(0x04, 0x01); // Date (BCD)
            RegistersCollection.DefineRegister(0x05, 0x01); // Month (BCD)
            RegistersCollection.DefineRegister(0x06, 0x25); // Year (BCD, 00-99)
            // Alarm 1 (0x07-0x0A)
            RegistersCollection.DefineRegister(0x07);
            RegistersCollection.DefineRegister(0x08);
            RegistersCollection.DefineRegister(0x09);
            RegistersCollection.DefineRegister(0x0A);
            // Alarm 2 (0x0B-0x0D)
            RegistersCollection.DefineRegister(0x0B);
            RegistersCollection.DefineRegister(0x0C);
            RegistersCollection.DefineRegister(0x0D);
            // Control (0x0E), Status (0x0F)
            RegistersCollection.DefineRegister(0x0E, 0x1C);
            RegistersCollection.DefineRegister(0x0F, 0x00);
            // Temperature (0x11-0x12), 10-bit, 0.25 C/LSB
            RegistersCollection.DefineRegister(0x11, 0x19); // 25Â°C integer
            RegistersCollection.DefineRegister(0x12, 0x00); // fraction
        }

        private void UpdateTime()
        {
            var now = DateTime.UtcNow;
            RegistersCollection.Write(0x00, ToBCD(now.Second));
            RegistersCollection.Write(0x01, ToBCD(now.Minute));
            RegistersCollection.Write(0x02, ToBCD(now.Hour));
            RegistersCollection.Write(0x03, (byte)((int)now.DayOfWeek + 1));
            RegistersCollection.Write(0x04, ToBCD(now.Day));
            RegistersCollection.Write(0x05, ToBCD(now.Month));
            RegistersCollection.Write(0x06, ToBCD(now.Year % 100));
        }

        private static byte ToBCD(int value)
        {
            return (byte)(((value / 10) << 4) | (value % 10));
        }

        private int registerAddress;
    }
}
