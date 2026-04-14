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
    // Bosch BMI270 - 6-axis IMU (I2C), Chip ID=0x24
    public class BMI270 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public BMI270()
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
            Registers.ChipId.Define(this, 0x24);
            Registers.Status.Define(this, 0xF0);
            Registers.AccXL.Define(this).WithValueField(0, 8, out axL, FieldMode.Read);
            Registers.AccXH.Define(this).WithValueField(0, 8, out axH, FieldMode.Read);
            Registers.AccYL.Define(this).WithValueField(0, 8, out ayL, FieldMode.Read);
            Registers.AccYH.Define(this).WithValueField(0, 8, out ayH, FieldMode.Read);
            Registers.AccZL.Define(this).WithValueField(0, 8, out azL, FieldMode.Read);
            Registers.AccZH.Define(this).WithValueField(0, 8, out azH, FieldMode.Read);
            Registers.GyrXL.Define(this).WithValueField(0, 8, out gxL, FieldMode.Read);
            Registers.GyrXH.Define(this).WithValueField(0, 8, out gxH, FieldMode.Read);
            Registers.GyrYL.Define(this).WithValueField(0, 8, out gyL, FieldMode.Read);
            Registers.GyrYH.Define(this).WithValueField(0, 8, out gyH, FieldMode.Read);
            Registers.GyrZL.Define(this).WithValueField(0, 8, out gzL, FieldMode.Read);
            Registers.GyrZH.Define(this).WithValueField(0, 8, out gzH, FieldMode.Read);
            Registers.TempL.Define(this).WithValueField(0, 8, out tL, FieldMode.Read);
            Registers.TempH.Define(this).WithValueField(0, 8, out tH, FieldMode.Read);
            Registers.Cmd.Define(this).WithWriteCallback((_, val) => { if(val == 0xB6) Reset(); });
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var ax = (short)(AccelerationX * 16384m); axL.Value=(byte)(ax&0xFF); axH.Value=(byte)(ax>>8);
            var ay = (short)(AccelerationY * 16384m); ayL.Value=(byte)(ay&0xFF); ayH.Value=(byte)(ay>>8);
            var az = (short)(AccelerationZ * 16384m); azL.Value=(byte)(az&0xFF); azH.Value=(byte)(az>>8);
            var gx = (short)(AngularRateX * 131m); gxL.Value=(byte)(gx&0xFF); gxH.Value=(byte)(gx>>8);
            var gy = (short)(AngularRateY * 131m); gyL.Value=(byte)(gy&0xFF); gyH.Value=(byte)(gy>>8);
            var gz = (short)(AngularRateZ * 131m); gzL.Value=(byte)(gz&0xFF); gzH.Value=(byte)(gz>>8);
            var rawT = (short)((Temperature - 23m) * 512m);
            tL.Value=(byte)(rawT&0xFF); tH.Value=(byte)(rawT>>8);
        }

        private IValueRegisterField axL,axH,ayL,ayH,azL,azH,gxL,gxH,gyL,gyH,gzL,gzH,tL,tH;
        private int registerAddress;

        private enum Registers : int
        {
            ChipId=0x00, Status=0x1B,
            AccXL=0x12,AccXH=0x13,AccYL=0x14,AccYH=0x15,AccZL=0x16,AccZH=0x17,
            GyrXL=0x0C,GyrXH=0x0D,GyrYL=0x0E,GyrYH=0x0F,GyrZL=0x10,GyrZH=0x11,
            TempL=0x20,TempH=0x21,
            Cmd=0x7E,
        }
    }
}
