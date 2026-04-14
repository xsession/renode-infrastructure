// Auto-generated register mapping for PIC16
#pragma warning disable IDE0005
using System;
using System.Collections.Generic;
using System.Linq;

using Antmicro.Renode.Exceptions;
using Antmicro.Renode.Peripherals.CPU.Registers;
using Antmicro.Renode.Utilities.Binding;
#pragma warning restore IDE0005

namespace Antmicro.Renode.Peripherals.CPU
{
    public partial class PIC16
    {
        public override void SetRegister(int register, RegisterValue value)
        {
            if(!mapping.TryGetValue((PIC16Registers)register, out var r))
            {
                throw new RecoverableException($"Wrong register index: {register}");
            }
            SetRegisterValue32(r.Index, checked((uint)value));
        }

        public override RegisterValue GetRegister(int register)
        {
            if(!mapping.TryGetValue((PIC16Registers)register, out var r))
            {
                throw new RecoverableException($"Wrong register index: {register}");
            }
            return GetRegisterValue32(r.Index);
        }

        public override IEnumerable<CPURegister> GetRegisters()
        {
            return mapping.Values.OrderBy(x => x.Index);
        }

        [Register]
        public override RegisterValue PC
        {
            get => GetRegisterValue32((int)PIC16Registers.PC);
            set
            {
                SetRegisterValue32((int)PIC16Registers.PC, value);
                AfterPCSet(value);
            }
        }

        protected override void InitializeRegisters() { }
        private void AfterPCSet(uint value) { }

#pragma warning disable 649
        [Import(Name = "tlib_set_register_value_32")]
        protected Action<int, uint> SetRegisterValue32;

        [Import(Name = "tlib_get_register_value_32")]
        protected Func<int, uint> GetRegisterValue32;
#pragma warning restore 649

        private static readonly Dictionary<PIC16Registers, CPURegister> mapping =
            new Dictionary<PIC16Registers, CPURegister>
        {
            { PIC16Registers.W,      new CPURegister(0, 8,  isGeneral: true,  isReadonly: false, aliases: new[] { "W"      }) },
            { PIC16Registers.PC,     new CPURegister(1, 16, isGeneral: false, isReadonly: false, aliases: new[] { "PC"     }) },
            { PIC16Registers.STATUS, new CPURegister(2, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "STATUS" }) },
            { PIC16Registers.FSR,    new CPURegister(3, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "FSR"    }) },
            { PIC16Registers.PCLATH, new CPURegister(4, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "PCLATH" }) },
            { PIC16Registers.INTCON, new CPURegister(5, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "INTCON" }) },
            { PIC16Registers.OPTION, new CPURegister(6, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "OPTION" }) },
        };
    }

    public enum PIC16Registers
    {
        W      = 0,
        PC     = 1,
        STATUS = 2,
        FSR    = 3,
        PCLATH = 4,
        INTCON = 5,
        OPTION = 6,
    }
}
