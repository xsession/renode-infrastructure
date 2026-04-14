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
    // TDK ICM-42688-P - high-performance 6-axis IMU (I2C), WHO_AM_I=0x47
    public class ICM42688P : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public ICM42688P()
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
        public decimal AccelerationX { get; set; }
        public decimal AccelerationY { get; set; }
        public decimal AccelerationZ { get; set; } = 1.0m;
        public decimal AngularRateX { get; set; }
        public decimal AngularRateY { get; set; }
        public decimal AngularRateZ { get; set; }
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.IntStatus.Define(this, 0x08);
            Registers.TempDataH.Define(this).WithValueField(0,8,out tH,FieldMode.Read);
            Registers.TempDataL.Define(this).WithValueField(0,8,out tL,FieldMode.Read);
            Registers.AccelXH.Define(this).WithValueField(0,8,out axH,FieldMode.Read);
            Registers.AccelXL.Define(this).WithValueField(0,8,out axL,FieldMode.Read);
            Registers.AccelYH.Define(this).WithValueField(0,8,out ayH,FieldMode.Read);
            Registers.AccelYL.Define(this).WithValueField(0,8,out ayL,FieldMode.Read);
            Registers.AccelZH.Define(this).WithValueField(0,8,out azH,FieldMode.Read);
            Registers.AccelZL.Define(this).WithValueField(0,8,out azL,FieldMode.Read);
            Registers.GyroXH.Define(this).WithValueField(0,8,out gxH,FieldMode.Read);
            Registers.GyroXL.Define(this).WithValueField(0,8,out gxL,FieldMode.Read);
            Registers.GyroYH.Define(this).WithValueField(0,8,out gyH,FieldMode.Read);
            Registers.GyroYL.Define(this).WithValueField(0,8,out gyL,FieldMode.Read);
            Registers.GyroZH.Define(this).WithValueField(0,8,out gzH,FieldMode.Read);
            Registers.GyroZL.Define(this).WithValueField(0,8,out gzL,FieldMode.Read);
            Registers.PwrMgmt0.Define(this);
            Registers.WhoAmI.Define(this, 0x47);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var ax=(short)(AccelerationX*16384m); axH.Value=(byte)(ax>>8); axL.Value=(byte)(ax&0xFF);
            var ay=(short)(AccelerationY*16384m); ayH.Value=(byte)(ay>>8); ayL.Value=(byte)(ay&0xFF);
            var az=(short)(AccelerationZ*16384m); azH.Value=(byte)(az>>8); azL.Value=(byte)(az&0xFF);
            var gx=(short)(AngularRateX*131m); gxH.Value=(byte)(gx>>8); gxL.Value=(byte)(gx&0xFF);
            var gy=(short)(AngularRateY*131m); gyH.Value=(byte)(gy>>8); gyL.Value=(byte)(gy&0xFF);
            var gz=(short)(AngularRateZ*131m); gzH.Value=(byte)(gz>>8); gzL.Value=(byte)(gz&0xFF);
            var rawT=(short)((Temperature-25m)*132.48m+25m*132.48m);
            tH.Value=(byte)(rawT>>8); tL.Value=(byte)(rawT&0xFF);
        }

        private IValueRegisterField axH,axL,ayH,ayL,azH,azL,gxH,gxL,gyH,gyL,gzH,gzL,tH,tL;
        private int registerAddress;

        private enum Registers : int
        {
            IntStatus=0x2D,
            TempDataH=0x1D,TempDataL=0x1E,
            AccelXH=0x1F,AccelXL=0x20,AccelYH=0x21,AccelYL=0x22,AccelZH=0x23,AccelZL=0x24,
            GyroXH=0x25,GyroXL=0x26,GyroYH=0x27,GyroYL=0x28,GyroZH=0x29,GyroZL=0x2A,
            PwrMgmt0=0x4E,
            WhoAmI=0x75,
        }
    }
}
