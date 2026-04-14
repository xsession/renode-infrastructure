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
    // Toshiba TB6612FNG - dual DC motor driver (GPIO stub)
    // AIN1/AIN2/BIN1/BIN2 control direction, STBY enables
    public class TB6612FNG : IGPIOReceiver, IPeripheral
    {
        public TB6612FNG()
        {
            Reset();
        }

        public void Reset()
        {
            motorAForward = false;
            motorAReverse = false;
            motorBForward = false;
            motorBReverse = false;
        }

        public void OnGPIO(int number, bool value)
        {
            switch(number)
            {
                case 0: motorAForward = value; break; // AIN1
                case 1: motorAReverse = value; break; // AIN2
                case 2: motorBForward = value; break; // BIN1
                case 3: motorBReverse = value; break; // BIN2
                case 4: Standby = !value; break;      // STBY (active low = standby)
            }
            this.Log(LogLevel.Debug, "MotorA: {0}, MotorB: {1}, Standby: {2}",
                motorAForward && !motorAReverse ? "FWD" : (!motorAForward && motorAReverse ? "REV" : "STOP"),
                motorBForward && !motorBReverse ? "FWD" : (!motorBForward && motorBReverse ? "REV" : "STOP"),
                Standby);
        }

        public bool Standby { get; set; }
        private bool motorAForward, motorAReverse, motorBForward, motorBReverse;
    }
}
