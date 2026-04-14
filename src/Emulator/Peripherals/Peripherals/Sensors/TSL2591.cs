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
    // ams TSL2591 - high-dynamic-range light sensor (I2C), Device ID=0x50
    public class TSL2591 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public TSL2591()
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

        public int FullSpectrum { get; set; } = 500;
        public int Infrared { get; set; } = 100;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.Enable.Define(this, 0x01);
            Registers.Config.Define(this);
            Registers.Status.Define(this, 0x01);
            Registers.Ch0L.Define(this).WithValueField(0,8,out ch0L,FieldMode.Read);
            Registers.Ch0H.Define(this).WithValueField(0,8,out ch0H,FieldMode.Read);
            Registers.Ch1L.Define(this).WithValueField(0,8,out ch1L,FieldMode.Read);
            Registers.Ch1H.Define(this).WithValueField(0,8,out ch1H,FieldMode.Read);
            Registers.Id.Define(this, 0x50);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            ch0L.Value=(byte)(FullSpectrum&0xFF); ch0H.Value=(byte)(FullSpectrum>>8);
            ch1L.Value=(byte)(Infrared&0xFF); ch1H.Value=(byte)(Infrared>>8);
        }

        private IValueRegisterField ch0L,ch0H,ch1L,ch1H;
        private int registerAddress;

        private enum Registers : int
        {
            Enable=0x00,Config=0x01,Status=0x13,
            Ch0L=0x14,Ch0H=0x15,Ch1L=0x16,Ch1H=0x17,
            Id=0x12,
        }
    }
}
