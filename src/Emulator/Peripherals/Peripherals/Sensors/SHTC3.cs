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
    // Sensirion SHTC3 - humidity/temperature sensor (I2C)
    public class SHTC3 : II2CPeripheral, ITemperatureSensor, IHumiditySensor
    {
        public SHTC3()
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
            if(data.Length < 2) return;
            var cmd = (ushort)((data[0] << 8) | data[1]);
            // Generic measurement response
            var rawT = (ushort)((Temperature + 45m) * 65535m / 175m);
            var rawH = (ushort)(Humidity * 65535m / 100m);
            readBuffer = new byte[]
            {
                (byte)(rawT >> 8), (byte)(rawT & 0xFF), 0x00,
                (byte)(rawH >> 8), (byte)(rawH & 0xFF), 0x00,
            };
            readIndex = 0;
        }

        public byte[] Read(int count)
        {
            var result = new byte[count];
            for(var i = 0; i < count && readIndex < readBuffer.Length; i++)
                result[i] = readBuffer[readIndex++];
            return result;
        }

        public void FinishTransmission() { }

        public decimal Temperature { get; set; } = 25.0m;
        public decimal Humidity { get; set; } = 50.0m;
        public int CO2 { get; set; } = 400;


        private byte[] readBuffer;
        private int readIndex;

    }
}
