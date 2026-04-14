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
    // ST LPS22HB - MEMS pressure sensor (I2C), WHO_AM_I=0xB1
    public class LPS22HB : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public LPS22HB()
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
            Registers.WhoAmI.Define(this, 0xB1);
            Registers.CtrlReg1.Define(this).WithValueField(0, 8, name: "ctrl_reg1");
            Registers.CtrlReg2.Define(this).WithValueField(0, 8, name: "ctrl_reg2")
                .WithWriteCallback((_, val) => { if((val & 0x04) != 0) Reset(); if((val & 0x01) != 0) UpdateReadout(); });
            Registers.StatusReg.Define(this, 0x03);
            Registers.PressOutXL.Define(this).WithValueField(0, 8, out pXL, FieldMode.Read);
            Registers.PressOutL.Define(this).WithValueField(0, 8, out pL, FieldMode.Read);
            Registers.PressOutH.Define(this).WithValueField(0, 8, out pH, FieldMode.Read);
            Registers.TempOutL.Define(this).WithValueField(0, 8, out tL, FieldMode.Read);
            Registers.TempOutH.Define(this).WithValueField(0, 8, out tH, FieldMode.Read);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // Pressure in hPa, output is 4096 LSB/hPa (24-bit)
            var rawP = (int)(Pressure * 4096m);
            pXL.Value = (byte)(rawP & 0xFF);
            pL.Value  = (byte)((rawP >> 8) & 0xFF);
            pH.Value  = (byte)((rawP >> 16) & 0xFF);
            // Temperature: 100 LSB/C (16-bit signed)
            var rawT = (short)(Temperature * 100m);
            tL.Value = (byte)(rawT & 0xFF);
            tH.Value = (byte)((rawT >> 8) & 0xFF);
        }

        private IValueRegisterField pXL, pL, pH, tL, tH;
        private int registerAddress;

        private enum Registers : int
        {
            WhoAmI = 0x0F,
            CtrlReg1 = 0x10,
            CtrlReg2 = 0x11,
            StatusReg = 0x27,
            PressOutXL = 0x28,
            PressOutL = 0x29,
            PressOutH = 0x2A,
            TempOutL = 0x2B,
            TempOutH = 0x2C,
        }
    }
}
