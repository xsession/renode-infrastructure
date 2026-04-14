// Auto-generated register mapping for dsPIC33
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
    public partial class dsPIC33
    {
        public override void SetRegister(int register, RegisterValue value)
        {
            if(!mapping.TryGetValue((dsPIC33Registers)register, out var r))
            {
                throw new RecoverableException($"Wrong register index: {register}");
            }
            SetRegisterValue32(r.Index, checked((uint)value));
        }

        public override RegisterValue GetRegister(int register)
        {
            if(!mapping.TryGetValue((dsPIC33Registers)register, out var r))
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
            get => GetRegisterValue32((int)dsPIC33Registers.PC);
            set
            {
                SetRegisterValue32((int)dsPIC33Registers.PC, value);
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

        private static readonly Dictionary<dsPIC33Registers, CPURegister> mapping =
            new Dictionary<dsPIC33Registers, CPURegister>
        {
            { dsPIC33Registers.W0,      new CPURegister( 0, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W0"      }) },
            { dsPIC33Registers.W1,      new CPURegister( 1, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W1"      }) },
            { dsPIC33Registers.W2,      new CPURegister( 2, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W2"      }) },
            { dsPIC33Registers.W3,      new CPURegister( 3, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W3"      }) },
            { dsPIC33Registers.W4,      new CPURegister( 4, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W4"      }) },
            { dsPIC33Registers.W5,      new CPURegister( 5, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W5"      }) },
            { dsPIC33Registers.W6,      new CPURegister( 6, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W6"      }) },
            { dsPIC33Registers.W7,      new CPURegister( 7, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W7"      }) },
            { dsPIC33Registers.W8,      new CPURegister( 8, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W8"      }) },
            { dsPIC33Registers.W9,      new CPURegister( 9, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W9"      }) },
            { dsPIC33Registers.W10,     new CPURegister(10, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W10"     }) },
            { dsPIC33Registers.W11,     new CPURegister(11, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W11"     }) },
            { dsPIC33Registers.W12,     new CPURegister(12, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W12"     }) },
            { dsPIC33Registers.W13,     new CPURegister(13, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W13"     }) },
            { dsPIC33Registers.W14,     new CPURegister(14, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W14"     }) },
            { dsPIC33Registers.W15,     new CPURegister(15, 16, isGeneral: true,  isReadonly: false, aliases: new[] { "W15", "SP" }) },
            { dsPIC33Registers.PC,      new CPURegister(16, 32, isGeneral: false, isReadonly: false, aliases: new[] { "PC"      }) },
            { dsPIC33Registers.STATUS,  new CPURegister(17, 16, isGeneral: false, isReadonly: false, aliases: new[] { "STATUS", "SR" }) },
            { dsPIC33Registers.TBLPAG,  new CPURegister(18, 16, isGeneral: false, isReadonly: false, aliases: new[] { "TBLPAG"  }) },
            { dsPIC33Registers.PSVPAG,  new CPURegister(19, 16, isGeneral: false, isReadonly: false, aliases: new[] { "PSVPAG"  }) },
            { dsPIC33Registers.RCOUNT,  new CPURegister(20, 16, isGeneral: false, isReadonly: false, aliases: new[] { "RCOUNT"  }) },
            { dsPIC33Registers.DCOUNT,  new CPURegister(21, 16, isGeneral: false, isReadonly: false, aliases: new[] { "DCOUNT"  }) },
            { dsPIC33Registers.CORCON,  new CPURegister(22, 16, isGeneral: false, isReadonly: false, aliases: new[] { "CORCON"  }) },
            { dsPIC33Registers.DISICNT, new CPURegister(23, 16, isGeneral: false, isReadonly: false, aliases: new[] { "DISICNT" }) },
        };
    }

    public enum dsPIC33Registers
    {
        W0  = 0,  W1  = 1,  W2  = 2,  W3  = 3,
        W4  = 4,  W5  = 5,  W6  = 6,  W7  = 7,
        W8  = 8,  W9  = 9,  W10 = 10, W11 = 11,
        W12 = 12, W13 = 13, W14 = 14, W15 = 15,
        PC      = 16,
        STATUS  = 17,
        TBLPAG  = 18,
        PSVPAG  = 19,
        RCOUNT  = 20,
        DCOUNT  = 21,
        CORCON  = 22,
        DISICNT = 23,
    }
}
