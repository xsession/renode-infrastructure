// Auto-generated register mapping for PIC18
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
    public partial class PIC18
    {
        public override void SetRegister(int register, RegisterValue value)
        {
            if(!mapping.TryGetValue((PIC18Registers)register, out var r))
            {
                throw new RecoverableException($"Wrong register index: {register}");
            }
            SetRegisterValue32(r.Index, checked((uint)value));
        }

        public override RegisterValue GetRegister(int register)
        {
            if(!mapping.TryGetValue((PIC18Registers)register, out var r))
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
            get => GetRegisterValue32((int)PIC18Registers.PC);
            set
            {
                SetRegisterValue32((int)PIC18Registers.PC, value);
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

        private static readonly Dictionary<PIC18Registers, CPURegister> mapping =
            new Dictionary<PIC18Registers, CPURegister>
        {
            { PIC18Registers.WREG,   new CPURegister( 0, 8,  isGeneral: true,  isReadonly: false, aliases: new[] { "WREG"   }) },
            { PIC18Registers.BSR,    new CPURegister( 1, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "BSR"    }) },
            { PIC18Registers.STATUS, new CPURegister( 2, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "STATUS" }) },
            { PIC18Registers.PC,     new CPURegister( 3, 32, isGeneral: false, isReadonly: false, aliases: new[] { "PC"     }) },
            { PIC18Registers.FSR0,   new CPURegister( 4, 16, isGeneral: false, isReadonly: false, aliases: new[] { "FSR0"   }) },
            { PIC18Registers.FSR1,   new CPURegister( 5, 16, isGeneral: false, isReadonly: false, aliases: new[] { "FSR1"   }) },
            { PIC18Registers.FSR2,   new CPURegister( 6, 16, isGeneral: false, isReadonly: false, aliases: new[] { "FSR2"   }) },
            { PIC18Registers.STKPTR, new CPURegister( 7, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "STKPTR" }) },
            { PIC18Registers.TOS,    new CPURegister( 8, 32, isGeneral: false, isReadonly: false, aliases: new[] { "TOS"    }) },
            { PIC18Registers.PRODH,  new CPURegister( 9, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "PRODH"  }) },
            { PIC18Registers.PRODL,  new CPURegister(10, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "PRODL"  }) },
            { PIC18Registers.TABLAT, new CPURegister(11, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "TABLAT" }) },
            { PIC18Registers.TBLPTR, new CPURegister(12, 32, isGeneral: false, isReadonly: false, aliases: new[] { "TBLPTR" }) },
            { PIC18Registers.INTCON, new CPURegister(13, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "INTCON" }) },
        };
    }

    public enum PIC18Registers
    {
        WREG   = 0,
        BSR    = 1,
        STATUS = 2,
        PC     = 3,
        FSR0   = 4,
        FSR1   = 5,
        FSR2   = 6,
        STKPTR = 7,
        TOS    = 8,
        PRODH  = 9,
        PRODL  = 10,
        TABLAT = 11,
        TBLPTR = 12,
        INTCON = 13,
    }
}
