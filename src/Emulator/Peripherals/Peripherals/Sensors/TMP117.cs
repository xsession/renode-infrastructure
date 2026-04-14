//
// Copyright (c) 2010-2025 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using System.Linq;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.Sensor;
using Antmicro.Renode.Utilities;


namespace Antmicro.Renode.Peripherals.Sensors
{
    // TI TMP117 - high-precision temperature sensor (I2C), 16-bit 0.0078125 C/LSB
    public class TMP117 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public TMP117()
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
            var result = new byte[count];
            for(var i = 0; i < count; i++)
            {
                result[i] = RegistersCollection.Read(registerAddress);
                registerAddress++;
            }
            return result;
        }

        public void FinishTransmission() { }

        public decimal Temperature { get; set; } = 25.0m;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.TempResultMSB.Define(this).WithValueField(0, 8, out tempMSB, FieldMode.Read);
            Registers.TempResultLSB.Define(this).WithValueField(0, 8, out tempLSB, FieldMode.Read);
            Registers.ConfigurationMSB.Define(this, 0x02);
            Registers.ConfigurationLSB.Define(this, 0x20);
            Registers.DeviceIdMSB.Define(this, 0x01);
            Registers.DeviceIdLSB.Define(this, 0x17);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var raw = (short)(Temperature / 0.0078125m);
            tempMSB.Value = (byte)((raw >> 8) & 0xFF);
            tempLSB.Value = (byte)(raw & 0xFF);
        }

        private IValueRegisterField tempMSB, tempLSB;
        private int registerAddress;

        private enum Registers : int
        {
            TempResultMSB = 0x00,
            TempResultLSB = 0x01,
            ConfigurationMSB = 0x02,
            ConfigurationLSB = 0x03,
            DeviceIdMSB = 0x0E,
            DeviceIdLSB = 0x0F,
        }
    }
}
