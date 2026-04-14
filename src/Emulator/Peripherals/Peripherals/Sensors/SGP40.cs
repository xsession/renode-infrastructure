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
    // Sensirion SGP40 - VOC index sensor (I2C)
    public class SGP40 : II2CPeripheral
    {
        public SGP40()
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
            if(cmd == 0x260F) // Measure raw signal
            {
                var raw = (ushort)VocIndex;
                readBuffer = new byte[] { (byte)(raw >> 8), (byte)(raw & 0xFF), 0x00 };
                readIndex = 0;
            }
            else if(cmd == 0x3682) // Get serial
            {
                readBuffer = new byte[] { 0x00, 0x01, 0x00, 0x02, 0x03, 0x00, 0x04, 0x05, 0x00 };
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

        public int VocIndex { get; set; } = 100;


        private byte[] readBuffer;
        private int readIndex;

    }
}
