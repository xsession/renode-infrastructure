//
// Copyright (c) 2010-2025 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.GPIOPort;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    // Allegro A4988 - stepper motor driver (GPIO interface stub)
    public class A4988 : IGPIOReceiver, IPeripheral
    {
        public A4988()
        {
            Reset();
        }

        public void Reset()
        {
            StepCount = 0;
            Direction = false;
        }

        public void OnGPIO(int number, bool value)
        {
            switch(number)
            {
                case 0: // STEP - rising edge
                    if(value)
                    {
                        StepCount += Direction ? -1 : 1;
                        this.Log(LogLevel.Debug, "Step {0}, total: {1}", Direction ? "CCW" : "CW", StepCount);
                    }
                    break;
                case 1: // DIR
                    Direction = value;
                    break;
                case 2: // ENABLE (active low)
                    Enabled = !value;
                    break;
            }
        }

        public int StepCount { get; set; }
        public bool Direction { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
