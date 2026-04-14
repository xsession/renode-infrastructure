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
    // Asair AHT20 - humidity and temperature sensor (I2C)
    public class AHT20 : II2CPeripheral, ITemperatureSensor, IHumiditySensor
    {
        public AHT20()
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
            if(data[0] == 0xAC && data.Length >= 3)
            {
                // Trigger measurement
                var rawH = (uint)(Humidity / 100m * 0x100000);
                var rawT = (uint)((Temperature + 50m) / 200m * 0x100000);
                readBuffer = new byte[]
                {
                    0x1C, // status: calibrated, not busy
                    (byte)((rawH >> 12) & 0xFF),
                    (byte)((rawH >> 4) & 0xFF),
                    (byte)(((rawH & 0x0F) << 4) | ((rawT >> 16) & 0x0F)),
                    (byte)((rawT >> 8) & 0xFF),
                    (byte)(rawT & 0xFF),
                    0x00  // CRC placeholder
                };
                readIndex = 0;
            }
            else if(data[0] == 0xBA) { Reset(); } // soft reset
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


        private byte[] readBuffer;
        private int readIndex;

    }
}
