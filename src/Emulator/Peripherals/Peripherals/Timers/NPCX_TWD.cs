//
// Copyright (c) 2010-2023 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using System.Collections.Generic;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Time;

namespace Antmicro.Renode.Peripherals.Timers
{
    [AllowedTranslations(AllowedTranslation.ByteToWord | AllowedTranslation.WordToByte)]
    public class NPCX_TWD : IWordPeripheral, IProvidesRegisterCollection<WordRegisterCollection>, IKnownSize
    {
        public NPCX_TWD(IMachine machine)
        {
            this.machine = machine;

            RegistersCollection = new WordRegisterCollection(this, BuildRegisterMap());

            periodicInterruptTimer = new LimitTimer(machine.ClockSource, DefaultFrequency, this, "PeriodicInterruptTimer", 0xFFFF, eventEnabled: true);
            periodicInterruptTimer.LimitReached += () =>
            {
                IRQ.Blink();
                terminalCountReached = true;
                HandleWatchdogTickWith(TimerWatchdogTick);
            };

            watchdog = new Watchdog(this, 0xFF);
            watchdog.LimitReached += WatchdogAlarmHandler;

            watchdogCounter = new LimitTimer(machine.ClockSource, DefaultFrequency, this, "WatchdogCounter", 0x1, eventEnabled: true);
            watchdogCounter.LimitReached += () => HandleWatchdogTickWith(CounterWatchdogTick);

            IRQ = new GPIO();
            Reset();
        }

        public void Reset()
        {
            RegistersCollection.Reset();
            periodicInterruptTimer.Reset();

            watchdogCounter.Reset();
            watchdog.Reset();

            IRQ.Unset();

            byteInSequence = StopUnlockSequence.None;
            timerAndWatchdogPrescaler = DefaultDivider;
            watchdogPrescaler = DefaultDivider;
            terminalCountReached = false;
            watchdogCounterPresetValue = WatchdogCounterMaxValue;

            CounterWatchdogTick -= watchdog.Tick;
            TimerWatchdogTick -= watchdog.Tick;
            TimerWatchdogTick += watchdog.Tick;
        }

        public ushort ReadWord(long offset)
        {
            return RegistersCollection.Read(offset);
        }

        public void WriteWord(long offset, ushort value)
        {
            RegistersCollection.Write(offset, value);
        }

        public GPIO IRQ { get; }

        public long Size => 0x1000;

        public WordRegisterCollection RegistersCollection { get; }

        public event Action CounterWatchdogTick;

        public event Action TimerWatchdogTick;

