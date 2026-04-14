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
    // ScioSense ENS160 - digital metal oxide multi-gas sensor (I2C), Part ID=0x0160
    public class ENS160 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public ENS160()
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

        public int eCO2 { get; set; } = 400;
        public int TVOC { get; set; } = 0;
        public int AQI { get; set; } = 1;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.PartIdL.Define(this, 0x60);
            Registers.PartIdH.Define(this, 0x01);
            Registers.OpMode.Define(this);
            Registers.DeviceStatus.Define(this, 0x80);
            Registers.DataAQI.Define(this).WithValueField(0,8,out aqiReg,FieldMode.Read);
            Registers.DataTVOCL.Define(this).WithValueField(0,8,out tvocL,FieldMode.Read);
            Registers.DataTVOCH.Define(this).WithValueField(0,8,out tvocH,FieldMode.Read);
            Registers.DataECO2L.Define(this).WithValueField(0,8,out eco2L,FieldMode.Read);
            Registers.DataECO2H.Define(this).WithValueField(0,8,out eco2H,FieldMode.Read);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            aqiReg.Value=(byte)AQI;
            tvocL.Value=(byte)(TVOC&0xFF); tvocH.Value=(byte)(TVOC>>8);
            eco2L.Value=(byte)(eCO2&0xFF); eco2H.Value=(byte)(eCO2>>8);
        }

        private IValueRegisterField aqiReg,tvocL,tvocH,eco2L,eco2H;
        private int registerAddress;

        private enum Registers : int
        {
            PartIdL=0x00,PartIdH=0x01,OpMode=0x10,
            DeviceStatus=0x20,DataAQI=0x21,
            DataTVOCL=0x22,DataTVOCH=0x23,
            DataECO2L=0x24,DataECO2H=0x25,
        }
    }
}
