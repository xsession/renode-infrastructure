//
// Copyright (c) 2010-2025 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using System.Linq;
using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.Sensor;
using Antmicro.Renode.Utilities;


namespace Antmicro.Renode.Peripherals.Sensors
{
    // ST VL53L1X - long-range ToF distance sensor (I2C)
    public class VL53L1X : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public VL53L1X()
        {
            RegistersCollection = new ByteRegisterCollection(this);
            DefineRegisters();
        }

        public void Reset()
        {
            RegistersCollection.Reset();
            registerAddress = 0;
        }

        public void Write(byte[] data)
        {
            if(data.Length == 0) return;
            registerAddress = data[0];
            for(var i = 1; i < data.Length; i++)
            {
                RegistersCollection.Write(registerAddress, data[i]);
                registerAddress++;
            }
        }

        public byte[] Read(int count)
        {
            var result = new byte[count];
            for(var i = 0; i < count; i++)
            {
                result[i] = RegistersCollection.Read(registerAddress);
                registerAddress++;
            }
            return result;
        }

        public void FinishTransmission() { }

        public int DistanceMm { get; set; } = 500;
        public int AmbientLight { get; set; } = 500;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.ModelId.Define(this, 0xEA);
            Registers.RangeStatus.Define(this, 0x01);
            Registers.RangeValueH.Define(this).WithValueField(0,8,out rangeH,FieldMode.Read);
            Registers.RangeValueL.Define(this).WithValueField(0,8,out rangeL,FieldMode.Read);
            Registers.SystemStart.Define(this).WithWriteCallback((_,__) => UpdateReadout());
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            rangeH.Value = (byte)((DistanceMm >> 8) & 0xFF);
            rangeL.Value = (byte)(DistanceMm & 0xFF);
        }

        private IValueRegisterField rangeH, rangeL;
        private int registerAddress;

        private enum Registers : int
        {
            ModelId=0x00, RangeStatus=0x09,
            RangeValueH=0x1E, RangeValueL=0x1F,
            SystemStart=0x80,
        }
    }
}
