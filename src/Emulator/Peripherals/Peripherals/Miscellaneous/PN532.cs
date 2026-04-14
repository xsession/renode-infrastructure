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

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    // NXP PN532 - NFC/RFID reader/writer (I2C)
    public class PN532 : II2CPeripheral, IPeripheral
    {
        public PN532()
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
            if(data.Length < 6) return;
            // Expected: 0x00 0x00 0xFF LEN LCS [TFI CMD ...] DCS 0x00
            var len = data[3];
            if(data.Length < 6 + len) return;
            var tfi = data[5]; // 0xD4 = host-to-PN532
            var cmd = data[6];

            this.Log(LogLevel.Debug, "PN532 command: 0x{0:X2}", cmd);

            switch(cmd)
            {
                case 0x02: // GetFirmwareVersion
                    readBuffer = BuildResponse(0x03, new byte[] { 0x07, 0x03, 0x07, 0x00 }); // IC=PN532, FW 1.6, support ISO14443A/B
                    break;
                case 0x14: // SAMConfiguration
                    readBuffer = BuildResponse(0x15, new byte[0]);
                    break;
                case 0x4A: // InListPassiveTarget
                    if(TagUID != null && TagUID.Length > 0)
                    {
                        // Return 1 target found
                        var resp = new byte[5 + TagUID.Length];
                        resp[0] = 0x01; // NbTg = 1
                        resp[1] = 0x01; // Tg = 1
                        resp[2] = 0x04; // SENS_RES MSB
                        resp[3] = 0x00; // SENS_RES LSB
                        resp[4] = (byte)TagUID.Length;
                        Array.Copy(TagUID, 0, resp, 5, TagUID.Length);
                        readBuffer = BuildResponse(0x4B, resp);
                    }
                    else
                    {
                        readBuffer = BuildResponse(0x4B, new byte[] { 0x00 }); // No targets
                    }
                    break;
                default:
                    readBuffer = BuildResponse((byte)(cmd + 1), new byte[0]);
                    break;
            }
            readIndex = 0;
        }

        public byte[] Read(int count)
        {
            // First byte is status (0x01 = ready)
            var result = new byte[count];
            result[0] = 0x01; // Ready
            for(var i = 1; i < count && readIndex < readBuffer.Length; i++)
                result[i] = readBuffer[readIndex++];
            return result;
        }

        public void FinishTransmission() { }

        // Set this to simulate a tag present
        public byte[] TagUID { get; set; } = new byte[] { 0x04, 0x11, 0x22, 0x33 };

        private byte[] BuildResponse(byte cmd, byte[] payload)
        {
            var len = (byte)(payload.Length + 1); // +1 for TFI
            var lcs = (byte)(~len + 1);
            byte tfi = 0xD5;
            var dcs = tfi;
            dcs = (byte)(dcs + cmd);
            foreach(var b in payload) dcs = (byte)(dcs + b);
            dcs = (byte)(~dcs + 1);

            var resp = new byte[7 + payload.Length];
            resp[0] = 0x00; resp[1] = 0x00; resp[2] = 0xFF; // preamble + start
            resp[3] = len; resp[4] = lcs;
            resp[5] = tfi; resp[6] = cmd;
            Array.Copy(payload, 0, resp, 7, payload.Length);
            // append DCS and postamble (simplified)
            return resp;
        }

        private byte[] readBuffer;
        private int readIndex;
    }
}
