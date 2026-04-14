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
    // ams CCS811 - indoor air quality sensor (I2C), HW_ID=0x81
    public class CCS811 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public CCS811()
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
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.Status.Define(this, 0x98);
            Registers.MeasMode.Define(this);
            Registers.AlgResultECO2H.Define(this).WithValueField(0,8,out eco2H,FieldMode.Read);
            Registers.AlgResultECO2L.Define(this).WithValueField(0,8,out eco2L,FieldMode.Read);
            Registers.AlgResultTVOCH.Define(this).WithValueField(0,8,out tvocH,FieldMode.Read);
            Registers.AlgResultTVOCL.Define(this).WithValueField(0,8,out tvocL,FieldMode.Read);
            Registers.HwId.Define(this, 0x81);
            Registers.HwVersion.Define(this, 0x1A);
            Registers.SwReset.Define(this).WithWriteCallback((_,__) => Reset());
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            eco2H.Value=(byte)((eCO2>>8)&0xFF); eco2L.Value=(byte)(eCO2&0xFF);
            tvocH.Value=(byte)((TVOC>>8)&0xFF); tvocL.Value=(byte)(TVOC&0xFF);
        }

        private IValueRegisterField eco2H,eco2L,tvocH,tvocL;
        private int registerAddress;

        private enum Registers : int
        {
            Status=0x00,MeasMode=0x01,
            AlgResultECO2H=0x02,AlgResultECO2L=0x03,AlgResultTVOCH=0x04,AlgResultTVOCL=0x05,
            HwId=0x20,HwVersion=0x21,
            SwReset=0xFF,
        }
    }
}
