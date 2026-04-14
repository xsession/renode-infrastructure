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
    // Maxim MAX31856 - precision thermocouple-to-digital (SPI)
    public class MAX31856 : ISPIPeripheral, ITemperatureSensor
    {
        public MAX31856()
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
            // Register-based SPI: first byte = address (bit 7: 0=read, 1=write)
            if(byteIndex == 0) { currentRegister = (byte)(data & 0x7F); isWrite = (data & 0x80) != 0; byteIndex++; return 0; }
            if(isWrite) { registers[currentRegister] = data; byteIndex++; return 0; }
            byte result = 0;
            switch(currentRegister + byteIndex - 1)
            {
                case 0x0C: result = (byte)((int)(Temperature * 16m) >> 11); break;
                case 0x0D: result = (byte)((int)(Temperature * 16m) >> 3); break;
                case 0x0E: result = (byte)(((int)(Temperature * 16m) & 0x07) << 5); break;
            }
            byteIndex++;
            return result;
        }

        public decimal Temperature { get; set; } = 25.0m;

        private byte currentRegister;
        private bool isWrite;
        private byte[] registers = new byte[0x20];
        private int byteIndex;
    }
}
