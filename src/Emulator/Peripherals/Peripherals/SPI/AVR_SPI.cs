//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
//
// AVR SPI peripheral (ATmega SPI master/slave).
//
// Register map (byte-wide I/O):
//   0x00  SPCR  — SPI Control Register
//   0x01  SPSR  — SPI Status Register
//   0x02  SPDR  — SPI Data Register
//
using System;
using System.Collections.Generic;

using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.SPI;

namespace Antmicro.Renode.Peripherals.SPI
{
    public class AVR_SPI : IBytePeripheral, IKnownSize
    {
        public AVR_SPI(IMachine machine, long baseFrequency = 16_000_000)
        {
            this.baseFrequency = baseFrequency;
            IRQ = new GPIO();
            Reset();
        }

        public byte ReadByte(long offset)
        {
            switch((Registers)offset)
            {
            case Registers.SPCR: return spcr;
            case Registers.SPSR:
                var val = spsr;
                spsr &= unchecked((byte)~SPSR_SPIF); /* Reading clears SPIF */
                return val;
            case Registers.SPDR: return rxData;
            default:
                this.Log(LogLevel.Warning,
                         "AVR_SPI: Read from unknown offset 0x{0:X}", offset);
                return 0;
            }
        }

        public void WriteByte(long offset, byte value)
        {
            switch((Registers)offset)
            {
            case Registers.SPCR:
                spcr = value;
                break;
            case Registers.SPSR:
                /* SPI2X bit (bit 0) is writable */
                spsr = (byte)((spsr & 0xFE) | (value & 0x01));
                break;
            case Registers.SPDR:
                txData = value;
                DoTransfer();
                break;
            default:
                this.Log(LogLevel.Warning,
                         "AVR_SPI: Write 0x{0:X} to unknown offset 0x{1:X}", value, offset);
                break;
            }
        }

        public void Reset()
        {
            spcr   = 0;
            spsr   = 0;
            rxData = 0;
            txData = 0;
            IRQ.Unset();
        }

        /// <summary>
        /// Register a slave device to exchange bytes with.
        /// </summary>
        public void RegisterSlave(ISPISlave slave)
        {
            registeredSlave = slave;
        }

        public long Size => 0x03;

        public GPIO IRQ { get; }

        private void DoTransfer()
        {
            byte rx = 0;
            if(registeredSlave != null)
            {
                rx = registeredSlave.Transmit(txData);
            }
            rxData = rx;
            spsr |= SPSR_SPIF;

            if((spcr & SPCR_SPIE) != 0)
            {
                IRQ.Set();
                IRQ.Unset();
            }

            TransferCompleted?.Invoke(txData, rxData);
        }

        public event Action<byte, byte> TransferCompleted;

        private enum Registers : long
        {
            SPCR = 0x00,
            SPSR = 0x01,
            SPDR = 0x02,
        }

        private const byte SPCR_SPIE = (byte)(1 << 7);
        private const byte SPCR_SPE  = (1 << 6);
        private const byte SPCR_MSTR = (1 << 4);

        private const byte SPSR_SPIF = (byte)(1 << 7);
        private const byte SPSR_WCOL = (1 << 6);

        private byte spcr;
        private byte spsr;
        private byte rxData;
        private byte txData;

        private ISPISlave registeredSlave;
        private readonly long baseFrequency;
    }

    /// <summary>Simple SPI slave interface for AVR SPI master to talk to.</summary>
    public interface ISPISlave
    {
        byte Transmit(byte data);
    }
}
