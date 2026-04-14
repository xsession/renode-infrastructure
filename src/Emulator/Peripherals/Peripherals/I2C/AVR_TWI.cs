//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
//
// AVR TWI (Two-Wire Interface / I2C) peripheral.
//
// Register map (byte-wide I/O):
//   0x00  TWBR  — TWI Bit Rate Register
//   0x01  TWSR  — TWI Status Register
//   0x02  TWAR  — TWI (Slave) Address Register
//   0x03  TWDR  — TWI Data Register
//   0x04  TWCR  — TWI Control Register
//   0x05  TWAMR — TWI (Slave) Address Mask Register
//
using System;
using System.Collections.Generic;

using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.I2C;

namespace Antmicro.Renode.Peripherals.I2C
{
    public class AVR_TWI : IBytePeripheral, IKnownSize
    {
        public AVR_TWI(IMachine machine)
        {
            IRQ = new GPIO();
            Reset();
        }

        public byte ReadByte(long offset)
        {
            switch((Registers)offset)
            {
            case Registers.TWBR:  return twbr;
            case Registers.TWSR:  return (byte)((twsr & 0xF8) | (prescaler & 0x03));
            case Registers.TWAR:  return twar;
            case Registers.TWDR:  return twdr;
            case Registers.TWCR:  return twcr;
            case Registers.TWAMR: return twamr;
            default:
                this.Log(LogLevel.Warning,
                         "AVR_TWI: Read from unknown offset 0x{0:X}", offset);
                return 0;
            }
        }

        public void WriteByte(long offset, byte value)
        {
            switch((Registers)offset)
            {
            case Registers.TWBR:
                twbr = value;
                break;
            case Registers.TWSR:
                prescaler = (byte)(value & 0x03);
                break;
            case Registers.TWAR:
                twar = value;
                break;
            case Registers.TWDR:
                twdr = value;
                break;
            case Registers.TWCR:
                HandleControlWrite(value);
                break;
            case Registers.TWAMR:
                twamr = value;
                break;
            default:
                this.Log(LogLevel.Warning,
                         "AVR_TWI: Write 0x{0:X} to unknown offset 0x{1:X}", value, offset);
                break;
            }
        }

        public void Reset()
        {
            twbr      = 0;
            twsr      = 0xF8; /* Status: no relevant state */
            twar      = 0xFE;
            twdr      = 0xFF;
            twcr      = 0;
            twamr     = 0;
            prescaler = 0;
            IRQ.Unset();
        }

        public long Size => 0x06;

        public GPIO IRQ { get; }

        private void HandleControlWrite(byte value)
        {
            twcr = value;

            /* TWINT is cleared by writing 1 */
            if((value & TWCR_TWINT) != 0)
            {
                twcr &= unchecked((byte)~TWCR_TWINT);
            }

            /* If TWEN and TWSTA → issue START */
            if((value & TWCR_TWEN) != 0 && (value & TWCR_TWSTA) != 0)
            {
                twsr = STATUS_START;
                twcr |= TWCR_TWINT;
                FireIRQ();
            }
            /* If TWEN and TWSTO → issue STOP */
            else if((value & TWCR_TWEN) != 0 && (value & TWCR_TWSTO) != 0)
            {
                twsr = 0xF8;
                twcr &= unchecked((byte)~TWCR_TWSTO);
            }
            /* If TWEN and data write */
            else if((value & TWCR_TWEN) != 0)
            {
                /* Master transmit: SLA+W ACK */
                twsr = STATUS_MT_SLA_ACK;
                twcr |= TWCR_TWINT;
                FireIRQ();
            }
        }

        private void FireIRQ()
        {
            if((twcr & TWCR_TWIE) != 0)
            {
                IRQ.Set();
                IRQ.Unset();
            }
        }

        private enum Registers : long
        {
            TWBR  = 0x00,
            TWSR  = 0x01,
            TWAR  = 0x02,
            TWDR  = 0x03,
            TWCR  = 0x04,
            TWAMR = 0x05,
        }

        /* TWCR bits */
        private const byte TWCR_TWIE  = (1 << 0);
        private const byte TWCR_TWEN  = (1 << 2);
        private const byte TWCR_TWSTO = (1 << 4);
        private const byte TWCR_TWSTA = (1 << 5);
        private const byte TWCR_TWEA  = (1 << 6);
        private const byte TWCR_TWINT = (byte)(1 << 7);

        /* Common TWI status codes */
        private const byte STATUS_START       = 0x08;
        private const byte STATUS_REP_START   = 0x10;
        private const byte STATUS_MT_SLA_ACK  = 0x18;
        private const byte STATUS_MT_DATA_ACK = 0x28;
        private const byte STATUS_MR_SLA_ACK  = 0x40;

        private byte twbr;
        private byte twsr;
        private byte twar;
        private byte twdr;
        private byte twcr;
        private byte twamr;
        private byte prescaler;
    }
}
