//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
// MSP430 GPIO Port (8-bit port).
// Registers per port: PxIN(0x00), PxOUT(0x01), PxDIR(0x02), PxIFG(0x03),
//                     PxIES(0x04), PxIE(0x05), PxSEL(0x06), PxREN(0x07).
//
using System;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.GPIOPort
{
    public class MSP430_GPIO : IBytePeripheral, IKnownSize
    {
        public MSP430_GPIO(IMachine machine, int numberOfPins = 8)
        {
            IRQ = new GPIO();
            Connections = new GPIO[numberOfPins];
            for(int i = 0; i < numberOfPins; i++)
                Connections[i] = new GPIO();
            Reset();
        }

        public byte ReadByte(long offset)
        {
            switch(offset)
            {
                case 0x00: return pxIn;
                case 0x01: return pxOut;
                case 0x02: return pxDir;
                case 0x03: return pxIfg;
                case 0x04: return pxIes;
                case 0x05: return pxIe;
                case 0x06: return pxSel;
                case 0x07: return pxRen;
            }
            this.Log(LogLevel.Warning, "MSP430_GPIO: Read 0x{0:X}", offset);
            return 0;
        }

        public void WriteByte(long offset, byte value)
        {
            switch(offset)
            {
                case 0x00: return; // PxIN is read-only
                case 0x01:
                    pxOut = value;
                    UpdateOutputPins();
                    return;
                case 0x02: pxDir = value; UpdateOutputPins(); return;
                case 0x03: pxIfg = value; return;
                case 0x04: pxIes = value; return;
                case 0x05: pxIe = value; return;
                case 0x06: pxSel = value; return;
                case 0x07: pxRen = value; return;
            }
        }

        public void SetInputPin(int pin, bool state)
        {
            if(pin < 0 || pin >= 8) return;
            byte mask = (byte)(1 << pin);
            bool oldState = (pxIn & mask) != 0;
            if(state)
                pxIn |= mask;
            else
                pxIn &= (byte)~mask;

            // Edge detection
            if((pxIe & mask) != 0 && (pxSel & mask) == 0)
            {
                bool risingEdge = !oldState && state;
                bool fallingEdge = oldState && !state;
                bool iesHigh = (pxIes & mask) != 0; // 1 = falling edge

                if((iesHigh && fallingEdge) || (!iesHigh && risingEdge))
                {
                    pxIfg |= mask;
                    IRQ.Set();
                    IRQ.Unset();
                }
            }
        }

        public void Reset()
        {
            pxIn = 0; pxOut = 0; pxDir = 0;
            pxIfg = 0; pxIes = 0; pxIe = 0;
            pxSel = 0; pxRen = 0;
        }

        public long Size => 0x08;
        public GPIO IRQ { get; }
        public GPIO[] Connections { get; }

        private void UpdateOutputPins()
        {
            for(int i = 0; i < Connections.Length; i++)
            {
                byte mask = (byte)(1 << i);
                if((pxDir & mask) != 0) // output direction
                {
                    bool val = (pxOut & mask) != 0;
                    Connections[i].Set(val);
                }
            }
        }

        private byte pxIn, pxOut, pxDir;
        private byte pxIfg, pxIes, pxIe;
        private byte pxSel, pxRen;
    }
}
