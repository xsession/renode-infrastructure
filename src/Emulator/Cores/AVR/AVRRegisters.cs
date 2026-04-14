/********************************************************
*
* Warning!
* This file was generated automatically.
* Please do not edit. Changes should be made in the
* appropriate *.tt file.
*
*/
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
    public partial class AVR
    {
        public override void SetRegister(int register, RegisterValue value)
        {
            if(!mapping.TryGetValue((AVRRegisters)register, out var r))
            {
                throw new RecoverableException($"Wrong register index: {register}");
            }
            SetRegisterValue32(r.Index, checked((uint)value));
        }

        public override RegisterValue GetRegister(int register)
        {
            if(!mapping.TryGetValue((AVRRegisters)register, out var r))
            {
                throw new RecoverableException($"Wrong register index: {register}");
            }
            return GetRegisterValue32(r.Index);
        }

        public override IEnumerable<CPURegister> GetRegisters()
        {
            return mapping.Values.OrderBy(x => x.Index);
        }

        // Individual register properties

        [Register] public RegisterValue R0  { get => GetRegisterValue32((int)AVRRegisters.R0);  set => SetRegisterValue32((int)AVRRegisters.R0,  value); }
        [Register] public RegisterValue R1  { get => GetRegisterValue32((int)AVRRegisters.R1);  set => SetRegisterValue32((int)AVRRegisters.R1,  value); }
        [Register] public RegisterValue R2  { get => GetRegisterValue32((int)AVRRegisters.R2);  set => SetRegisterValue32((int)AVRRegisters.R2,  value); }
        [Register] public RegisterValue R3  { get => GetRegisterValue32((int)AVRRegisters.R3);  set => SetRegisterValue32((int)AVRRegisters.R3,  value); }
        [Register] public RegisterValue R4  { get => GetRegisterValue32((int)AVRRegisters.R4);  set => SetRegisterValue32((int)AVRRegisters.R4,  value); }
        [Register] public RegisterValue R5  { get => GetRegisterValue32((int)AVRRegisters.R5);  set => SetRegisterValue32((int)AVRRegisters.R5,  value); }
        [Register] public RegisterValue R6  { get => GetRegisterValue32((int)AVRRegisters.R6);  set => SetRegisterValue32((int)AVRRegisters.R6,  value); }
        [Register] public RegisterValue R7  { get => GetRegisterValue32((int)AVRRegisters.R7);  set => SetRegisterValue32((int)AVRRegisters.R7,  value); }
        [Register] public RegisterValue R8  { get => GetRegisterValue32((int)AVRRegisters.R8);  set => SetRegisterValue32((int)AVRRegisters.R8,  value); }
        [Register] public RegisterValue R9  { get => GetRegisterValue32((int)AVRRegisters.R9);  set => SetRegisterValue32((int)AVRRegisters.R9,  value); }
        [Register] public RegisterValue R10 { get => GetRegisterValue32((int)AVRRegisters.R10); set => SetRegisterValue32((int)AVRRegisters.R10, value); }
        [Register] public RegisterValue R11 { get => GetRegisterValue32((int)AVRRegisters.R11); set => SetRegisterValue32((int)AVRRegisters.R11, value); }
        [Register] public RegisterValue R12 { get => GetRegisterValue32((int)AVRRegisters.R12); set => SetRegisterValue32((int)AVRRegisters.R12, value); }
        [Register] public RegisterValue R13 { get => GetRegisterValue32((int)AVRRegisters.R13); set => SetRegisterValue32((int)AVRRegisters.R13, value); }
        [Register] public RegisterValue R14 { get => GetRegisterValue32((int)AVRRegisters.R14); set => SetRegisterValue32((int)AVRRegisters.R14, value); }
        [Register] public RegisterValue R15 { get => GetRegisterValue32((int)AVRRegisters.R15); set => SetRegisterValue32((int)AVRRegisters.R15, value); }
        [Register] public RegisterValue R16 { get => GetRegisterValue32((int)AVRRegisters.R16); set => SetRegisterValue32((int)AVRRegisters.R16, value); }
        [Register] public RegisterValue R17 { get => GetRegisterValue32((int)AVRRegisters.R17); set => SetRegisterValue32((int)AVRRegisters.R17, value); }
        [Register] public RegisterValue R18 { get => GetRegisterValue32((int)AVRRegisters.R18); set => SetRegisterValue32((int)AVRRegisters.R18, value); }
        [Register] public RegisterValue R19 { get => GetRegisterValue32((int)AVRRegisters.R19); set => SetRegisterValue32((int)AVRRegisters.R19, value); }
        [Register] public RegisterValue R20 { get => GetRegisterValue32((int)AVRRegisters.R20); set => SetRegisterValue32((int)AVRRegisters.R20, value); }
        [Register] public RegisterValue R21 { get => GetRegisterValue32((int)AVRRegisters.R21); set => SetRegisterValue32((int)AVRRegisters.R21, value); }
        [Register] public RegisterValue R22 { get => GetRegisterValue32((int)AVRRegisters.R22); set => SetRegisterValue32((int)AVRRegisters.R22, value); }
        [Register] public RegisterValue R23 { get => GetRegisterValue32((int)AVRRegisters.R23); set => SetRegisterValue32((int)AVRRegisters.R23, value); }
        [Register] public RegisterValue R24 { get => GetRegisterValue32((int)AVRRegisters.R24); set => SetRegisterValue32((int)AVRRegisters.R24, value); }
        [Register] public RegisterValue R25 { get => GetRegisterValue32((int)AVRRegisters.R25); set => SetRegisterValue32((int)AVRRegisters.R25, value); }
        [Register] public RegisterValue R26 { get => GetRegisterValue32((int)AVRRegisters.R26); set => SetRegisterValue32((int)AVRRegisters.R26, value); }
        [Register] public RegisterValue R27 { get => GetRegisterValue32((int)AVRRegisters.R27); set => SetRegisterValue32((int)AVRRegisters.R27, value); }
        [Register] public RegisterValue R28 { get => GetRegisterValue32((int)AVRRegisters.R28); set => SetRegisterValue32((int)AVRRegisters.R28, value); }
        [Register] public RegisterValue R29 { get => GetRegisterValue32((int)AVRRegisters.R29); set => SetRegisterValue32((int)AVRRegisters.R29, value); }
        [Register] public RegisterValue R30 { get => GetRegisterValue32((int)AVRRegisters.R30); set => SetRegisterValue32((int)AVRRegisters.R30, value); }
        [Register] public RegisterValue R31 { get => GetRegisterValue32((int)AVRRegisters.R31); set => SetRegisterValue32((int)AVRRegisters.R31, value); }

        [Register]
        public override RegisterValue PC
        {
            get => GetRegisterValue32((int)AVRRegisters.PC);
            set
            {
                SetRegisterValue32((int)AVRRegisters.PC, value);
                AfterPCSet(value);
            }
        }

        [Register] public RegisterValue SREG  { get => GetRegisterValue32((int)AVRRegisters.SREG);  set => SetRegisterValue32((int)AVRRegisters.SREG,  value); }
        [Register] public RegisterValue SP    { get => GetRegisterValue32((int)AVRRegisters.SP);    set => SetRegisterValue32((int)AVRRegisters.SP,    value); }
        [Register] public RegisterValue RAMPZ { get => GetRegisterValue32((int)AVRRegisters.RAMPZ); set => SetRegisterValue32((int)AVRRegisters.RAMPZ, value); }
        [Register] public RegisterValue EIND  { get => GetRegisterValue32((int)AVRRegisters.EIND);  set => SetRegisterValue32((int)AVRRegisters.EIND,  value); }

        // R register group: R[0]..R[31]

        public RegistersGroup R { get; private set; }

#pragma warning disable SA1508
        protected override void InitializeRegisters()
        {
            var indexValueMapR = new Dictionary<int, AVRRegisters>();
            for(int i = 0; i <= 31; i++)
            {
                indexValueMapR[i] = (AVRRegisters)i;
            }
            R = new RegistersGroup(
                indexValueMapR.Keys,
                i => GetRegister((int)indexValueMapR[i]),
                (i, v) => SetRegister((int)indexValueMapR[i], v));
        }
#pragma warning restore SA1508

        private void AfterPCSet(uint value)
        {
        }

#pragma warning disable 649
        [Import(Name = "tlib_set_register_value_32")]
        protected Action<int, uint> SetRegisterValue32;

        [Import(Name = "tlib_get_register_value_32")]
        protected Func<int, uint> GetRegisterValue32;
#pragma warning restore 649

        private static readonly Dictionary<AVRRegisters, CPURegister> mapping =
            new Dictionary<AVRRegisters, CPURegister>
        {
            { AVRRegisters.R0,    new CPURegister( 0, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R0"    }) },
            { AVRRegisters.R1,    new CPURegister( 1, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R1"    }) },
            { AVRRegisters.R2,    new CPURegister( 2, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R2"    }) },
            { AVRRegisters.R3,    new CPURegister( 3, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R3"    }) },
            { AVRRegisters.R4,    new CPURegister( 4, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R4"    }) },
            { AVRRegisters.R5,    new CPURegister( 5, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R5"    }) },
            { AVRRegisters.R6,    new CPURegister( 6, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R6"    }) },
            { AVRRegisters.R7,    new CPURegister( 7, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R7"    }) },
            { AVRRegisters.R8,    new CPURegister( 8, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R8"    }) },
            { AVRRegisters.R9,    new CPURegister( 9, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R9"    }) },
            { AVRRegisters.R10,   new CPURegister(10, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R10"   }) },
            { AVRRegisters.R11,   new CPURegister(11, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R11"   }) },
            { AVRRegisters.R12,   new CPURegister(12, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R12"   }) },
            { AVRRegisters.R13,   new CPURegister(13, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R13"   }) },
            { AVRRegisters.R14,   new CPURegister(14, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R14"   }) },
            { AVRRegisters.R15,   new CPURegister(15, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R15"   }) },
            { AVRRegisters.R16,   new CPURegister(16, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R16"   }) },
            { AVRRegisters.R17,   new CPURegister(17, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R17"   }) },
            { AVRRegisters.R18,   new CPURegister(18, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R18"   }) },
            { AVRRegisters.R19,   new CPURegister(19, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R19"   }) },
            { AVRRegisters.R20,   new CPURegister(20, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R20"   }) },
            { AVRRegisters.R21,   new CPURegister(21, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R21"   }) },
            { AVRRegisters.R22,   new CPURegister(22, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R22"   }) },
            { AVRRegisters.R23,   new CPURegister(23, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R23"   }) },
            { AVRRegisters.R24,   new CPURegister(24, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R24"   }) },
            { AVRRegisters.R25,   new CPURegister(25, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R25"   }) },
            { AVRRegisters.R26,   new CPURegister(26, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R26", "XL" }) },
            { AVRRegisters.R27,   new CPURegister(27, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R27", "XH" }) },
            { AVRRegisters.R28,   new CPURegister(28, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R28", "YL" }) },
            { AVRRegisters.R29,   new CPURegister(29, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R29", "YH" }) },
            { AVRRegisters.R30,   new CPURegister(30, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R30", "ZL" }) },
            { AVRRegisters.R31,   new CPURegister(31, 8, isGeneral: true,  isReadonly: false, aliases: new[] { "R31", "ZH" }) },
            { AVRRegisters.PC,    new CPURegister(32, 32, isGeneral: false, isReadonly: false, aliases: new[] { "PC"   }) },
            { AVRRegisters.SREG,  new CPURegister(33, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "SREG" }) },
            { AVRRegisters.SP,    new CPURegister(34, 16, isGeneral: false, isReadonly: false, aliases: new[] { "SP"   }) },
            { AVRRegisters.RAMPZ, new CPURegister(35, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "RAMPZ"}) },
            { AVRRegisters.EIND,  new CPURegister(36, 8,  isGeneral: false, isReadonly: false, aliases: new[] { "EIND" }) },
        };
    }

    public enum AVRRegisters
    {
        R0  =  0,  R1  =  1,  R2  =  2,  R3  =  3,
        R4  =  4,  R5  =  5,  R6  =  6,  R7  =  7,
        R8  =  8,  R9  =  9,  R10 = 10,  R11 = 11,
        R12 = 12,  R13 = 13,  R14 = 14,  R15 = 15,
        R16 = 16,  R17 = 17,  R18 = 18,  R19 = 19,
        R20 = 20,  R21 = 21,  R22 = 22,  R23 = 23,
        R24 = 24,  R25 = 25,  R26 = 26,  R27 = 27,
        R28 = 28,  R29 = 29,  R30 = 30,  R31 = 31,
        PC   = 32,
        SREG = 33,
        SP   = 34,
        RAMPZ = 35,
        EIND  = 36,
    }
}
