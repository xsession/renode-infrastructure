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
    // Bosch BME680 – gas, pressure, temperature, humidity sensor (I2C)
    // Chip ID 0x61
    public class BME680 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor, IHumiditySensor
    {
        public BME680()
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
        public decimal Humidity { get; set; } = 50.0m;
        public decimal Pressure { get; set; } = 101325m;
        public int GasResistance { get; set; } = 50000;
        public ByteRegisterCollection RegistersCollection { get; }

        private void DefineRegisters()
        {
            Registers.ChipId.Define(this, 0x61);
            Registers.Reset.Define(this)
                .WithWriteCallback((_, val) => { if(val == 0xB6) Reset(); });
            Registers.CtrlHum.Define(this)
                .WithValueField(0, 3, name: "osrs_h");
            Registers.CtrlMeas.Define(this)
                .WithValueField(0, 2, name: "mode")
                .WithValueField(2, 3, name: "osrs_p")
                .WithValueField(5, 3, name: "osrs_t")
                .WithWriteCallback((_, __) => UpdateReadout());
            Registers.CtrlGas1.Define(this)
                .WithValueField(0, 4, name: "nb_conv")
                .WithFlag(4, name: "run_gas");
            Registers.Config.Define(this)
                .WithValueField(2, 3, name: "filter");
            Registers.EASStatus.Define(this, 0x80)
                .WithFlag(7, FieldMode.Read, name: "new_data");

            Registers.PressureMSB.Define(this).WithValueField(0, 8, out pressureMSB, FieldMode.Read);
            Registers.PressureLSB.Define(this).WithValueField(0, 8, out pressureLSB, FieldMode.Read);
            Registers.PressureXLSB.Define(this).WithValueField(0, 8, out pressureXLSB, FieldMode.Read);
            Registers.TemperatureMSB.Define(this).WithValueField(0, 8, out temperatureMSB, FieldMode.Read);
            Registers.TemperatureLSB.Define(this).WithValueField(0, 8, out temperatureLSB, FieldMode.Read);
            Registers.TemperatureXLSB.Define(this).WithValueField(0, 8, out temperatureXLSB, FieldMode.Read);
            Registers.HumidityMSB.Define(this).WithValueField(0, 8, out humidityMSB, FieldMode.Read);
            Registers.HumidityLSB.Define(this).WithValueField(0, 8, out humidityLSB, FieldMode.Read);
            Registers.GasResistanceMSB.Define(this).WithValueField(0, 8, out gasResMSB, FieldMode.Read);
            Registers.GasResistanceLSB.Define(this).WithValueField(0, 8, out gasResLSB, FieldMode.Read);
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

            var rawH = (int)(Humidity * 1024m);
            humidityMSB.Value = (byte)((rawH >> 8) & 0xFF);
            humidityLSB.Value = (byte)(rawH & 0xFF);

            // Gas resistance as 10-bit value
            var rawG = GasResistance / 100;
            gasResMSB.Value = (byte)((rawG >> 2) & 0xFF);
            gasResLSB.Value = (byte)((rawG & 0x03) << 6);
        }

        private IValueRegisterField pressureMSB, pressureLSB, pressureXLSB;
        private IValueRegisterField temperatureMSB, temperatureLSB, temperatureXLSB;
        private IValueRegisterField humidityMSB, humidityLSB;
        private IValueRegisterField gasResMSB, gasResLSB;
        private byte registerAddress;

        private enum Registers : byte
        {
            EASStatus = 0x1D,
            PressureMSB = 0x1F,
            PressureLSB = 0x20,
            PressureXLSB = 0x21,
            TemperatureMSB = 0x22,
            TemperatureLSB = 0x23,
            TemperatureXLSB = 0x24,
            HumidityMSB = 0x25,
            HumidityLSB = 0x26,
            GasResistanceMSB = 0x2A,
            GasResistanceLSB = 0x2B,
            CtrlGas1 = 0x71,
            CtrlHum = 0x72,
            CtrlMeas = 0x74,
            Config = 0x75,
            ChipId = 0xD0,
            Reset = 0xE0,
        }
    }
}
