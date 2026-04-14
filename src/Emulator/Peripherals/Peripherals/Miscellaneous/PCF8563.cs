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
    // NXP PCF8563 - real-time clock/calendar (I2C)
    public class PCF8563 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public PCF8563()
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
                registerAddress = (registerAddress + 1) & 0x0F;
            }
        }

        public byte[] Read(int count)
        {
            UpdateTime();
            var result = new byte[count];
            for(var i = 0; i < count; i++)
            {
                result[i] = RegistersCollection.Read(registerAddress);
                registerAddress = (registerAddress + 1) & 0x0F;
            }
            return result;
        }

        public void FinishTransmission() { }

        public ByteRegisterCollection RegistersCollection { get; }

        private void DefineRegisters()
        {
            RegistersCollection.DefineRegister(0x00); // Control_status_1
            RegistersCollection.DefineRegister(0x01); // Control_status_2
            RegistersCollection.DefineRegister(0x02); // VL_seconds (BCD)
            RegistersCollection.DefineRegister(0x03); // Minutes (BCD)
            RegistersCollection.DefineRegister(0x04); // Hours (BCD)
            RegistersCollection.DefineRegister(0x05); // Days (BCD)
            RegistersCollection.DefineRegister(0x06); // Weekdays
            RegistersCollection.DefineRegister(0x07); // Century_months (BCD)
            RegistersCollection.DefineRegister(0x08); // Years (BCD)
            // Alarms 0x09-0x0C
            RegistersCollection.DefineRegister(0x09, 0x80);
            RegistersCollection.DefineRegister(0x0A, 0x80);
            RegistersCollection.DefineRegister(0x0B, 0x80);
            RegistersCollection.DefineRegister(0x0C, 0x80);
            // CLKOUT, Timer
            RegistersCollection.DefineRegister(0x0D, 0x83);
            RegistersCollection.DefineRegister(0x0E);
            RegistersCollection.DefineRegister(0x0F);
        }

        private void UpdateTime()
        {
            var now = DateTime.UtcNow;
            RegistersCollection.Write(0x02, (byte)(ToBCD(now.Second) & 0x7F));
            RegistersCollection.Write(0x03, (byte)(ToBCD(now.Minute) & 0x7F));
            RegistersCollection.Write(0x04, (byte)(ToBCD(now.Hour) & 0x3F));
            RegistersCollection.Write(0x05, (byte)(ToBCD(now.Day) & 0x3F));
            RegistersCollection.Write(0x06, (byte)((int)now.DayOfWeek & 0x07));
            RegistersCollection.Write(0x07, (byte)(ToBCD(now.Month) & 0x1F));
            RegistersCollection.Write(0x08, ToBCD(now.Year % 100));
        }

        private static byte ToBCD(int value)
        {
            return (byte)(((value / 10) << 4) | (value % 10));
        }

        private int registerAddress;
    }
}
