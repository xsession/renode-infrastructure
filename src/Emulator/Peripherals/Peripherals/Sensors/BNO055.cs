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
    // Bosch BNO055 - 9-axis absolute orientation sensor (I2C), Chip ID=0xA0
    public class BNO055 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public BNO055()
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
        public decimal EulerHeading { get; set; }
        public decimal EulerRoll { get; set; }
        public decimal EulerPitch { get; set; }
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.ChipId.Define(this, 0xA0);
            Registers.AccId.Define(this, 0xFB);
            Registers.MagId.Define(this, 0x32);
            Registers.GyrId.Define(this, 0x0F);
            Registers.SwRevLSB.Define(this, 0x08);
            Registers.SwRevMSB.Define(this, 0x03);
            Registers.PageId.Define(this);
            Registers.AccXL.Define(this).WithValueField(0,8,out axL,FieldMode.Read);
            Registers.AccXH.Define(this).WithValueField(0,8,out axH,FieldMode.Read);
            Registers.AccYL.Define(this).WithValueField(0,8,out ayL,FieldMode.Read);
            Registers.AccYH.Define(this).WithValueField(0,8,out ayH,FieldMode.Read);
            Registers.AccZL.Define(this).WithValueField(0,8,out azL,FieldMode.Read);
            Registers.AccZH.Define(this).WithValueField(0,8,out azH,FieldMode.Read);
            Registers.GyrXL.Define(this).WithValueField(0,8,out gxL,FieldMode.Read);
            Registers.GyrXH.Define(this).WithValueField(0,8,out gxH,FieldMode.Read);
            Registers.GyrYL.Define(this).WithValueField(0,8,out gyL,FieldMode.Read);
            Registers.GyrYH.Define(this).WithValueField(0,8,out gyH,FieldMode.Read);
            Registers.GyrZL.Define(this).WithValueField(0,8,out gzL,FieldMode.Read);
            Registers.GyrZH.Define(this).WithValueField(0,8,out gzH,FieldMode.Read);
            Registers.EulHL.Define(this).WithValueField(0,8,out ehL,FieldMode.Read);
            Registers.EulHH.Define(this).WithValueField(0,8,out ehH,FieldMode.Read);
            Registers.EulRL.Define(this).WithValueField(0,8,out erL,FieldMode.Read);
            Registers.EulRH.Define(this).WithValueField(0,8,out erH,FieldMode.Read);
            Registers.EulPL.Define(this).WithValueField(0,8,out epL,FieldMode.Read);
            Registers.EulPH.Define(this).WithValueField(0,8,out epH,FieldMode.Read);
            Registers.TempReg.Define(this).WithValueField(0,8,out tempReg,FieldMode.Read);
            Registers.CalibStat.Define(this, 0xFF);
            Registers.SysStat.Define(this, 0x05);
            Registers.OprMode.Define(this);
            Registers.SysTrigger.Define(this).WithWriteCallback((_,val) => { if((val & 0x20) != 0) Reset(); });
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var ax = (short)(AccelerationX * 100m); axL.Value=(byte)(ax&0xFF); axH.Value=(byte)(ax>>8);
            var ay = (short)(AccelerationY * 100m); ayL.Value=(byte)(ay&0xFF); ayH.Value=(byte)(ay>>8);
            var az = (short)(AccelerationZ * 100m); azL.Value=(byte)(az&0xFF); azH.Value=(byte)(az>>8);
            var gx = (short)(AngularRateX * 16m); gxL.Value=(byte)(gx&0xFF); gxH.Value=(byte)(gx>>8);
            var gy = (short)(AngularRateY * 16m); gyL.Value=(byte)(gy&0xFF); gyH.Value=(byte)(gy>>8);
            var gz = (short)(AngularRateZ * 16m); gzL.Value=(byte)(gz&0xFF); gzH.Value=(byte)(gz>>8);
            var eh = (short)(EulerHeading * 16m); ehL.Value=(byte)(eh&0xFF); ehH.Value=(byte)(eh>>8);
            var er = (short)(EulerRoll * 16m); erL.Value=(byte)(er&0xFF); erH.Value=(byte)(er>>8);
            var ep = (short)(EulerPitch * 16m); epL.Value=(byte)(ep&0xFF); epH.Value=(byte)(ep>>8);
            tempReg.Value = (byte)(sbyte)Temperature;
        }

        private IValueRegisterField axL,axH,ayL,ayH,azL,azH,gxL,gxH,gyL,gyH,gzL,gzH;
        private IValueRegisterField ehL,ehH,erL,erH,epL,epH,tempReg;
        private int registerAddress;

        private enum Registers : int
        {
            ChipId=0x00,AccId=0x01,MagId=0x02,GyrId=0x03,SwRevLSB=0x04,SwRevMSB=0x05,PageId=0x07,
            AccXL=0x08,AccXH=0x09,AccYL=0x0A,AccYH=0x0B,AccZL=0x0C,AccZH=0x0D,
            GyrXL=0x14,GyrXH=0x15,GyrYL=0x16,GyrYH=0x17,GyrZL=0x18,GyrZH=0x19,
            EulHL=0x1A,EulHH=0x1B,EulRL=0x1C,EulRH=0x1D,EulPL=0x1E,EulPH=0x1F,
            TempReg=0x34,CalibStat=0x35,SysStat=0x39,
            OprMode=0x3D,SysTrigger=0x3F,
        }
    }
}
