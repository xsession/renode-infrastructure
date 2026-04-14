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
    // TI ADS1115 - 16-bit ADC, 4-channel (I2C)
    public class ADS1115 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public ADS1115()
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

        public decimal Channel0Voltage { get; set; } = 1.5m;
        public decimal Channel1Voltage { get; set; } = 1.0m;
        public decimal Channel2Voltage { get; set; } = 0.5m;
        public decimal Channel3Voltage { get; set; } = 0.0m;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.ConversionMSB.Define(this).WithValueField(0,8,out convH,FieldMode.Read);
            Registers.ConversionLSB.Define(this).WithValueField(0,8,out convL,FieldMode.Read);
            Registers.ConfigMSB.Define(this, 0x85).WithValueField(0,8,name:"config_hi")
                .WithWriteCallback((_,val) => { currentMux = (byte)((val>>4)&0x07); UpdateReadout(); });
            Registers.ConfigLSB.Define(this, 0x83);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // PGA=+/-4.096V default => 1 LSB = 0.125 mV
            decimal voltage;
            switch(currentMux)
            {
                case 4: voltage = Channel0Voltage; break;
                case 5: voltage = Channel1Voltage; break;
                case 6: voltage = Channel2Voltage; break;
                case 7: voltage = Channel3Voltage; break;
                default: voltage = Channel0Voltage - Channel1Voltage; break;
            }
            var raw = (short)(voltage / 0.000125m);
            convH.Value=(byte)(raw>>8); convL.Value=(byte)(raw&0xFF);
        }

        private IValueRegisterField convH, convL;
        private byte currentMux;
        private int registerAddress;

        private enum Registers : int
        {
            ConversionMSB=0x00,ConversionLSB=0x01,
            ConfigMSB=0x02,ConfigLSB=0x03,
        }
    }
}
