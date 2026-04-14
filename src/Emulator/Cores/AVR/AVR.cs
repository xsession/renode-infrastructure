//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
//
// Atmel AVR 8-bit RISC CPU emulation core.
//
// Architecture notes:
//   - 32 × 8-bit general purpose registers R0-R31
//   - Pointer register pairs: X(R27:R26), Y(R29:R28), Z(R31:R30)
//   - 8-bit SREG status register
//   - Harvard architecture; program memory separate from data memory
//   - Data memory mapped at 0x800000 offset in Renode system bus
//
using System;
using System.Collections.Generic;

using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Utilities.Binding;

using Endianess = ELFSharp.ELF.Endianess;

namespace Antmicro.Renode.Peripherals.CPU
{
    [GPIO(NumberOfInputs = 1)]
    public partial class AVR : TranslationCPU
    {
        public AVR(string cpuType, IMachine machine)
            : base(cpuType, machine, Endianess.LittleEndian)
        {
        }

        public override string Architecture     => "avr";
        public override string GDBArchitecture  => "avr";
        public override string[] AllLLVMTriples => new[] { "avr" };
        public override string LLVMModel        => "avr";
        public override string GetLLVMTriple(uint flags) => AllLLVMTriples[0];
        public override Endianess DisassemblyHexFormatting => Endianess.LittleEndian;

        public override List<GDBFeatureDescriptor> GDBFeatures =>
            new List<GDBFeatureDescriptor>();

        public override void OnGPIO(int number, bool value)
        {
            switch(number)
            {
            case 0:
                base.OnGPIO(number, value);
                break;
            default:
                this.Log(LogLevel.Warning, "AVR: GPIO index {0} out of range", number);
                break;
            }
        }

        protected override Interrupt DecodeInterrupt(int number)
        {
            switch(number)
            {
            case 0:  return Interrupt.Hard;
            default: throw InvalidInterruptNumberException;
            }
        }

        protected override string GetExceptionDescription(ulong exceptionIndex)
        {
            return ExceptionDescriptionsMap.TryGetValue(exceptionIndex, out var result)
                ? result
                : base.GetExceptionDescription(exceptionIndex);
        }

        [Export]
        private int FindBestInterrupt()
        {
            return 0;
        }

        [Export]
        private void AcknowledgeInterrupt(int interruptNumber)
        {
            this.NoisyLog("AVR: Acknowledged interrupt {0}", interruptNumber);
        }

        [Export]
        private void OnCpuHalted()
        {
            IsHalted = true;
        }

        [Export]
        private void OnCpuPowerDown()
        {
            IsHalted = true;
            this.NoisyLog("AVR: CPU powered down (SLEEP)");
        }

#pragma warning disable 649
        [Import]
        private readonly Action<uint> TlibSetEntryPoint;

        [Import]
        private readonly Action TlibClearWfi;

        [Import]
        private readonly Action TlibSetWfi;
#pragma warning restore 649

        private readonly Dictionary<ulong, string> ExceptionDescriptionsMap =
            new Dictionary<ulong, string>
        {
            { 0,  "RESET" },
            { 1,  "INT0 — External interrupt 0" },
            { 2,  "INT1 — External interrupt 1" },
            { 3,  "PCINT0 — Pin change interrupt 0" },
            { 4,  "PCINT1 — Pin change interrupt 1" },
            { 5,  "PCINT2 — Pin change interrupt 2" },
            { 6,  "WDT — Watchdog timer" },
            { 14, "TIMER1_OVF — Timer1 overflow" },
            { 17, "TIMER0_OVF — Timer0 overflow" },
            { 18, "SPI_STC — SPI transfer complete" },
            { 19, "USART_RX — USART receive complete" },
            { 20, "USART_UDRE — USART data register empty" },
            { 21, "USART_TX — USART transmit complete" },
            { 25, "TWI — 2-wire serial interface" },
            { 63, "Undefined instruction" },
        };
    }
}
