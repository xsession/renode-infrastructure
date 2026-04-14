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
    // u-blox NEO-M8 series - GNSS receiver (I2C)
    public class NEO_M8 : II2CPeripheral
    {
        public NEO_M8()
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
            // UBX protocol: 0xB5 0x62 class id len payload checksum
            // On any write, prepare a NAV-PVT response
            var lat = (int)(Latitude * 10000000m);
            var lon = (int)(Longitude * 10000000m);
            var alt = AltitudeMm;
            readBuffer = new byte[]
            {
                0xB5, 0x62, // sync
                0x01, 0x07, // NAV-PVT
                0x5C, 0x00, // length = 92
                // iTOW (4 bytes)
                0x00, 0x00, 0x00, 0x00,
                // year, month, day, hour, min, sec
                0xE8, 0x07, 0x01, 0x01, 0x00, 0x00, 0x00,
                // valid, tAcc
                0x37, 0x00, 0x00, 0x00, 0x00,
                // fixType, flags
                0x03, 0x01, 0x0F, (byte)NumSatellites,
                // lon, lat (little-endian)
                (byte)(lon&0xFF),(byte)((lon>>8)&0xFF),(byte)((lon>>16)&0xFF),(byte)((lon>>24)&0xFF),
                (byte)(lat&0xFF),(byte)((lat>>8)&0xFF),(byte)((lat>>16)&0xFF),(byte)((lat>>24)&0xFF),
                // height, hMSL
                (byte)(alt&0xFF),(byte)((alt>>8)&0xFF),(byte)((alt>>16)&0xFF),(byte)((alt>>24)&0xFF),
                (byte)(alt&0xFF),(byte)((alt>>8)&0xFF),(byte)((alt>>16)&0xFF),(byte)((alt>>24)&0xFF),
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

        public decimal Latitude { get; set; } = 48.8566m;
        public decimal Longitude { get; set; } = 2.3522m;
        public int AltitudeMm { get; set; } = 35000;
        public int NumSatellites { get; set; } = 8;


        private byte[] readBuffer;
        private int readIndex;

    }
}