        private Dictionary<long, WordRegister> BuildRegisterMap()
        {
            var registerMap = new Dictionary<long, WordRegister>
            {
                {(long)Registers.TimerAndWatchdogConfiguration, new WordRegister(this)
                    .WithReservedBits(6, 10)
                    .WithFlag(5, out watchdogTouchSelect,
                        writeCallback: (_, val) =>
                        {
                            if(!val)
                            {
                                return;
                            }
                            watchdog.Value = watchdogCounterPresetValue;
                        },
                        name: "WDSDME (Watchdog Touch Select)")
                    .WithFlag(4, out isCounterClockSource,
                        writeCallback: (_, val) =>
                        {
                            if(val)
                            {
                                CounterWatchdogTick += watchdog.Tick;
                                watchdogCounter.Enabled = true;
                                
                                TimerWatchdogTick -= watchdog.Tick;
                            }
                            else
                            {
                                TimerWatchdogTick += watchdog.Tick;

                                watchdogCounter.Enabled = false;
                                CounterWatchdogTick -= watchdog.Tick;
                            }
                        },
                        name: "WDCT0I (Watchdog Clock Select)")
                    .WithFlag(3, out lockWatchdog, FieldMode.Set, name: "LWDCNT (Lock Watchdog Counter)")
                    .WithFlag(2, out lockTimer, FieldMode.Set, name: "LTWDT0 (Lock T0 Timer)")
                    .WithFlag(1, out lockPrescalers, FieldMode.Set, name: "LTWCP (Lock Prescalers)")
                    .WithFlag(0, out lockWatchdogConfig, FieldMode.Set, name: "LTWCFG (Lock Watchdog Configuration)")
                },

                {(long)Registers.TimerAndWatchdogClockPrescaler, new WordRegister(this)
                    .WithReservedBits(4, 12)
                    .WithValueField(0, 4,
                        writeCallback: (__, val) =>
                        {
                            if(lockPrescalers.Value)
                            {
                                this.Log(LogLevel.Warning, "Prescaler lock active: cannot reconfigure!");
                                return;
                            }
                            if(val > 10)
                            {
                                this.Log(LogLevel.Warning, "Prescaler ratio should be in range <0,10>!");
                                return;
                            }
                            timerAndWatchdogPrescaler = (1 << (int)val);
                            periodicInterruptTimer.Divider = timerAndWatchdogPrescaler;
                        },
                        valueProviderCallback: _ =>
                        {
                            if(lockPrescalers.Value)
                            {
                                this.Log(LogLevel.Warning, "Prescaler lock active: returning zero!");
                                return 0;
                            }
                            return (ulong)timerAndWatchdogPrescaler;
                        },
                        name: "MDIV")
                },

                {(long)Registers.Timer0, new WordRegister(this)
                    .WithValueField(0, 16,
                        writeCallback: (__, val) =>
                        {
                            if(lockTimer.Value)
                            {
                                this.Log(LogLevel.Warning, "Timer lock active: cannot reconfigure!");
                                return;
                            }
                            periodicInterruptTimer.Limit = val;
                        },
                        valueProviderCallback: _ =>
                        {
                            if(lockTimer.Value)
                            {
                                this.Log(LogLevel.Warning, "Timer lock active: returning zero!");
                                return 0;
                            }
                            return periodicInterruptTimer.Limit;
                        },
                        name: "T0_PRESET (T0 Counter Preset)")
                },

                {(long)Registers.Timer0ControlAndStatus, new WordRegister(this)
                    .WithReservedBits(8, 8)
                    .WithTaggedFlag("TESDIS (Too Early Service Disable)", 7)
                    .WithReservedBits(6, 1)
                    .WithFlag(5, FieldMode.Read, valueProviderCallback: _ => watchdogCounter.Enabled, name: "WD_RUN (Watchdog Run Status)")
                    .WithFlag(4, out watchdogResetStatus, FieldMode.WriteOneToClear | FieldMode.Read, name: "WDRST_STS (Watchdog Reset Status)")
                    .WithTaggedFlag("WDLTD (Watchdog Last Touch Delay)", 3)
                    .WithReservedBits(2, 1)
                    .WithFlag(1, FieldMode.Read, 
                        valueProviderCallback: _ => 
                        {
                            if(terminalCountReached)
                            {
                                terminalCountReached = false;
                                return true;
                            }
                            return terminalCountReached;
                        },
                        name: "TC (Terminal Count)")
                    .WithFlag(0,
                        valueProviderCallback: _ => false,
                        writeCallback: (_, val) =>
                        {
                            if(!val)
                            {
                                return;
                            }
                            periodicInterruptTimer.Value = PeriodicInterruptTimerMaxValue;
                        },
                        name: "RST (Reset)")
                },

                {(long)Registers.WatchdogCount, new WordRegister(this)
                    .WithReservedBits(8, 8)
                    .WithValueField(0, 8,
                        writeCallback: (__, val) =>
                        {
                            if(!watchdogCounter.Enabled)
                            {
                                watchdog.Value = val;
                                watchdogCounter.Enabled = true;
                            }
                            if(lockWatchdog.Value && !watchdogTouchSelect.Value)
                            {
                                this.Log(LogLevel.Warning, "Watchdog lock active: cannot reconfigure!");
                                return;
                            }
                            watchdogCounterPresetValue = (byte)val;
                        },
                        valueProviderCallback: _ =>
                        {
                            if(lockWatchdog.Value)
                            {
                                this.Log(LogLevel.Warning, "Watchdog lock active: returning zero!");
                                return 0;
                            }
                            return (ulong)watchdog.Value;
                        },
                        name: "WD_PRESET (Watchdog Counter Preset)")
                },

                {(long)Registers.WatchdogServiceDataMatch, new WordRegister(this)
                    .WithReservedBits(8, 8)
                    .WithValueField(0, 8, FieldMode.Write,
                        writeCallback: (__, val) =>
                        {
                            // stop sequence: 87h, 61h, 63h
                            // unlock sequence is the same
                            if(HandleStopUnlockSequence((byte)val) == StopUnlockSequence.ThirdByte)
                            {
                                if(watchdogCounter.Enabled)
                                {
                                    watchdogCounter.Enabled = false;
                                }
                                else
                                {
                                    lockWatchdog.Value = false;
                                    lockTimer.Value = false;
                                    lockPrescalers.Value = false;
                                    lockWatchdogConfig.Value = false;
                                }
                            }
                            else if(watchdogTouchSelect.Value && val == TouchValue)
                            {
                                this.Log(LogLevel.Noisy, "Watchdog has been touched!");
                                watchdog.Value = watchdogCounterPresetValue;
                            }
                            else
                            {
                                WatchdogAlarmHandler();
                            }
                        },
                        name: "RSDATA (Watchdog Restart Data)")
                },

                {(long)Registers.Timer0Counter, new WordRegister(this)
                    .WithValueField(0, 16, FieldMode.Read, valueProviderCallback: _ => periodicInterruptTimer.Value, name: "T0_COUNT (T0 Counter Value)")
                },

                {(long)Registers.WatchdogCounter, new WordRegister(this)
                    .WithReservedBits(8, 8)
                    .WithValueField(0, 8, FieldMode.Read, valueProviderCallback: _ => (ulong)watchdog.Value, name: "WD_COUNT (Watchdog Counter Value)")
                },
                
                {(long)Registers.WatchdogClockPrescaler, new WordRegister(this)
                    .WithReservedBits(4, 12)
                    .WithValueField(0, 4,
                        writeCallback: (__, val) =>
                        {
                            if(lockPrescalers.Value)
                            {
                                this.Log(LogLevel.Warning, "Prescaler lock active: cannot reconfigure!");
                                return;
                            }
                            if(val > 15)
                            {
                                this.Log(LogLevel.Warning, "Prescaler ratio should be in range <0,15>!");
                                return;
                            }
                            watchdogPrescaler = (1 << (int)val);

                            if(isCounterClockSource.Value)
                            {
                                // Watchdog ticked by Counter (watchdogCounter):
                                // in Renode we can take a shortcut and implement two counters as one,
                                // hence the multiplication `timerAndWatchdogPrescaler * watchdogPrescaler`;
                                // We set the divider on the Counter instead of Watchdog to increase performance
                                watchdogCounter.Divider = timerAndWatchdogPrescaler * watchdogPrescaler;
                                watchdog.Divider = 1;
                            }
                            else
                            {
                                // Watchdog ticked by Timer (periodicInterruptTimer):
                                // `watchdog.Divider` is set only to `watchdogPrescaler` because
                                // `timerAndWatchdogPrescaler` is taken into account as the divider
                                // of `periodicInterruptTimer`
                                watchdog.Divider = watchdogPrescaler;
                            }
                        },
                        valueProviderCallback: _ =>
                        {
                            if(lockPrescalers.Value)
                            {
                                this.Log(LogLevel.Warning, "Prescaler lock active: returning zero!");
                                return 0;
                            }
                            return (ulong)watchdogPrescaler;
                        },
                        name: "WDIV")
                },
            };

            return registerMap;
        }

