// Copyright (c) Antmicro
// Copyright (c) 2026 C2000 interrupt extension contributors
// This file is licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Utilities.Binding;

using Endianess = ELFSharp.ELF.Endianess;

namespace Antmicro.Renode.Peripherals.CPU
{
    [GPIO(NumberOfInputs = 16)]
    public partial class C2000 : TranslationCPU
    {
        public C2000(string cpuType, IMachine machine) : base(cpuType, machine, Endianess.LittleEndian)
        {
        }

        public override string Architecture => "c2000";
        public override string GDBArchitecture => "c2000";
        public override string[] AllLLVMTriples => new[] { "c2000" };
        public override string LLVMModel => "c2000";
        public override string GetLLVMTriple(uint flags) => AllLLVMTriples[0];
        public override Endianess DisassemblyHexFormatting => Endianess.LittleEndian;
        public override List<GDBFeatureDescriptor> GDBFeatures => new List<GDBFeatureDescriptor>();

        public override void OnGPIO(int number, bool value)
        {
            if(number < 0 || number >= 16)
            {
                this.Log(LogLevel.Warning, "C2000: GPIO index {0} out of range", number);
                return;
            }
            lock(pendingInterrupts)
            {
                if(value) pendingInterrupts.Add(number);
                else pendingInterrupts.Remove(number);
                base.OnGPIO(0, pendingInterrupts.Count != 0);
            }
        }

        protected override Interrupt DecodeInterrupt(int number)
        {
            if(number >= 0 && number < 16) return Interrupt.Hard;
            throw InvalidInterruptNumberException;
        }

        protected override string GetExceptionDescription(ulong exceptionIndex)
        {
            return ExceptionDescriptionsMap.TryGetValue(exceptionIndex, out var result)
                ? result : base.GetExceptionDescription(exceptionIndex);
        }

        [Export]
        private int FindBestInterrupt()
        {
            lock(pendingInterrupts)
            {
                // Input 0 is NMI/reset-class; INT1..INT14 retain C28x priority order.
                return pendingInterrupts.Count == 0 ? -1 : pendingInterrupts.Min();
            }
        }

        [Export]
        private void AcknowledgeInterrupt(int interruptNumber)
        {
            lock(pendingInterrupts)
            {
                pendingInterrupts.Remove(interruptNumber);
                base.OnGPIO(0, pendingInterrupts.Count != 0);
            }
            this.NoisyLog("C2000: Acknowledged interrupt {0}", interruptNumber);
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
            this.NoisyLog("C2000: CPU entered IDLE");
        }

#pragma warning disable 649
        [Import]
        private readonly Action<uint> TlibSetEntryPoint;
        [Import]
        private readonly Action TlibClearWfi;
        [Import]
        private readonly Action TlibSetWfi;
#pragma warning restore 649

        private readonly SortedSet<int> pendingInterrupts = new SortedSet<int>();
        private readonly Dictionary<ulong, string> ExceptionDescriptionsMap = new Dictionary<ulong, string>
        {
            { 0, "RESET" }, { 1, "NMI" }, { 2, "INT1" }, { 3, "INT2" },
            { 4, "INT3" }, { 5, "INT4" }, { 6, "INT5" }, { 7, "INT6" },
            { 8, "INT7" }, { 9, "INT8" }, { 10, "INT9" }, { 11, "INT10" },
            { 12, "INT11" }, { 13, "INT12" }, { 14, "INT13" }, { 15, "INT14" },
            { 63, "Undefined instruction" },
        };
    }
}
