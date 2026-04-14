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
    // Vishay VEML7700 - ambient light sensor (I2C)
    public class VEML7700 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public VEML7700()
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
            Registers.AlsConf.Define(this);
            Registers.AlsWH.Define(this);
            Registers.AlsWL.Define(this);
            Registers.PowerSave.Define(this);
            Registers.AlsL.Define(this).WithValueField(0,8,out alsL,FieldMode.Read);
            Registers.AlsH.Define(this).WithValueField(0,8,out alsH,FieldMode.Read);
            Registers.WhiteL.Define(this).WithValueField(0,8,out whiteL,FieldMode.Read);
            Registers.WhiteH.Define(this).WithValueField(0,8,out whiteH,FieldMode.Read);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var raw = (ushort)(LuxValue / 0.0576);
            alsL.Value=(byte)(raw&0xFF); alsH.Value=(byte)(raw>>8);
            whiteL.Value=(byte)(raw&0xFF); whiteH.Value=(byte)(raw>>8);
        }

        private IValueRegisterField alsL,alsH,whiteL,whiteH;
        private int registerAddress;

        private enum Registers : int
        {
            AlsConf=0x00,AlsWH=0x01,AlsWL=0x02,PowerSave=0x03,
            AlsL=0x04,AlsH=0x05,WhiteL=0x06,WhiteH=0x07,
        }
    }
}
