//
// Copyright (c) 2010-2025 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.Sensor;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Sensors
{
    // ROHM BH1750FVI - ambient light sensor (I2C)
    public class BH1750 : II2CPeripheral
    {
        public BH1750()
        {
            Reset();
        }

        public void Reset()
        {
            readBuffer = new byte[0];
            readIndex = 0;
        }

        public void Write(byte[] data)
        {
            if(data.Length == 0) return;
            // BH1750 commands: 0x01=PowerOn, 0x10=ContinuousHiRes, etc.
            if(data[0] == 0x10 || data[0] == 0x11 || data[0] == 0x20)
            {
                var raw = (ushort)(LuxValue / 1.2 * 1.0);
                readBuffer = new byte[] { (byte)(raw >> 8), (byte)(raw & 0xFF) };
                readIndex = 0;
            }
        }

        public byte[] Read(int count)
        {
            var result = new byte[count];
            for(var i = 0; i < count && readIndex < readBuffer.Length; i++)
                result[i] = readBuffer[readIndex++];
            return result;
        }

        public void FinishTransmission() { }

        public int LuxValue { get; set; } = 500;


        private byte[] readBuffer;
        private int readIndex;

    }
}
