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
    // Sensirion SGP30 - TVOC and eCO2 sensor (I2C)
    public class SGP30 : II2CPeripheral
    {
        public SGP30()
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
            switch(cmd)
            {
                case 0x2008: // Measure air quality
                    readBuffer = new byte[]
                    {
                        (byte)((eCO2 >> 8) & 0xFF), (byte)(eCO2 & 0xFF), 0x00,
                        (byte)((TVOC >> 8) & 0xFF), (byte)(TVOC & 0xFF), 0x00,
                    };
                    readIndex = 0;
                    break;
                case 0x2003: // Init air quality
                case 0x0020: // Get serial
                    readBuffer = new byte[] { 0x00, 0x01, 0x00, 0x02, 0x03, 0x00, 0x04, 0x05, 0x00 };
                    readIndex = 0;
                    break;
                case 0x2050: // Measure test
                    readBuffer = new byte[] { 0xD4, 0x00, 0x00 };
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

        public int eCO2 { get; set; } = 400;
        public int TVOC { get; set; } = 0;


        private byte[] readBuffer;
        private int readIndex;

    }
}
