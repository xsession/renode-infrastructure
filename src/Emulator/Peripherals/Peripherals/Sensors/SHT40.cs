//
// Copyright (c) 2010-2025 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System.Linq;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.Sensor;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Peripherals.Sensors
{
    // Sensirion SHT40 – humidity and temperature sensor (I2C)
    // Command-based: write 1-byte command, read 6 bytes
    public class SHT40 : II2CPeripheral, ITemperatureSensor, IHumiditySensor
    {
        public SHT40()
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
                case 0xFD: // Measure high precision
                case 0xF6: // Measure medium precision
                case 0xE0: // Measure low precision
                    PrepareMeasurement();
                    break;
                case 0x94: // Soft reset
                    Reset();
                    break;
                case 0x89: // Read serial
                    readBuffer = new byte[] { 0x12, 0x34, 0x00, 0x56, 0x78, 0x00 };
                    readIndex = 0;
                    break;
            }
        }

        public byte[] Read(int count)
        {
            var result = new byte[count];
            for(var i = 0; i < count && readIndex < readBuffer.Length; i++)
            {
                result[i] = readBuffer[readIndex++];
            }
            return result;
        }

        public void FinishTransmission() { }

        public decimal Temperature { get; set; } = 25.0m;
        public decimal Humidity { get; set; } = 50.0m;

        private void PrepareMeasurement()
        {
            var rawT = (ushort)((Temperature + 45m) * 65535m / 175m);
            var rawH = (ushort)(Humidity * 65535m / 100m);
            readBuffer = new byte[]
            {
                (byte)(rawT >> 8), (byte)(rawT & 0xFF), 0x00,
                (byte)(rawH >> 8), (byte)(rawH & 0xFF), 0x00,
            };
            readIndex = 0;
        }

        private byte[] readBuffer;
        private int readIndex;
    }
}
