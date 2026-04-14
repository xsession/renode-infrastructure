//
// Copyright (c) 2010-2025 Antmicro
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System.Linq;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.Sensor;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Sensors
{
    // TI HDC1080 – humidity and temperature sensor (I2C, default addr 0x40)
    // Pointer-register based: write 1-byte register pointer, read 2 or 4 bytes
    public class HDC1080 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor, IHumiditySensor
    {
        public HDC1080()
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
            if(data.Length == 1) UpdateReadout();
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
        public decimal Humidity { get; set; } = 50.0m;
        public ByteRegisterCollection RegistersCollection { get; }

        private void DefineRegisters()
        {
            Registers.TemperatureMSB.Define(this).WithValueField(0, 8, out tempMSB, FieldMode.Read);
            Registers.TemperatureLSB.Define(this).WithValueField(0, 8, out tempLSB, FieldMode.Read);
            Registers.HumidityMSB.Define(this).WithValueField(0, 8, out humMSB, FieldMode.Read);
            Registers.HumidityLSB.Define(this).WithValueField(0, 8, out humLSB, FieldMode.Read);
            Registers.Configuration.Define(this, 0x10).WithValueField(0, 8, name: "config");
            Registers.ManufacturerIdMSB.Define(this, 0x54);
            Registers.ManufacturerIdLSB.Define(this, 0x49);
            Registers.DeviceIdMSB.Define(this, 0x10);
            Registers.DeviceIdLSB.Define(this, 0x50);
        }

        private void UpdateReadout()
        {
            var rawT = (ushort)(Temperature / 165m * 65536m + 40m / 165m * 65536m);
            tempMSB.Value = (byte)(rawT >> 8);
            tempLSB.Value = (byte)(rawT & 0xFF);
            var rawH = (ushort)(Humidity / 100m * 65536m);
            humMSB.Value = (byte)(rawH >> 8);
            humLSB.Value = (byte)(rawH & 0xFF);
        }

        private IValueRegisterField tempMSB, tempLSB, humMSB, humLSB;
        private byte registerAddress;

        private enum Registers : byte
        {
            TemperatureMSB = 0x00,
            TemperatureLSB = 0x01,
            HumidityMSB = 0x02,
            HumidityLSB = 0x03,
            Configuration = 0x04,
            ManufacturerIdMSB = 0xFC,
            ManufacturerIdLSB = 0xFD,
            DeviceIdMSB = 0xFE,
            DeviceIdLSB = 0xFF,
        }
    }
}
