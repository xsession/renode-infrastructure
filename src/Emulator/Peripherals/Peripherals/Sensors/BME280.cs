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
    public class BME280 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor, IHumiditySensor
    {
        public BME280()
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
            if(data.Length == 0)
            {
                this.Log(LogLevel.Warning, "Unexpected write with no data");
                return;
            }
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
        public ByteRegisterCollection RegistersCollection { get; }

        private void DefineRegisters()
        {
            // Calibration data (simplified defaults)
            for(var addr = 0x88; addr <= 0xA1; addr++)
            {
                ((Registers)addr).Define(this, (addr == 0x88) ? (byte)0x70 : (byte)0x6B);
            }
            for(var addr = 0xE1; addr <= 0xF0; addr++)
            {
                ((Registers)addr).Define(this, 0x00);
            }

            Registers.ChipId.Define(this, 0x60);
            Registers.Reset.Define(this)
                .WithWriteCallback((_, val) => { if(val == 0xB6) Reset(); });
            Registers.CtrlHum.Define(this)
                .WithValueField(0, 3, name: "osrs_h");
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

            // Pressure readout
            Registers.PressureMSB.Define(this)
                .WithValueField(0, 8, out pressureMSB, FieldMode.Read, name: "press_msb");
            Registers.PressureLSB.Define(this)
                .WithValueField(0, 8, out pressureLSB, FieldMode.Read, name: "press_lsb");
            Registers.PressureXLSB.Define(this)
                .WithValueField(0, 8, out pressureXLSB, FieldMode.Read, name: "press_xlsb");
            // Temperature readout
            Registers.TemperatureMSB.Define(this)
                .WithValueField(0, 8, out temperatureMSB, FieldMode.Read, name: "temp_msb");
            Registers.TemperatureLSB.Define(this)
                .WithValueField(0, 8, out temperatureLSB, FieldMode.Read, name: "temp_lsb");
            Registers.TemperatureXLSB.Define(this)
                .WithValueField(0, 8, out temperatureXLSB, FieldMode.Read, name: "temp_xlsb");
            // Humidity readout
            Registers.HumidityMSB.Define(this)
                .WithValueField(0, 8, out humidityMSB, FieldMode.Read, name: "hum_msb");
            Registers.HumidityLSB.Define(this)
                .WithValueField(0, 8, out humidityLSB, FieldMode.Read, name: "hum_lsb");
        }

        private void UpdateReadout()
        {
            // Simplified: encode temperature as 20-bit ADC value (T = raw/5120.0)
            var rawT = (int)(Temperature * 5120m);
            temperatureMSB.Value = (byte)((rawT >> 12) & 0xFF);
            temperatureLSB.Value = (byte)((rawT >> 4) & 0xFF);
            temperatureXLSB.Value = (byte)((rawT & 0x0F) << 4);

            // Pressure as 20-bit raw (P = raw/256.0 Pa)
            var rawP = (int)(Pressure * 256m / 101325m * 0x50000);
            pressureMSB.Value = (byte)((rawP >> 12) & 0xFF);
            pressureLSB.Value = (byte)((rawP >> 4) & 0xFF);
            pressureXLSB.Value = (byte)((rawP & 0x0F) << 4);

            // Humidity as 16-bit raw (H = raw/1024.0 %RH)
            var rawH = (int)(Humidity * 1024m);
            humidityMSB.Value = (byte)((rawH >> 8) & 0xFF);
            humidityLSB.Value = (byte)(rawH & 0xFF);
        }

        private IValueRegisterField pressureMSB, pressureLSB, pressureXLSB;
        private IValueRegisterField temperatureMSB, temperatureLSB, temperatureXLSB;
        private IValueRegisterField humidityMSB, humidityLSB;
        private byte registerAddress;

        private enum Registers : byte
        {
            // Calibration 0x88-0xA1, 0xE1-0xF0
            ChipId = 0xD0,
            Reset = 0xE0,
            CtrlHum = 0xF2,
            Status = 0xF3,
            CtrlMeas = 0xF4,
            Config = 0xF5,
            PressureMSB = 0xF7,
            PressureLSB = 0xF8,
            PressureXLSB = 0xF9,
            TemperatureMSB = 0xFA,
            TemperatureLSB = 0xFB,
            TemperatureXLSB = 0xFC,
            HumidityMSB = 0xFD,
            HumidityLSB = 0xFE,
        }
    }
}
