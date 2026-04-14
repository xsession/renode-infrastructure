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
    // QST QMC5883L - 3-axis magnetometer (I2C), Chip ID=0xFF
    public class QMC5883L : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, IMagneticSensor
    {
        public QMC5883L()
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
            Registers.DataXL.Define(this).WithValueField(0,8,out xL,FieldMode.Read);
            Registers.DataXH.Define(this).WithValueField(0,8,out xH,FieldMode.Read);
            Registers.DataYL.Define(this).WithValueField(0,8,out yL,FieldMode.Read);
            Registers.DataYH.Define(this).WithValueField(0,8,out yH,FieldMode.Read);
            Registers.DataZL.Define(this).WithValueField(0,8,out zL,FieldMode.Read);
            Registers.DataZH.Define(this).WithValueField(0,8,out zH,FieldMode.Read);
            Registers.Status.Define(this, 0x01);
            Registers.Control1.Define(this);
            Registers.Control2.Define(this);
            Registers.ChipId.Define(this, 0xFF);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var mx = (short)(MagneticFluxDensityX / 100000.0 * 3000);
            var my = (short)(MagneticFluxDensityY / 100000.0 * 3000);
            var mz = (short)(MagneticFluxDensityZ / 100000.0 * 3000);
            xL.Value=(byte)(mx&0xFF); xH.Value=(byte)(mx>>8);
            yL.Value=(byte)(my&0xFF); yH.Value=(byte)(my>>8);
            zL.Value=(byte)(mz&0xFF); zH.Value=(byte)(mz>>8);
        }

        private IValueRegisterField xL,xH,yL,yH,zL,zH;
        private int registerAddress;

        private enum Registers : int
        {
            DataXL=0x00,DataXH=0x01,DataYL=0x02,DataYH=0x03,
            DataZL=0x04,DataZH=0x05,Status=0x06,
            Control1=0x09,Control2=0x0A,ChipId=0x0D,
        }
    }
}
