// Auto-generated register mapping for STM8
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
    public partial class STM8
    {
        public override void SetRegister(int register, RegisterValue value)
        {
            if(!mapping.TryGetValue((STM8Registers)register, out var r))
            {
                throw new RecoverableException($"Wrong register index: {register}");
            }
            SetRegisterValue32(r.Index, checked((uint)value));
        }

        public override RegisterValue GetRegister(int register)
        {
            if(!mapping.TryGetValue((STM8Registers)register, out var r))
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
            get => GetRegisterValue32((int)STM8Registers.PC);
            set
            {
                SetRegisterValue32((int)STM8Registers.PC, value);
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

        private static readonly Dictionary<STM8Registers, CPURegister> mapping =
            new Dictionary<STM8Registers, CPURegister>
        {
            { STM8Registers.A,   new CPURegister(0, 8,  isGeneral: true,  isReadonly: false, aliases: new[] { "A"  }) },
            { STM8Registers.X,   new CPURegister(1, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "X"  }) },
            { STM8Registers.XH,  new CPURegister(2, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "XH" }) },
            { STM8Registers.XL,  new CPURegister(3, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "XL" }) },
            { STM8Registers.Y,   new CPURegister(4, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "Y"  }) },
            { STM8Registers.YH,  new CPURegister(5, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "YH" }) },
            { STM8Registers.YL,  new CPURegister(6, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "YL" }) },
            { STM8Registers.SP,  new CPURegister(7, 16, isGeneral: false, isReadonly: false, aliases: new[] { "SP" }) },
            { STM8Registers.PC,  new CPURegister(8, 32, isGeneral: false, isReadonly: false, aliases: new[] { "PC" }) },
            { STM8Registers.CC,  new CPURegister(9, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "CC" }) },
        };
    }

    public enum STM8Registers
    {
        A  = 0,
        X  = 1,
        XH = 2,
        XL = 3,
        Y  = 4,
        YH = 5,
        YL = 6,
        SP = 7,
        PC = 8,
        CC = 9,
    }
}
