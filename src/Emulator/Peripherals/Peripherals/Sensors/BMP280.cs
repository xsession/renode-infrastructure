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
    // Bosch BMP280 – digital pressure and temperature sensor (I2C)
    // Chip ID 0x58, register-compatible with BME280 minus humidity
    public class BMP280 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public BMP280()
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
        public decimal Pressure { get; set; } = 101325m;
        public ByteRegisterCollection RegistersCollection { get; }

        private void DefineRegisters()
        {
            Registers.ChipId.Define(this, 0x58);
            Registers.Reset.Define(this)
                .WithWriteCallback((_, val) => { if(val == 0xB6) Reset(); });
            Registers.Status.Define(this)
                .WithFlag(0, FieldMode.Read, name: "im_update")
                .WithFlag(3, FieldMode.Read, name: "measuring");
            Registers.CtrlMeas.Define(this)
                .WithValueField(0, 2, name: "mode")
                .WithValueField(2, 3, name: "osrs_p")
                .WithValueField(5, 3, name: "osrs_t")
                .WithWriteCallback((_, __) => UpdateReadout());
            Registers.Config.Define(this)
                .WithValueField(0, 1, name: "spi3w_en")
                .WithValueField(2, 3, name: "filter")
                .WithValueField(5, 3, name: "t_sb");

            Registers.PressureMSB.Define(this).WithValueField(0, 8, out pressureMSB, FieldMode.Read);
            Registers.PressureLSB.Define(this).WithValueField(0, 8, out pressureLSB, FieldMode.Read);
            Registers.PressureXLSB.Define(this).WithValueField(0, 8, out pressureXLSB, FieldMode.Read);
            Registers.TemperatureMSB.Define(this).WithValueField(0, 8, out temperatureMSB, FieldMode.Read);
            Registers.TemperatureLSB.Define(this).WithValueField(0, 8, out temperatureLSB, FieldMode.Read);
            Registers.TemperatureXLSB.Define(this).WithValueField(0, 8, out temperatureXLSB, FieldMode.Read);
        }

        private void UpdateReadout()
        {
            var rawT = (int)(Temperature * 5120m);
            temperatureMSB.Value = (byte)((rawT >> 12) & 0xFF);
            temperatureLSB.Value = (byte)((rawT >> 4) & 0xFF);
            temperatureXLSB.Value = (byte)((rawT & 0x0F) << 4);

            var rawP = (int)(Pressure * 256m / 101325m * 0x50000);
            pressureMSB.Value = (byte)((rawP >> 12) & 0xFF);
            pressureLSB.Value = (byte)((rawP >> 4) & 0xFF);
            pressureXLSB.Value = (byte)((rawP & 0x0F) << 4);
        }

        private IValueRegisterField pressureMSB, pressureLSB, pressureXLSB;
        private IValueRegisterField temperatureMSB, temperatureLSB, temperatureXLSB;
        private byte registerAddress;

        private enum Registers : byte
        {
            ChipId = 0xD0,
            Reset = 0xE0,
            Status = 0xF3,
            CtrlMeas = 0xF4,
            Config = 0xF5,
            PressureMSB = 0xF7,
            PressureLSB = 0xF8,
            PressureXLSB = 0xF9,
            TemperatureMSB = 0xFA,
            TemperatureLSB = 0xFB,
            TemperatureXLSB = 0xFC,
        }
    }
}
