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
    // ST ISM330DHCX - 6-axis IMU (I2C)
    public class ISM330DHCX : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public ISM330DHCX()
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
            Registers.WhoAmI.Define(this, 0x6B);
            Registers.CtrlReg1XL.Define(this);
            Registers.CtrlReg2G.Define(this);
            Registers.StatusReg.Define(this, 0x07);
            Registers.TempOutL.Define(this).WithValueField(0,8,out tL,FieldMode.Read);
            Registers.TempOutH.Define(this).WithValueField(0,8,out tH,FieldMode.Read);
            Registers.GyroXL.Define(this).WithValueField(0,8,out gxL,FieldMode.Read);
            Registers.GyroXH.Define(this).WithValueField(0,8,out gxH,FieldMode.Read);
            Registers.GyroYL.Define(this).WithValueField(0,8,out gyL,FieldMode.Read);
            Registers.GyroYH.Define(this).WithValueField(0,8,out gyH,FieldMode.Read);
            Registers.GyroZL.Define(this).WithValueField(0,8,out gzL,FieldMode.Read);
            Registers.GyroZH.Define(this).WithValueField(0,8,out gzH,FieldMode.Read);
            Registers.AccXL.Define(this).WithValueField(0,8,out axL,FieldMode.Read);
            Registers.AccXH.Define(this).WithValueField(0,8,out axH,FieldMode.Read);
            Registers.AccYL.Define(this).WithValueField(0,8,out ayL,FieldMode.Read);
            Registers.AccYH.Define(this).WithValueField(0,8,out ayH,FieldMode.Read);
            Registers.AccZL.Define(this).WithValueField(0,8,out azL,FieldMode.Read);
            Registers.AccZH.Define(this).WithValueField(0,8,out azH,FieldMode.Read);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var ax=(short)(AccelerationX*16384m); axL.Value=(byte)(ax&0xFF); axH.Value=(byte)(ax>>8);
            var ay=(short)(AccelerationY*16384m); ayL.Value=(byte)(ay&0xFF); ayH.Value=(byte)(ay>>8);
            var az=(short)(AccelerationZ*16384m); azL.Value=(byte)(az&0xFF); azH.Value=(byte)(az>>8);
            var gx=(short)(AngularRateX*131m); gxL.Value=(byte)(gx&0xFF); gxH.Value=(byte)(gx>>8);
            var gy=(short)(AngularRateY*131m); gyL.Value=(byte)(gy&0xFF); gyH.Value=(byte)(gy>>8);
            var gz=(short)(AngularRateZ*131m); gzL.Value=(byte)(gz&0xFF); gzH.Value=(byte)(gz>>8);
            var rawT=(short)(Temperature*256m);
            tL.Value=(byte)(rawT&0xFF); tH.Value=(byte)(rawT>>8);
        }

        private IValueRegisterField axL,axH,ayL,ayH,azL,azH,gxL,gxH,gyL,gyH,gzL,gzH,tL,tH;
        private int registerAddress;

        private enum Registers : int
        {
            WhoAmI=0x0F,CtrlReg1XL=0x10,CtrlReg2G=0x11,StatusReg=0x1E,
            TempOutL=0x20,TempOutH=0x21,
            GyroXL=0x22,GyroXH=0x23,GyroYL=0x24,GyroYH=0x25,GyroZL=0x26,GyroZH=0x27,
            AccXL=0x28,AccXH=0x29,AccYL=0x2A,AccYH=0x2B,AccZL=0x2C,AccZH=0x2D,
        }
    }
}
