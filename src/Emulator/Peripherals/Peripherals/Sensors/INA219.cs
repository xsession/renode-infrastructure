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
    // TI INA219 - current/power monitor (I2C)
    public class INA219 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public INA219()
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

        public decimal BusVoltage { get; set; } = 12.0m;
        public decimal Current { get; set; } = 0.5m;
        public decimal Power { get; set; } = 6.0m;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.ConfigMSB.Define(this, 0x39);
            Registers.ConfigLSB.Define(this, 0x9F);
            Registers.ShuntVoltageMSB.Define(this).WithValueField(0,8,out svH,FieldMode.Read);
            Registers.ShuntVoltageLSB.Define(this).WithValueField(0,8,out svL,FieldMode.Read);
            Registers.BusVoltageMSB.Define(this).WithValueField(0,8,out bvH,FieldMode.Read);
            Registers.BusVoltageLSB.Define(this).WithValueField(0,8,out bvL,FieldMode.Read);
            Registers.PowerMSB.Define(this).WithValueField(0,8,out pwH,FieldMode.Read);
            Registers.PowerLSB.Define(this).WithValueField(0,8,out pwL,FieldMode.Read);
            Registers.CurrentMSB.Define(this).WithValueField(0,8,out crH,FieldMode.Read);
            Registers.CurrentLSB.Define(this).WithValueField(0,8,out crL,FieldMode.Read);
            Registers.ManufacturerIdH.Define(this, 0x54);
            Registers.ManufacturerIdL.Define(this, 0x49);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // Bus voltage: 4mV/LSB, shifted left 3 for INA219
            var rawBV = (short)(BusVoltage / 0.004m);
            bvH.Value=(byte)(rawBV>>8); bvL.Value=(byte)(rawBV&0xFF);
            // Shunt voltage: 10uV/LSB
            var rawSV = (short)(Current * 0.1m / 0.00001m); // assuming 0.1 ohm shunt
            svH.Value=(byte)(rawSV>>8); svL.Value=(byte)(rawSV&0xFF);
            // Current: 1mA/LSB
            var rawCur = (short)(Current * 1000m);
            crH.Value=(byte)(rawCur>>8); crL.Value=(byte)(rawCur&0xFF);
            // Power: 20mW/LSB
            var rawPow = (short)(Power / 0.02m);
            pwH.Value=(byte)(rawPow>>8); pwL.Value=(byte)(rawPow&0xFF);
        }

        private IValueRegisterField svH,svL,bvH,bvL,pwH,pwL,crH,crL;
        private int registerAddress;

        private enum Registers : int
        {
            ConfigMSB=0x00,ConfigLSB=0x01,
            ShuntVoltageMSB=0x02,ShuntVoltageLSB=0x03,
            BusVoltageMSB=0x04,BusVoltageLSB=0x05,
            PowerMSB=0x06,PowerLSB=0x07,
            CurrentMSB=0x08,CurrentLSB=0x09,
            ManufacturerIdH=0xFE,ManufacturerIdL=0xFF,
        }
    }
}
