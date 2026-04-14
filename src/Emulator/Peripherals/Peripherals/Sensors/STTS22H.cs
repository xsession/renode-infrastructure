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
    // ST STTS22H - digital temperature sensor (I2C)
    public class STTS22H : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public STTS22H()
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
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.WhoAmI.Define(this, 0xA0);
            Registers.Status.Define(this, 0x01);
            Registers.DataL.Define(this).WithValueField(0,8,out dataL,FieldMode.Read);
            Registers.DataH.Define(this).WithValueField(0,8,out dataH,FieldMode.Read);
            Registers.Ctrl1.Define(this);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var raw = (short)(Temperature * 100m);
            dataL.Value = (byte)(raw & 0xFF);
            dataH.Value = (byte)((raw >> 8) & 0xFF);
        }

        private IValueRegisterField dataL, dataH;
        private int registerAddress;

        private enum Registers : int
        {
            WhoAmI=0x0F,Status=0x27,
            DataL=0x28,DataH=0x29,
            Ctrl1=0x20,
        }
    }
}
