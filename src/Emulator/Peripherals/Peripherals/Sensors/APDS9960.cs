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
    // Broadcom APDS-9960 - RGB, proximity and gesture sensor (I2C), ID=0xAB
    public class APDS9960 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public APDS9960()
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
        public int Red { get; set; } = 500;
        public int Green { get; set; } = 500;
        public int Blue { get; set; } = 500;
        public int Clear { get; set; } = 1500;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.Enable.Define(this);
            Registers.Status.Define(this, 0x07);
            Registers.ClearL.Define(this).WithValueField(0,8,out cL,FieldMode.Read);
            Registers.ClearH.Define(this).WithValueField(0,8,out cH,FieldMode.Read);
            Registers.RedL.Define(this).WithValueField(0,8,out rL,FieldMode.Read);
            Registers.RedH.Define(this).WithValueField(0,8,out rH,FieldMode.Read);
            Registers.GreenL.Define(this).WithValueField(0,8,out gL,FieldMode.Read);
            Registers.GreenH.Define(this).WithValueField(0,8,out gH,FieldMode.Read);
            Registers.BlueL.Define(this).WithValueField(0,8,out bL,FieldMode.Read);
            Registers.BlueH.Define(this).WithValueField(0,8,out bH,FieldMode.Read);
            Registers.ProxData.Define(this).WithValueField(0,8,out proxData,FieldMode.Read);
            Registers.ID.Define(this, 0xAB);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            cL.Value=(byte)(Clear&0xFF); cH.Value=(byte)(Clear>>8);
            rL.Value=(byte)(Red&0xFF); rH.Value=(byte)(Red>>8);
            gL.Value=(byte)(Green&0xFF); gH.Value=(byte)(Green>>8);
            bL.Value=(byte)(Blue&0xFF); bH.Value=(byte)(Blue>>8);
            proxData.Value=(byte)(Proximity&0xFF);
        }

        private IValueRegisterField cL,cH,rL,rH,gL,gH,bL,bH,proxData;
        private int registerAddress;

        private enum Registers : int
        {
            Enable=0x80,Status=0x93,
            ClearL=0x94,ClearH=0x95,RedL=0x96,RedH=0x97,
            GreenL=0x98,GreenH=0x99,BlueL=0x9A,BlueH=0x9B,
            ProxData=0x9C,
            ID=0x92,
        }
    }
}
