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
    // Sensirion SHT31 – humidity and temperature sensor (I2C)
    // Uses command-based protocol: 2-byte command, then read 6 bytes (temp MSB, LSB, CRC, hum MSB, LSB, CRC)
    public class SHT31 : II2CPeripheral, ITemperatureSensor, IHumiditySensor
    {
        public SHT31()
        {
            Reset();
        }

        public void Reset()
        {
            state = State.Idle;
            readBuffer = new byte[0];
            readIndex = 0;
        }

        public void Write(byte[] data)
        {
            if(data.Length < 2) return;
            var cmd = (ushort)((data[0] << 8) | data[1]);
            switch(cmd)
            {
                case 0x2400: // Single shot, high repeatability
                case 0x240B: // Single shot, medium
                case 0x2416: // Single shot, low
                    PrepareMeasurement();
                    break;
                case 0x30A2: // Soft reset
                    Reset();
                    break;
                case 0xF32D: // Read status
                    readBuffer = new byte[] { 0x00, 0x00, 0x81 };
                    readIndex = 0;
                    state = State.Reading;
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

        public void FinishTransmission()
        {
            state = State.Idle;
        }

        public decimal Temperature { get; set; } = 25.0m;
        public decimal Humidity { get; set; } = 50.0m;

        private void PrepareMeasurement()
        {
            // Temperature: raw = (T + 45) * 65535 / 175
            var rawT = (ushort)((Temperature + 45m) * 65535m / 175m);
            // Humidity: raw = H * 65535 / 100
            var rawH = (ushort)(Humidity * 65535m / 100m);
            readBuffer = new byte[]
            {
                (byte)(rawT >> 8), (byte)(rawT & 0xFF), Crc8(rawT),
                (byte)(rawH >> 8), (byte)(rawH & 0xFF), Crc8(rawH),
            };
            readIndex = 0;
            state = State.Reading;
        }

        private static byte Crc8(ushort value)
        {
            byte crc = 0xFF;
            crc ^= (byte)(value >> 8);
            for(var j = 0; j < 8; j++) crc = (byte)((crc & 0x80) != 0 ? (crc << 1) ^ 0x31 : crc << 1);
            crc ^= (byte)(value & 0xFF);
            for(var j = 0; j < 8; j++) crc = (byte)((crc & 0x80) != 0 ? (crc << 1) ^ 0x31 : crc << 1);
            return crc;
        }

        private State state;
        private byte[] readBuffer;
        private int readIndex;

        private enum State { Idle, Reading }
    }
}
