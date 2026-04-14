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
    // NXP MMA8451Q - 3-axis accelerometer (I2C)
    public class MMA8451 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public MMA8451()
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
            Registers.WhoAmI.Define(this, 0x1A);
            Registers.Status.Define(this, 0x0F);
            Registers.OutXH.Define(this).WithValueField(0,8,out axH,FieldMode.Read);
            Registers.OutXL.Define(this).WithValueField(0,8,out axL,FieldMode.Read);
            Registers.OutYH.Define(this).WithValueField(0,8,out ayH,FieldMode.Read);
            Registers.OutYL.Define(this).WithValueField(0,8,out ayL,FieldMode.Read);
            Registers.OutZH.Define(this).WithValueField(0,8,out azH,FieldMode.Read);
            Registers.OutZL.Define(this).WithValueField(0,8,out azL,FieldMode.Read);
            Registers.CtrlReg1.Define(this);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // 14-bit, +/-2g default => 4096 LSB/g
            var ax=(short)(AccelerationX*4096m); axH.Value=(byte)(ax>>8); axL.Value=(byte)(ax&0xFF);
            var ay=(short)(AccelerationY*4096m); ayH.Value=(byte)(ay>>8); ayL.Value=(byte)(ay&0xFF);
            var az=(short)(AccelerationZ*4096m); azH.Value=(byte)(az>>8); azL.Value=(byte)(az&0xFF);
        }

        private IValueRegisterField axH,axL,ayH,ayL,azH,azL;
        private int registerAddress;

        private enum Registers : int
        {
            WhoAmI=0x0D,
            Status=0x27,
            OutXH=0x29,OutXL=0x28,OutYH=0x2B,OutYL=0x2A,OutZH=0x2D,OutZL=0x2C,
            CtrlReg1=0x20,
        }
    }
}
