//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
//
// Microchip PIC16 (Baseline/Mid-Range) 8-bit MCU emulation core.
//
// Architecture notes:
//   - 14-bit instruction words (mid-range)
//   - W register (accumulator) + file registers
//   - 13-bit PC, 8-level hardware stack
//   - Data memory: General-purpose + SFR registers
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
    public partial class PIC16 : TranslationCPU
    {
        public PIC16(string cpuType, IMachine machine)
            : base(cpuType, machine, Endianess.LittleEndian)
        {
        }

        public override string Architecture     => "pic16";
        public override string GDBArchitecture  => "pic16";
        public override string[] AllLLVMTriples => new[] { "pic16" };
        public override string LLVMModel        => "pic16";
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
                this.Log(LogLevel.Warning, "PIC16: GPIO index {0} out of range", number);
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
            this.NoisyLog("PIC16: Acknowledged interrupt {0}", interruptNumber);
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
            this.NoisyLog("PIC16: CPU entered SLEEP");
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
            { 0, "Reset" },
            { 4, "Interrupt" },
            { 63, "Undefined instruction" },
        };
    }
}
