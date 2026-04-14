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
    // InvenSense MPU-6050 - 6-axis IMU (I2C), WHO_AM_I=0x68
    public class MPU6050 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public MPU6050()
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
            Registers.SmprtDiv.Define(this);
            Registers.Config.Define(this);
            Registers.GyroConfig.Define(this);
            Registers.AccelConfig.Define(this);
            Registers.IntEnable.Define(this);
            Registers.IntStatus.Define(this, 0x01);
            Registers.AccelXOutH.Define(this).WithValueField(0, 8, out axH, FieldMode.Read);
            Registers.AccelXOutL.Define(this).WithValueField(0, 8, out axL, FieldMode.Read);
            Registers.AccelYOutH.Define(this).WithValueField(0, 8, out ayH, FieldMode.Read);
            Registers.AccelYOutL.Define(this).WithValueField(0, 8, out ayL, FieldMode.Read);
            Registers.AccelZOutH.Define(this).WithValueField(0, 8, out azH, FieldMode.Read);
            Registers.AccelZOutL.Define(this).WithValueField(0, 8, out azL, FieldMode.Read);
            Registers.TempOutH.Define(this).WithValueField(0, 8, out tH, FieldMode.Read);
            Registers.TempOutL.Define(this).WithValueField(0, 8, out tL, FieldMode.Read);
            Registers.GyroXOutH.Define(this).WithValueField(0, 8, out gxH, FieldMode.Read);
            Registers.GyroXOutL.Define(this).WithValueField(0, 8, out gxL, FieldMode.Read);
            Registers.GyroYOutH.Define(this).WithValueField(0, 8, out gyH, FieldMode.Read);
            Registers.GyroYOutL.Define(this).WithValueField(0, 8, out gyL, FieldMode.Read);
            Registers.GyroZOutH.Define(this).WithValueField(0, 8, out gzH, FieldMode.Read);
            Registers.GyroZOutL.Define(this).WithValueField(0, 8, out gzL, FieldMode.Read);
            Registers.PwrMgmt1.Define(this, 0x40);
            Registers.PwrMgmt2.Define(this);
            Registers.WhoAmI.Define(this, 0x68);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // Accel: +/-2g default => 16384 LSB/g
            var ax = (short)(AccelerationX * 16384m); var ay = (short)(AccelerationY * 16384m); var az = (short)(AccelerationZ * 16384m);
            axH.Value = (byte)(ax >> 8); axL.Value = (byte)(ax & 0xFF);
            ayH.Value = (byte)(ay >> 8); ayL.Value = (byte)(ay & 0xFF);
            azH.Value = (byte)(az >> 8); azL.Value = (byte)(az & 0xFF);
            // Gyro: +/-250 dps default => 131 LSB/(deg/s)
            var gx = (short)(AngularRateX * 131m); var gy = (short)(AngularRateY * 131m); var gz = (short)(AngularRateZ * 131m);
            gxH.Value = (byte)(gx >> 8); gxL.Value = (byte)(gx & 0xFF);
            gyH.Value = (byte)(gy >> 8); gyL.Value = (byte)(gy & 0xFF);
            gzH.Value = (byte)(gz >> 8); gzL.Value = (byte)(gz & 0xFF);
            // Temp: T_degC = (raw/340) + 36.53
            var rawT = (short)((Temperature - 36.53m) * 340m);
            tH.Value = (byte)(rawT >> 8); tL.Value = (byte)(rawT & 0xFF);
        }

        private IValueRegisterField axH, axL, ayH, ayL, azH, azL;
        private IValueRegisterField tH, tL;
        private IValueRegisterField gxH, gxL, gyH, gyL, gzH, gzL;
        private int registerAddress;

        private enum Registers : int
        {
            SmprtDiv = 0x19, Config = 0x1A, GyroConfig = 0x1B, AccelConfig = 0x1C,
            IntEnable = 0x38, IntStatus = 0x3A,
            AccelXOutH = 0x3B, AccelXOutL = 0x3C,
            AccelYOutH = 0x3D, AccelYOutL = 0x3E,
            AccelZOutH = 0x3F, AccelZOutL = 0x40,
            TempOutH = 0x41, TempOutL = 0x42,
            GyroXOutH = 0x43, GyroXOutL = 0x44,
            GyroYOutH = 0x45, GyroYOutL = 0x46,
            GyroZOutH = 0x47, GyroZOutL = 0x48,
            PwrMgmt1 = 0x6B, PwrMgmt2 = 0x6C,
            WhoAmI = 0x75,
        }
    }
}
