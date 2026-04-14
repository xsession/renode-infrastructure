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
    // Bosch BMI088 - accelerometer (I2C)
    public class BMI088_Accel : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public BMI088_Accel()
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
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.ChipId.Define(this, 0x1E);
            Registers.Status.Define(this, 0x0F);
            Registers.DataXL.Define(this).WithValueField(0,8,out axL,FieldMode.Read);
            Registers.DataXH.Define(this).WithValueField(0,8,out axH,FieldMode.Read);
            Registers.DataYL.Define(this).WithValueField(0,8,out ayL,FieldMode.Read);
            Registers.DataYH.Define(this).WithValueField(0,8,out ayH,FieldMode.Read);
            Registers.DataZL.Define(this).WithValueField(0,8,out azL,FieldMode.Read);
            Registers.DataZH.Define(this).WithValueField(0,8,out azH,FieldMode.Read);
            Registers.TempL.Define(this).WithValueField(0,8,out tL,FieldMode.Read);
            Registers.TempH.Define(this).WithValueField(0,8,out tH,FieldMode.Read);
            Registers.Ctrl1.Define(this);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var ax=(short)(AccelerationX*16384m); axL.Value=(byte)(ax&0xFF); axH.Value=(byte)(ax>>8);
            var ay=(short)(AccelerationY*16384m); ayL.Value=(byte)(ay&0xFF); ayH.Value=(byte)(ay>>8);
            var az=(short)(AccelerationZ*16384m); azL.Value=(byte)(az&0xFF); azH.Value=(byte)(az>>8);
            var rawT=(short)(Temperature*256m); tL.Value=(byte)(rawT&0xFF); tH.Value=(byte)(rawT>>8);
        }

        private IValueRegisterField axL,axH,ayL,ayH,azL,azH,tL,tH;
        private int registerAddress;

        private enum Registers : int
        {
            ChipId=0x00,Status=0x1E,
            DataXL=0x28,DataXH=0x29,DataYL=0x2A,DataYH=0x2B,DataZL=0x2C,DataZH=0x2D,
            TempL=0x20,TempH=0x21,
            Ctrl1=0x10,
        }
    }
}
