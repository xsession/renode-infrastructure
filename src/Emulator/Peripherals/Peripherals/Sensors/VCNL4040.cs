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
    // Vishay VCNL4040 - proximity and ambient light sensor (I2C), ID=0x0186
    public class VCNL4040 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public VCNL4040()
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

        public int Proximity { get; set; } = 100;
        public int AmbientLight { get; set; } = 500;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.AlsConf.Define(this);
            Registers.PsConf1.Define(this);
            Registers.PsDataL.Define(this).WithValueField(0,8,out psL,FieldMode.Read);
            Registers.PsDataH.Define(this).WithValueField(0,8,out psH,FieldMode.Read);
            Registers.AlsDataL.Define(this).WithValueField(0,8,out alsL,FieldMode.Read);
            Registers.AlsDataH.Define(this).WithValueField(0,8,out alsH,FieldMode.Read);
            Registers.DeviceIdL.Define(this, 0x86);
            Registers.DeviceIdH.Define(this, 0x01);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            psL.Value=(byte)(Proximity&0xFF); psH.Value=(byte)(Proximity>>8);
            alsL.Value=(byte)(AmbientLight&0xFF); alsH.Value=(byte)(AmbientLight>>8);
        }

        private IValueRegisterField psL,psH,alsL,alsH;
        private int registerAddress;

        private enum Registers : int
        {
            AlsConf=0x00,PsConf1=0x03,
            PsDataL=0x08,PsDataH=0x09,
            AlsDataL=0x0A,AlsDataH=0x0B,
            DeviceIdL=0x0C,DeviceIdH=0x0D,
        }
    }
}
