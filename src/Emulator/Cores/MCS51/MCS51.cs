//
// Copyright (c) Antmicro
// This file is licensed under the MIT License.
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
    public partial class MCS51 : TranslationCPU
    {
        public MCS51(string cpuType, IMachine machine)
            : base(cpuType, machine, Endianess.LittleEndian)
        {
        }

        public override string Architecture     => "mcs51";
        public override string GDBArchitecture  => "mcs51";
        public override string[] AllLLVMTriples => new[] { "mcs51" };
        public override string LLVMModel        => "mcs51";
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
                this.Log(LogLevel.Warning, "MCS51: GPIO index {0} out of range", number);
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
            this.NoisyLog("MCS51: Acknowledged interrupt {0}", interruptNumber);
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
            this.NoisyLog("MCS51: CPU powered down");
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
            { 0, "RESET" },
            { 1, "EXT0 — External interrupt 0" },
            { 2, "TIMER0 — Timer 0 overflow" },
            { 3, "EXT1 — External interrupt 1" },
            { 4, "TIMER1 — Timer 1 overflow" },
            { 5, "SERIAL — Serial port" },
            { 63, "Undefined instruction" },
        };
    }
}
