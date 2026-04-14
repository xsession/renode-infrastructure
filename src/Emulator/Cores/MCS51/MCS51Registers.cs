// Auto-generated register mapping for MCS51
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
    public partial class MCS51
    {
        public override void SetRegister(int register, RegisterValue value)
        {
            if(!mapping.TryGetValue((MCS51Registers)register, out var r))
            {
                throw new RecoverableException($"Wrong register index: {register}");
            }
            SetRegisterValue32(r.Index, checked((uint)value));
        }

        public override RegisterValue GetRegister(int register)
        {
            if(!mapping.TryGetValue((MCS51Registers)register, out var r))
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
            get => GetRegisterValue32((int)MCS51Registers.PC);
            set
            {
                SetRegisterValue32((int)MCS51Registers.PC, value);
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

        private static readonly Dictionary<MCS51Registers, CPURegister> mapping =
            new Dictionary<MCS51Registers, CPURegister>
        {
            { MCS51Registers.ACC,   new CPURegister(0, 8,  isGeneral: true,  isReadonly: false, aliases: new[] { "ACC" }) },
            { MCS51Registers.B,     new CPURegister(1, 8,  isGeneral: true,  isReadonly: false, aliases: new[] { "B"   }) },
            { MCS51Registers.PSW,   new CPURegister(2, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "PSW" }) },
            { MCS51Registers.SP,    new CPURegister(3, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "SP"  }) },
            { MCS51Registers.DPL,   new CPURegister(4, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "DPL" }) },
            { MCS51Registers.DPH,   new CPURegister(5, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "DPH" }) },
            { MCS51Registers.PC,    new CPURegister(6, 16, isGeneral: false, isReadonly: false, aliases: new[] { "PC"  }) },
            { MCS51Registers.IE,    new CPURegister(7, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "IE"  }) },
            { MCS51Registers.IP,    new CPURegister(8, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "IP"  }) },
        };
    }

    public enum MCS51Registers
    {
        ACC = 0,
        B   = 1,
        PSW = 2,
        SP  = 3,
        DPL = 4,
        DPH = 5,
        PC  = 6,
        IE  = 7,
        IP  = 8,
    }
}
