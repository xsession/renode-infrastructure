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
    // CEVA BNO085 - 9-axis sensor fusion (I2C/SPI), SH-2 protocol
    public class BNO085 : II2CPeripheral, ITemperatureSensor
    {
        public BNO085()
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
            // Simplified SH-2: respond with product ID report to any command
            readBuffer = new byte[]
            {
                0x14, 0x00, // length
                0x01, 0x01, // channel, sequence
                0xF8, 0x00, // product ID response
                0x01, // reset cause
                0x04, 0x03, 0x02, 0x01, // SW version
                0x00, 0x00, 0x00, 0x00, // part number
                0x00, 0x00, 0x00, 0x00, // build
                0x00, 0x00, // patch
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
        public decimal AccelerationX { get; set; }
        public decimal AccelerationY { get; set; }
        public decimal AccelerationZ { get; set; } = 1.0m;
        public decimal AngularRateX { get; set; }
        public decimal AngularRateY { get; set; }
        public decimal AngularRateZ { get; set; }


        private byte[] readBuffer;
        private int readIndex;

    }
}
