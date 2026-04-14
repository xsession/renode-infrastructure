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
    // TE Connectivity MS5611 - barometric pressure sensor (I2C)
    public class MS5611 : II2CPeripheral, ITemperatureSensor
    {
        public MS5611()
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
            var cmd = data[0];
            if(cmd == 0x1E) { Reset(); } // Reset command
            else if(cmd >= 0x40 && cmd <= 0x4E) // Convert D1 (pressure)
            {
                var rawP = (uint)(Pressure / 1013.25m * 6000000);
                readBuffer = new byte[] { (byte)((rawP >> 16) & 0xFF), (byte)((rawP >> 8) & 0xFF), (byte)(rawP & 0xFF) };
                readIndex = 0;
            }
            else if(cmd >= 0x50 && cmd <= 0x5E) // Convert D2 (temperature)
            {
                var rawT = (uint)((Temperature + 40m) / 85m * 8000000);
                readBuffer = new byte[] { (byte)((rawT >> 16) & 0xFF), (byte)((rawT >> 8) & 0xFF), (byte)(rawT & 0xFF) };
                readIndex = 0;
            }
            else if(cmd == 0x00) { /* ADC read - data already prepared */ }
            else if(cmd >= 0xA0 && cmd <= 0xAE) // PROM read
            {
                readBuffer = new byte[] { 0x00, 0x80 };
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

        public decimal Temperature { get; set; } = 25.0m;
        public decimal Pressure { get; set; } = 1013.25m;


        private byte[] readBuffer;
        private int readIndex;

    }
}
