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
    // Melexis MLX90393 - 3-axis magnetometer (I2C)
    public class MLX90393 : II2CPeripheral, IMagneticSensor
    {
        public MLX90393()
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
            if((cmd & 0xF0) == 0x30) // Single measurement
            {
                readBuffer = new byte[] { 0x01 }; // status
                readIndex = 0;
            }
            else if((cmd & 0xF0) == 0x40) // Read measurement
            {
                var mx = (short)(MagneticFluxDensityX / 100000.0 * 3000);
                var my = (short)(MagneticFluxDensityY / 100000.0 * 3000);
                var mz = (short)(MagneticFluxDensityZ / 100000.0 * 3000);
                readBuffer = new byte[] { 0x01, (byte)(mx>>8),(byte)(mx&0xFF), (byte)(my>>8),(byte)(my&0xFF), (byte)(mz>>8),(byte)(mz&0xFF) };
                readIndex = 0;
            }
            else if(cmd == 0x80) // Reset
            { Reset(); readBuffer = new byte[]{0x01}; readIndex=0; }
        }

        public byte[] Read(int count)
        {
            var result = new byte[count];
            for(var i = 0; i < count && readIndex < readBuffer.Length; i++)
                result[i] = readBuffer[readIndex++];
            return result;
        }

        public void FinishTransmission() { }

        public int MagneticFluxDensityX { get; set; }
        public int MagneticFluxDensityY { get; set; }
        public int MagneticFluxDensityZ { get; set; }


        private byte[] readBuffer;
        private int readIndex;

    }
}
