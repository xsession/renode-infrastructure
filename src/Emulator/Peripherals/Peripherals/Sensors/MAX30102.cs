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
    // Maxim MAX30102 - pulse oximeter and heart-rate sensor (I2C), Part ID=0x15
    public class MAX30102 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public MAX30102()
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

        public int HeartRate { get; set; } = 72;
        public int SpO2 { get; set; } = 98;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.IntStatus1.Define(this, 0x40);
            Registers.IntStatus2.Define(this);
            Registers.FifoWrPtr.Define(this);
            Registers.OvfCounter.Define(this);
            Registers.FifoRdPtr.Define(this);
            Registers.FifoData.Define(this).WithValueField(0,8,out fifoData,FieldMode.Read);
            Registers.ModeConfig.Define(this).WithWriteCallback((_,val) => { if((val&0x40)!=0) Reset(); });
            Registers.SpO2Config.Define(this);
            Registers.RevId.Define(this, 0x06);
            Registers.PartId.Define(this, 0x15);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            fifoData.Value = (byte)(SpO2 & 0xFF);
        }

        private IValueRegisterField fifoData;
        private int registerAddress;

        private enum Registers : int
        {
            IntStatus1=0x00,IntStatus2=0x01,
            FifoWrPtr=0x04,OvfCounter=0x05,FifoRdPtr=0x06,FifoData=0x07,
            ModeConfig=0x09,SpO2Config=0x0A,
            RevId=0xFE,PartId=0xFF,
        }
    }
}
