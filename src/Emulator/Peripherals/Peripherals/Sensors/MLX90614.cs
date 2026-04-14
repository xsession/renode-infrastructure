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
    // Melexis MLX90614 - infrared contactless thermometer (I2C/SMBus)
    public class MLX90614 : II2CPeripheral, ITemperatureSensor
    {
        public MLX90614()
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
            switch(data[0])
            {
                case 0x06: // Read ambient temp (Ta)
                    var rawTa = (ushort)((AmbientTemperature + 273.15m) * 50m);
                    readBuffer = new byte[] { (byte)(rawTa & 0xFF), (byte)(rawTa >> 8), 0x00 };
                    readIndex = 0;
                    break;
                case 0x07: // Read object temp (Tobj1)
                    var rawTo = (ushort)((Temperature + 273.15m) * 50m);
                    readBuffer = new byte[] { (byte)(rawTo & 0xFF), (byte)(rawTo >> 8), 0x00 };
                    readIndex = 0;
                    break;
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

        public decimal Temperature { get; set; } = 25.0m;
        public decimal AmbientTemperature { get; set; } = 22.0m;


        private byte[] readBuffer;
        private int readIndex;

    }
}
