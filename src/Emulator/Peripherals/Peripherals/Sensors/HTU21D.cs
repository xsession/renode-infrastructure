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
    // TE Connectivity HTU21D - humidity and temperature sensor (I2C)
    public class HTU21D : II2CPeripheral, ITemperatureSensor, IHumiditySensor
    {
        public HTU21D()
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
                case 0xE3: // Trigger temp measurement (hold master)
                case 0xF3: // Trigger temp measurement (no hold)
                    var rawT = (ushort)(Temperature / 175.72m * 65536m + 46.85m / 175.72m * 65536m);
                    readBuffer = new byte[] { (byte)(rawT >> 8), (byte)(rawT & 0xFC), 0x00 };
                    readIndex = 0;
                    break;
                case 0xE5: // Trigger humidity (hold master)
                case 0xF5: // Trigger humidity (no hold)
                    var rawH = (ushort)(Humidity / 125m * 65536m + 6m / 125m * 65536m);
                    readBuffer = new byte[] { (byte)(rawH >> 8), (byte)(rawH & 0xFC), 0x00 };
                    readIndex = 0;
                    break;
                case 0xFE: // Soft reset
                    Reset();
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
        public decimal Humidity { get; set; } = 50.0m;


        private byte[] readBuffer;
        private int readIndex;

    }
}
