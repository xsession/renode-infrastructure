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
    // Panasonic AMG8833 (Grid-EYE) - 8x8 infrared thermal camera (I2C)
    public class AMG8833 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public AMG8833()
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
        public decimal PixelUniformTemp { get; set; } = 25.0m;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.PowerControl.Define(this);
            Registers.Reset.Define(this).WithWriteCallback((_,val) => { if(val==0x3F) Reset(); });
            Registers.FrameRate.Define(this, 0x01);
            Registers.StatusReg.Define(this);
            Registers.ThermistorL.Define(this).WithValueField(0,8,out thermL,FieldMode.Read);
            Registers.ThermistorH.Define(this).WithValueField(0,8,out thermH,FieldMode.Read);
            // Pixel data starts at 0x80, 128 bytes for 64 pixels (16-bit each)
            for(byte addr = 0x80; addr < 0x80 + 128 && addr != 0; addr++)
            {
                ((Registers)addr).Define(this, 0x00);
            }
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            var rawT = (short)(Temperature * 16m);
            thermL.Value = (byte)(rawT & 0xFF);
            thermH.Value = (byte)((rawT >> 8) & 0x0F);
            // Fill pixel data with uniform temperature
            var rawPix = (short)(PixelUniformTemp * 4m);
            for(byte addr = 0x80; addr < 0x80 + 128; addr += 2)
            {
                RegistersCollection.Write(addr, (byte)(rawPix & 0xFF));
                RegistersCollection.Write((byte)(addr + 1), (byte)((rawPix >> 8) & 0x0F));
            }
        }

        private IValueRegisterField thermL, thermH;
        private int registerAddress;

        private enum Registers : int
        {
            PowerControl=0x00,Reset=0x01,FrameRate=0x02,
            StatusReg=0x04,
            ThermistorL=0x0E,ThermistorH=0x0F,
        }
    }
}