        private void HandleWatchdogTickWith(Action alarm)
        {
            if(alarm != null)
            {
                alarm();
            }
        }

        private void WatchdogAlarmHandler()
        {
            this.Log(LogLevel.Debug, "Watchdog reset triggered!");
            watchdogResetStatus.Value = true;
            machine.RequestReset();
        }

        private StopUnlockSequence HandleStopUnlockSequence(byte data)
        {
            if((byteInSequence == StopUnlockSequence.None) && (data == 0x87))
            {
                byteInSequence = StopUnlockSequence.FirstByte;
            }
            else if ((byteInSequence == StopUnlockSequence.FirstByte) && (data == 0x61))
            {
                byteInSequence = StopUnlockSequence.SecondByte;
            }
            else if((byteInSequence == StopUnlockSequence.SecondByte) && (data == 0x63))
            {
                byteInSequence = StopUnlockSequence.ThirdByte;
            }
            else
            {
                byteInSequence = StopUnlockSequence.None;
            }
            return byteInSequence;
        }

        private readonly IMachine machine;
        private readonly LimitTimer periodicInterruptTimer;
        private readonly LimitTimer watchdogCounter;
        private readonly Watchdog watchdog;

        private IFlagRegisterField lockTimer;
        private IFlagRegisterField watchdogTouchSelect;
        private IFlagRegisterField lockWatchdog;
        private IFlagRegisterField lockPrescalers;
        private IFlagRegisterField lockWatchdogConfig;
        private IFlagRegisterField watchdogResetStatus;
        private IFlagRegisterField isCounterClockSource;
        private StopUnlockSequence byteInSequence;
        private int timerAndWatchdogPrescaler;
        private int watchdogPrescaler;
        private bool terminalCountReached;
        private byte watchdogCounterPresetValue;

