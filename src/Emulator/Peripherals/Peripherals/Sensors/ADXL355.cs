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
    // Analog Devices ADXL355 - low-noise 3-axis accelerometer (I2C), Device ID=0xAD
    public class ADXL355 : II2CPeripheral, IProvidesRegisterCollection<ByteRegisterCollection>, ITemperatureSensor
    {
        public ADXL355()
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

        public decimal Temperature { get; set; } = 25.0m;
        public decimal AccelerationX { get; set; }
        public decimal AccelerationY { get; set; }
        public decimal AccelerationZ { get; set; } = 1.0m;
        public ByteRegisterCollection RegistersCollection { get; }


        private void DefineRegisters()
        {
            Registers.DevIdAD.Define(this, 0xAD);
            Registers.DevIdMST.Define(this, 0x1D);
            Registers.PartId.Define(this, 0xED);
            Registers.Status.Define(this, 0x01);
            Registers.XData3.Define(this).WithValueField(0,8,out ax3,FieldMode.Read);
            Registers.XData2.Define(this).WithValueField(0,8,out ax2,FieldMode.Read);
            Registers.XData1.Define(this).WithValueField(0,8,out ax1,FieldMode.Read);
            Registers.YData3.Define(this).WithValueField(0,8,out ay3,FieldMode.Read);
            Registers.YData2.Define(this).WithValueField(0,8,out ay2,FieldMode.Read);
            Registers.YData1.Define(this).WithValueField(0,8,out ay1,FieldMode.Read);
            Registers.ZData3.Define(this).WithValueField(0,8,out az3,FieldMode.Read);
            Registers.ZData2.Define(this).WithValueField(0,8,out az2,FieldMode.Read);
            Registers.ZData1.Define(this).WithValueField(0,8,out az1,FieldMode.Read);
            Registers.TempH.Define(this).WithValueField(0,8,out tempH,FieldMode.Read);
            Registers.TempL.Define(this).WithValueField(0,8,out tempL,FieldMode.Read);
            Registers.PowerCtl.Define(this);
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            // 20-bit, +/-2g => 256000 LSB/g
            var ax=(int)(AccelerationX*256000m); ax3.Value=(byte)((ax>>12)&0xFF); ax2.Value=(byte)((ax>>4)&0xFF); ax1.Value=(byte)((ax&0xF)<<4);
            var ay=(int)(AccelerationY*256000m); ay3.Value=(byte)((ay>>12)&0xFF); ay2.Value=(byte)((ay>>4)&0xFF); ay1.Value=(byte)((ay&0xF)<<4);
            var az=(int)(AccelerationZ*256000m); az3.Value=(byte)((az>>12)&0xFF); az2.Value=(byte)((az>>4)&0xFF); az1.Value=(byte)((az&0xF)<<4);
            var rawT=(ushort)((Temperature+25m)*(-9.05m)+1852);
            tempH.Value=(byte)((rawT>>8)&0x0F); tempL.Value=(byte)(rawT&0xFF);
        }

        private IValueRegisterField ax3,ax2,ax1,ay3,ay2,ay1,az3,az2,az1,tempH,tempL;
        private int registerAddress;

        private enum Registers : int
        {
            DevIdAD=0x00,DevIdMST=0x01,PartId=0x02,
            Status=0x04,
            TempH=0x06,TempL=0x07,
            XData3=0x08,XData2=0x09,XData1=0x0A,
            YData3=0x0B,YData2=0x0C,YData1=0x0D,
            ZData3=0x0E,ZData2=0x0F,ZData1=0x10,
            PowerCtl=0x2D,
        }
    }
}
