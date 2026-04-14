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
    // TI DRV8825 - stepper motor driver (GPIO interface stub)
    // Pins: STEP (GPIO input), DIR (GPIO input), ENABLE, FAULT (output)
    public class DRV8825 : IGPIOReceiver, IPeripheral
    {
        public DRV8825()
        {
            Fault = new GPIO();
            Reset();
        }

        public void Reset()
        {
            StepCount = 0;
            Direction = false;
            Fault.Set(true); // active-low, no fault
        }

        public void OnGPIO(int number, bool value)
        {
            switch(number)
            {
                case 0: // STEP pin - rising edge
                    if(value)
                    {
                        StepCount += Direction ? -1 : 1;
                        this.Log(LogLevel.Debug, "Step {0}, total: {1}", Direction ? "CCW" : "CW", StepCount);
                    }
                    break;
                case 1: // DIR pin
                    Direction = value;
                    break;
                case 2: // ENABLE pin (active low)
                    Enabled = !value;
                    break;
            }
        }

        public int StepCount { get; set; }
        public bool Direction { get; set; }
        public bool Enabled { get; set; } = true;
        public GPIO Fault { get; }
    }
}
