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
    // Honeywell HMC5883L - 3-axis magnetometer (I2C)
    public class HMC5883L : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, IMagneticSensor
    {
        public HMC5883L()
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

        public int MagneticFluxDensityX { get; set; }
        public int MagneticFluxDensityY { get; set; }
        public int MagneticFluxDensityZ { get; set; }
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.ConfigA.Define(this, 0x10);
            Registers.ConfigB.Define(this, 0x20);
            Registers.Mode.Define(this, 0x01);
            Registers.DataXH.Define(this).WithValueField(0,8,out xH,FieldMode.Read);
            Registers.DataXL.Define(this).WithValueField(0,8,out xL,FieldMode.Read);
            Registers.DataZH.Define(this).WithValueField(0,8,out zH,FieldMode.Read);
            Registers.DataZL.Define(this).WithValueField(0,8,out zL,FieldMode.Read);
            Registers.DataYH.Define(this).WithValueField(0,8,out yH,FieldMode.Read);
            Registers.DataYL.Define(this).WithValueField(0,8,out yL,FieldMode.Read);
            Registers.Status.Define(this, 0x01);
            Registers.IdA.Define(this, 0x48);
            Registers.IdB.Define(this, 0x34);
            Registers.IdC.Define(this, 0x33);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // 1090 LSB/Gauss default, nT to Gauss: 1 Gauss = 100000 nT
            var mx = (short)(MagneticFluxDensityX / 100000.0 * 1090);
            var my = (short)(MagneticFluxDensityY / 100000.0 * 1090);
            var mz = (short)(MagneticFluxDensityZ / 100000.0 * 1090);
            xH.Value=(byte)(mx>>8); xL.Value=(byte)(mx&0xFF);
            yH.Value=(byte)(my>>8); yL.Value=(byte)(my&0xFF);
            zH.Value=(byte)(mz>>8); zL.Value=(byte)(mz&0xFF);
        }

        private IValueRegisterField xH,xL,yH,yL,zH,zL;
        private int registerAddress;

        private enum Registers : int
        {
            ConfigA=0x00,ConfigB=0x01,Mode=0x02,
            DataXH=0x03,DataXL=0x04,DataZH=0x05,DataZL=0x06,DataYH=0x07,DataYL=0x08,
            Status=0x09,IdA=0x0A,IdB=0x0B,IdC=0x0C,
        }
    }
}