        private const int DefaultFrequency = 32768;
        private const int DefaultDivider = 1;
        private const int WatchdogCounterMaxValue = 0xFF;
        private const int PeriodicInterruptTimerMaxValue = 0xFFFF;
        private const int TouchValue = 0x5C;

        private class Watchdog
        {
            public Watchdog(NPCX_TWD parent, ulong initialValue)
            {
                this.parent = parent;
                this.initialValue = initialValue;
                this.dividerRestoreValue = 1;
                Reset();
            }

            public void Reset()
            {
                isEnabled = false;
                RestoreClock();
            }

            public void Tick()
            {
                if(!isEnabled)
                {
                    return;
                }
                if(internalDivider > 0)
                {
                    --internalDivider;
                    return;
                }
                --Value;
                if(Value > 0 && internalDivider == 0)
                {
                    internalDivider = dividerRestoreValue;
                    return;
                }
                RestoreClock();
                HandleAlarm();
            }

            public ulong Value
            {
                get; set;
            }

            public int Divider
            {
                get => dividerRestoreValue;
                set => dividerRestoreValue = value;
            }

            public event Action LimitReached;

            private void HandleAlarm()
            {
                var alarm = LimitReached;
                if(alarm != null)
                {
                    alarm();
                }
            }

            private void RestoreClock()
            {
                Value = initialValue;
                internalDivider = dividerRestoreValue;
            }

            private NPCX_TWD parent;
            private ulong initialValue;
            private int internalDivider;
            private int dividerRestoreValue;
            private bool isEnabled;
        }

        private enum StopUnlockSequence
        {
            None,
            FirstByte,
            SecondByte,
            ThirdByte
        }

        private enum Registers : long
        {
            TimerAndWatchdogConfiguration = 0x00,   // TWCFG
            TimerAndWatchdogClockPrescaler = 0x02,  // TWCP
            Timer0 = 0x04,                          // TWDT0
            Timer0ControlAndStatus = 0x06,          // T0CSR
            WatchdogCount = 0x08,                   // WDCNT
            WatchdogServiceDataMatch = 0x0A,        // WDSDM
            Timer0Counter = 0x0C,                   // TWMT0
            WatchdogCounter = 0x0E,                 // TWMWD
            WatchdogClockPrescaler = 0x10,          // WDCP
        }
    }
}
