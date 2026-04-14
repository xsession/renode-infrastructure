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
    // Microchip MCP9808 - high-accuracy temperature sensor (I2C)
    public class MCP9808 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public MCP9808()
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
            Registers.Config.Define(this, 0x00);
            Registers.TUpper.Define(this, 0x00);
            Registers.TLower.Define(this, 0x00);
            Registers.TCrit.Define(this, 0x00);
            Registers.TAmbientMSB.Define(this).WithValueField(0, 8, out taMSB, FieldMode.Read);
            Registers.TAmbientLSB.Define(this).WithValueField(0, 8, out taLSB, FieldMode.Read);
            Registers.ManufacturerId.Define(this, 0x00).WithValueField(0, 8, FieldMode.Read, valueProviderCallback: _ => 0x54);
            Registers.DeviceId.Define(this, 0x04).WithValueField(0, 8, FieldMode.Read, valueProviderCallback: _ => 0x04);
            Registers.Resolution.Define(this, 0x03);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // 13-bit signed value, 0.0625 C/LSB, bits [12]=sign, [11:4]=integer, [3:0]=fraction
            var raw = (short)(Temperature / 0.0625m);
            var upper = (byte)((raw >> 8) & 0x1F);
            if(Temperature < 0) upper |= 0x10;
            taMSB.Value = upper;
            taLSB.Value = (byte)(raw & 0xFF);
        }

        private IValueRegisterField taMSB, taLSB;
        private int registerAddress;

        private enum Registers : int
        {
            Config = 0x01,
            TUpper = 0x02,
            TLower = 0x03,
            TCrit  = 0x04,
            TAmbientMSB = 0x05,
            TAmbientLSB = 0x06,
            ManufacturerId = 0x06,
            DeviceId = 0x07,
            Resolution = 0x08,
        }
    }
}
