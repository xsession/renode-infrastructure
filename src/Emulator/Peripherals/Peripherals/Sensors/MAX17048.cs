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
    // Maxim MAX17048 - Li+ fuel gauge (I2C)
    public class MAX17048 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>
    {
        public MAX17048()
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

        public decimal CellVoltage { get; set; } = 3.7m;
        public decimal StateOfCharge { get; set; } = 75.0m;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.VCellMSB.Define(this).WithValueField(0,8,out vcH,FieldMode.Read);
            Registers.VCellLSB.Define(this).WithValueField(0,8,out vcL,FieldMode.Read);
            Registers.SOCMSB.Define(this).WithValueField(0,8,out socH,FieldMode.Read);
            Registers.SOCLSB.Define(this).WithValueField(0,8,out socL,FieldMode.Read);
            Registers.VersionMSB.Define(this, 0x00);
            Registers.VersionLSB.Define(this, 0x11);
            Registers.ConfigMSB.Define(this, 0x97);
            Registers.ConfigLSB.Define(this, 0x1C);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // VCELL: 78.125 uV/LSB, 16-bit
            var rawV = (ushort)(CellVoltage / 0.000078125m);
            vcH.Value=(byte)(rawV>>8); vcL.Value=(byte)(rawV&0xFF);
            // SOC: 1%/256 LSB
            var rawSOC = (ushort)(StateOfCharge * 256m);
            socH.Value=(byte)(rawSOC>>8); socL.Value=(byte)(rawSOC&0xFF);
        }

        private IValueRegisterField vcH,vcL,socH,socL;
        private int registerAddress;

        private enum Registers : int
        {
            VCellMSB=0x02,VCellLSB=0x03,
            SOCMSB=0x04,SOCLSB=0x05,
            VersionMSB=0x08,VersionLSB=0x09,
            ConfigMSB=0x0C,ConfigLSB=0x0D,
        }
    }
}
