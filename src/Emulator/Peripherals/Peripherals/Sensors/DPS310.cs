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
    // Infineon DPS310 - barometric pressure sensor (I2C), Product ID=0x10
    public class DPS310 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public DPS310()
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
        public decimal Pressure { get; set; } = 1013.25m;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.PsrB2.Define(this).WithValueField(0, 8, out pB2, FieldMode.Read);
            Registers.PsrB1.Define(this).WithValueField(0, 8, out pB1, FieldMode.Read);
            Registers.PsrB0.Define(this).WithValueField(0, 8, out pB0, FieldMode.Read);
            Registers.TmpB2.Define(this).WithValueField(0, 8, out tB2, FieldMode.Read);
            Registers.TmpB1.Define(this).WithValueField(0, 8, out tB1, FieldMode.Read);
            Registers.TmpB0.Define(this).WithValueField(0, 8, out tB0, FieldMode.Read);
            Registers.PrsCfg.Define(this);
            Registers.TmpCfg.Define(this, 0x80);
            Registers.MeasCfg.Define(this, 0xC0)
                .WithWriteCallback((_, __) => UpdateReadout());
            Registers.CfgReg.Define(this);
            Registers.ProductId.Define(this, 0x10);
            Registers.CoefSrc.Define(this, 0x80);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var rawP = (int)(Pressure * 100m);
            pB2.Value = (byte)((rawP >> 16) & 0xFF);
            pB1.Value = (byte)((rawP >> 8) & 0xFF);
            pB0.Value = (byte)(rawP & 0xFF);
            var rawT = (int)(Temperature * 100m);
            tB2.Value = (byte)((rawT >> 16) & 0xFF);
            tB1.Value = (byte)((rawT >> 8) & 0xFF);
            tB0.Value = (byte)(rawT & 0xFF);
        }

        private IValueRegisterField pB2, pB1, pB0, tB2, tB1, tB0;
        private int registerAddress;

        private enum Registers : int
        {
            PsrB2 = 0x00,
            PsrB1 = 0x01,
            PsrB0 = 0x02,
            TmpB2 = 0x03,
            TmpB1 = 0x04,
            TmpB0 = 0x05,
            PrsCfg = 0x06,
            TmpCfg = 0x07,
            MeasCfg = 0x08,
            CfgReg = 0x09,
            ProductId = 0x0D,
            CoefSrc = 0x28,
        }
    }
}
