// Auto-generated register mapping for C2000
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
    public partial class C2000
    {
        public override void SetRegister(int register, RegisterValue value)
        {
            if(!mapping.TryGetValue((C2000Registers)register, out var r))
            {
                throw new RecoverableException($"Wrong register index: {register}");
            }
            SetRegisterValue32(r.Index, checked((uint)value));
        }

        public override RegisterValue GetRegister(int register)
        {
            if(!mapping.TryGetValue((C2000Registers)register, out var r))
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
            get => GetRegisterValue32((int)C2000Registers.PC);
            set
            {
                SetRegisterValue32((int)C2000Registers.PC, value);
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

        private static readonly Dictionary<C2000Registers, CPURegister> mapping =
            new Dictionary<C2000Registers, CPURegister>
        {
            { C2000Registers.ACC,  new CPURegister( 0, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "ACC"  }) },
            { C2000Registers.AH,   new CPURegister( 1, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AH"   }) },
            { C2000Registers.AL,   new CPURegister( 2, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AL"   }) },
            { C2000Registers.P,    new CPURegister( 3, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "P"    }) },
            { C2000Registers.PH,   new CPURegister( 4, 16, isGeneral: false, isReadonly: false, aliases: new[] { "PH"   }) },
            { C2000Registers.PL,   new CPURegister( 5, 16, isGeneral: false, isReadonly: false, aliases: new[] { "PL"   }) },
            { C2000Registers.XT,   new CPURegister( 6, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "XT"   }) },
            { C2000Registers.T,    new CPURegister( 7, 16, isGeneral: false, isReadonly: false, aliases: new[] { "T"    }) },
            { C2000Registers.XAR0, new CPURegister( 8, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "XAR0" }) },
            { C2000Registers.XAR1, new CPURegister( 9, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "XAR1" }) },
            { C2000Registers.XAR2, new CPURegister(10, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "XAR2" }) },
            { C2000Registers.XAR3, new CPURegister(11, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "XAR3" }) },
            { C2000Registers.XAR4, new CPURegister(12, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "XAR4" }) },
            { C2000Registers.XAR5, new CPURegister(13, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "XAR5" }) },
            { C2000Registers.XAR6, new CPURegister(14, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "XAR6" }) },
            { C2000Registers.XAR7, new CPURegister(15, 32, isGeneral: true,  isReadonly: false, aliases: new[] { "XAR7" }) },
            { C2000Registers.AR0,  new CPURegister(16, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AR0"  }) },
            { C2000Registers.AR1,  new CPURegister(17, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AR1"  }) },
            { C2000Registers.AR2,  new CPURegister(18, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AR2"  }) },
            { C2000Registers.AR3,  new CPURegister(19, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AR3"  }) },
            { C2000Registers.AR4,  new CPURegister(20, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AR4"  }) },
            { C2000Registers.AR5,  new CPURegister(21, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AR5"  }) },
            { C2000Registers.AR6,  new CPURegister(22, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AR6"  }) },
            { C2000Registers.AR7,  new CPURegister(23, 16, isGeneral: false, isReadonly: false, aliases: new[] { "AR7"  }) },
            { C2000Registers.SP,   new CPURegister(24, 16, isGeneral: false, isReadonly: false, aliases: new[] { "SP"   }) },
            { C2000Registers.PC,   new CPURegister(25, 32, isGeneral: false, isReadonly: false, aliases: new[] { "PC"   }) },
            { C2000Registers.RPC,  new CPURegister(26, 32, isGeneral: false, isReadonly: false, aliases: new[] { "RPC"  }) },
            { C2000Registers.ST0,  new CPURegister(27, 16, isGeneral: false, isReadonly: false, aliases: new[] { "ST0"  }) },
            { C2000Registers.ST1,  new CPURegister(28, 16, isGeneral: false, isReadonly: false, aliases: new[] { "ST1"  }) },
            { C2000Registers.IFR,  new CPURegister(29, 16, isGeneral: false, isReadonly: false, aliases: new[] { "IFR"  }) },
            { C2000Registers.IER,  new CPURegister(30, 16, isGeneral: false, isReadonly: false, aliases: new[] { "IER"  }) },
            { C2000Registers.DP,   new CPURegister(31, 16, isGeneral: false, isReadonly: false, aliases: new[] { "DP"   }) },
        };
    }

    public enum C2000Registers
    {
        ACC  = 0,  AH   = 1,  AL   = 2,
        P    = 3,  PH   = 4,  PL   = 5,
        XT   = 6,  T    = 7,
        XAR0 = 8,  XAR1 = 9,  XAR2 = 10, XAR3 = 11,
        XAR4 = 12, XAR5 = 13, XAR6 = 14, XAR7 = 15,
        AR0  = 16, AR1  = 17, AR2  = 18, AR3  = 19,
        AR4  = 20, AR5  = 21, AR6  = 22, AR7  = 23,
        SP   = 24, PC   = 25, RPC  = 26,
        ST0  = 27, ST1  = 28,
        IFR  = 29, IER  = 30, DP   = 31,
    }
}
