//
// Copyright (c) 2010-2025 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.SPI;
using Antmicro.Renode.Peripherals.Sensor;
using Antmicro.Renode.Utilities;


namespace Antmicro.Renode.Peripherals.Sensors
{
    // Maxim MAX31855 - thermocouple-to-digital converter (SPI)
    public class MAX31855 : ISPIPeripheral, ITemperatureSensor
    {
        public MAX31855()
        {
            Reset();
        }

        public void FinishTransmission()
        {
            byteIndex = 0;
        }

        public void Reset()
        {
            byteIndex = 0;
        }

        public byte Transmit(byte data)
        {
            // 32-bit read: bits [31:18] = 14-bit thermocouple temp (0.25C/LSB)
            // bits [15:4] = 12-bit internal temp (0.0625C/LSB)
            var tcRaw = (int)(Temperature * 4m);
            var intRaw = (int)(25m * 16m);
            uint word = ((uint)(tcRaw & 0x3FFF) << 18) | ((uint)(intRaw & 0xFFF) << 4);
            byte value = (byte)((word >> (24 - byteIndex * 8)) & 0xFF);
            byteIndex++;
            return value;
        }

        public decimal Temperature { get; set; } = 25.0m;


        private int byteIndex;
    }
}
