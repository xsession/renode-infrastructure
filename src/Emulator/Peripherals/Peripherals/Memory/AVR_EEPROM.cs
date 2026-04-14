//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
// AVR EEPROM controller.
// Registers: EECR (0x00), EEDR (0x01), EEARL (0x02), EEARH (0x03).
// Write requires EEMPE→EEPE timed sequence.
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Memory
{
    public class AVR_EEPROM : IBytePeripheral, IKnownSize
    {
        public AVR_EEPROM(IMachine machine, int eepromSize = 512)
        {
            this.eepromSize = eepromSize;
            storage = new byte[eepromSize];
            IRQ = new GPIO();
            Reset();
        }

        public byte ReadByte(long offset)
        {
            switch(offset)
            {
                case 0x00: return eecr;
                case 0x01: return eedr;
                case 0x02: return (byte)(eear & 0xFF);
                case 0x03: return (byte)((eear >> 8) & 0xFF);
            }
            this.Log(LogLevel.Warning, "AVR_EEPROM: Read 0x{0:X}", offset);
            return 0;
        }

        public void WriteByte(long offset, byte value)
        {
            switch(offset)
            {
                case 0x00: // EECR
                    bool eempe = (value & EEMPE) != 0;
                    bool eepe  = (value & EEPE) != 0;
                    bool eere  = (value & EERE) != 0;
                    eecr = (byte)(value & 0x3F);

                    if(eere)
                    {
                        // Read
                        if(eear < eepromSize)
                            eedr = storage[eear];
                        eecr &= unchecked((byte)~EERE);
                    }
                    if(eepe && masterWriteEnabled)
                    {
                        // Write
                        if(eear < eepromSize)
                            storage[eear] = eedr;
                        eecr &= unchecked((byte)~EEPE);
                        if((eecr & EERIE) != 0)
                        {
                            IRQ.Set();
                            IRQ.Unset();
                        }
                    }
                    masterWriteEnabled = eempe;
                    return;

                case 0x01:
                    eedr = value;
                    return;
                case 0x02:
                    eear = (ushort)((eear & 0xFF00) | value);
                    return;
                case 0x03:
                    eear = (ushort)((eear & 0x00FF) | (value << 8));
                    return;
            }
        }

        public void Reset()
        {
            eecr = 0;
            eedr = 0;
            eear = 0;
            masterWriteEnabled = false;
            Array.Clear(storage, 0, storage.Length);
        }

        public long Size => 0x04;
        public GPIO IRQ { get; }

        private const byte EERE  = 0x01;
        private const byte EEPE  = 0x02;
        private const byte EEMPE = 0x04;
        private const byte EERIE = 0x08;

        private byte eecr;
        private byte eedr;
        private ushort eear;
        private bool masterWriteEnabled;
        private readonly int eepromSize;
        private readonly byte[] storage;
    }
}
