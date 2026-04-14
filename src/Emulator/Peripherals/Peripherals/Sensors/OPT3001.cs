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
    // TI OPT3001 - ambient light sensor (I2C), Manufacturer ID=0x5449
    public class OPT3001 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public OPT3001()
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

        public int LuxValue { get; set; } = 500;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.ResultMSB.Define(this).WithValueField(0,8,out resH,FieldMode.Read);
            Registers.ResultLSB.Define(this).WithValueField(0,8,out resL,FieldMode.Read);
            Registers.Configuration.Define(this, 0xC8);
            Registers.ManufacturerIdH.Define(this, 0x54);
            Registers.ManufacturerIdL.Define(this, 0x49);
            Registers.DeviceIdH.Define(this, 0x30);
            Registers.DeviceIdL.Define(this, 0x01);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // 12-bit mantissa, 4-bit exponent: result = mantissa << exponent * 0.01 lux
            var val = (ushort)LuxValue;
            var exp = 0; var mant = val * 100;
            while(mant > 0xFFF && exp < 11) { mant >>= 1; exp++; }
            resH.Value=(byte)(((exp&0xF)<<4)|((mant>>8)&0xF));
            resL.Value=(byte)(mant&0xFF);
        }

        private IValueRegisterField resH,resL;
        private int registerAddress;

        private enum Registers : int
        {
            ResultMSB=0x00,ResultLSB=0x01,
            Configuration=0x02,
            ManufacturerIdH=0x7E,ManufacturerIdL=0x7F,
            DeviceIdH=0x80,DeviceIdL=0x81,
        }
    }
}
