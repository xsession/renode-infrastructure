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

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    // NXP PCA9685 - 16-channel 12-bit PWM I2C driver (servos, LEDs)
    public class PCA9685 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public PCA9685()
        {
            RegistersCollection = new ByteRegisterCollection(this);
            DefineRegisters();
        }

        public void Reset()
        {
            RegistersCollection.Reset();
            registerAddress = 0;
            Array.Clear(channelOnL, 0, 16);
            Array.Clear(channelOnH, 0, 16);
            Array.Clear(channelOffL, 0, 16);
            Array.Clear(channelOffH, 0, 16);
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

        // Expose duty cycle for each channel (0-4095)
        public int GetChannelDuty(int channel)
        {
            if(channel < 0 || channel > 15) return 0;
            return (channelOffL[channel] | (channelOffH[channel] << 8)) & 0xFFF;
        }

        public ByteRegisterCollection RegistersCollection { get; }

        private void DefineRegisters()
        {
            // MODE1 @ 0x00, MODE2 @ 0x01
            RegistersCollection.DefineRegister(0x00, 0x11); // MODE1: sleep on, allcall
            RegistersCollection.DefineRegister(0x01, 0x04); // MODE2: totem pole
            // Channels 0-15: each has ON_L, ON_H, OFF_L, OFF_H at 0x06 + 4*ch
            for(var ch = 0; ch < 16; ch++)
            {
                var c = ch;
                var baseAddr = 0x06 + 4 * ch;
                RegistersCollection.DefineRegister(baseAddr, 0)
                    .WithValueField(0, 8, name: $"LED{c}_ON_L",
                        writeCallback: (_, val) => channelOnL[c] = (byte)val,
                        valueProviderCallback: _ => channelOnL[c]);
                RegistersCollection.DefineRegister(baseAddr + 1, 0)
                    .WithValueField(0, 8, name: $"LED{c}_ON_H",
                        writeCallback: (_, val) => channelOnH[c] = (byte)(val & 0x1F),
                        valueProviderCallback: _ => channelOnH[c]);
                RegistersCollection.DefineRegister(baseAddr + 2, 0)
                    .WithValueField(0, 8, name: $"LED{c}_OFF_L",
                        writeCallback: (_, val) => channelOffL[c] = (byte)val,
                        valueProviderCallback: _ => channelOffL[c]);
                RegistersCollection.DefineRegister(baseAddr + 3, 0)
                    .WithValueField(0, 8, name: $"LED{c}_OFF_H",
                        writeCallback: (_, val) => channelOffH[c] = (byte)(val & 0x1F),
                        valueProviderCallback: _ => channelOffH[c]);
            }
            // ALL_LED ON/OFF at 0xFA-0xFD
            RegistersCollection.DefineRegister(0xFA);
            RegistersCollection.DefineRegister(0xFB);
            RegistersCollection.DefineRegister(0xFC);
            RegistersCollection.DefineRegister(0xFD);
            // PRE_SCALE at 0xFE (default 30 = ~200Hz)
            RegistersCollection.DefineRegister(0xFE, 0x1E);
        }

        private byte[] channelOnL = new byte[16];
        private byte[] channelOnH = new byte[16];
        private byte[] channelOffL = new byte[16];
        private byte[] channelOffH = new byte[16];
        private int registerAddress;
    }
}
