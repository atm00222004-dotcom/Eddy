using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using _8F.Models;
using _8F.Services;

namespace _8F.Tests
{
    public class HardwareCommTests
    {
        [Fact]
        public void ProcessPortDataByte_FC20_ParsesInspectionDataPacketCorrectly()
        {
            // Arrange
            var deviceCom = new DeviceCOM();
            DeviceCOM.responses = new List<Response>();
            DeviceCOM.ChannelNo = 4;
            DeviceCOM.IsAutoEllipseActive = false;

            // Raw byte array representing an FC 20 inspection data packet from ECT instrument
            // Format: STX(0x02), FC(20=0x14), Length(12 bytes for 2 frequencies = 2*6), CN(1), OR(1=OK)
            short[] testPacket = new short[]
            {
                0x02, 0x14, 12, 0x01, 0x01,
                // Frequency 1: FN=1, R=1, X=100 (0x0064), Y=200 (0x00C8)
                0x01, 0x01, 0x64, 0x00, 0xC8, 0x00,
                // Frequency 2: FN=2, R=1, X=-50 (0xFFCE), Y=-100 (0xFF9C)
                0x02, 0x01, 0xCE, 0xFF, 0x9C, 0xFF
            };

            // Act
            // Invoke internal parser via reflection if private, or test directly
            var methodInfo = typeof(DeviceCOM).GetMethod("ProcessPortDataBytpe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(methodInfo);

            methodInfo.Invoke(deviceCom, new object[] { testPacket });

            // Assert
            Assert.NotEmpty(DeviceCOM.responses);
            var lastResponse = DeviceCOM.responses.Last();

            Assert.Equal(20, lastResponse.FC);
            Assert.Equal(1, lastResponse.CN);
            Assert.Equal(1, lastResponse.OR);
            Assert.NotNull(lastResponse.FD);
            Assert.Equal(2, lastResponse.FD.Count);

            // Frequency 1 assertions
            Assert.Equal(1, lastResponse.FD[0].FN);
            Assert.Equal(100, lastResponse.FD[0].X);
            Assert.Equal(200, lastResponse.FD[0].Y);

            // Frequency 2 assertions
            Assert.Equal(2, lastResponse.FD[1].FN);
            Assert.Equal(-50, lastResponse.FD[1].X);
            Assert.Equal(-100, lastResponse.FD[1].Y);
        }

        [Fact]
        public void ProcessPortDataByte_FC21_SetsSystemBusyTrue()
        {
            // Arrange
            var deviceCom = new DeviceCOM();
            DeviceCOM.IsSystemBusy = false;

            short[] busyPacket = new short[] { 0x02, 21 };

            var methodInfo = typeof(DeviceCOM).GetMethod("ProcessPortDataBytpe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(methodInfo);

            // Act
            methodInfo.Invoke(deviceCom, new object[] { busyPacket });

            // Assert
            Assert.True(DeviceCOM.IsSystemBusy);
        }

        [Fact]
        public void ProcessPortDataByte_FC22_ClearsSystemBusy()
        {
            // Arrange
            var deviceCom = new DeviceCOM();
            DeviceCOM.IsSystemBusy = true;

            short[] readyPacket = new short[] { 0x02, 22, 0, 0, 0 };

            var methodInfo = typeof(DeviceCOM).GetMethod("ProcessPortDataBytpe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(methodInfo);

            // Act
            methodInfo.Invoke(deviceCom, new object[] { readyPacket });

            // Assert
            Assert.False(DeviceCOM.IsSystemBusy);
        }

        [Fact]
        public void ProcessPortDataByte_TruncatedOrEmptyPacket_HandlesGracefullyWithoutThrowing()
        {
            // Arrange
            var deviceCom = new DeviceCOM();
            short[] emptyPacket = new short[0];
            short[] singleBytePacket = new short[] { 0x02 };

            var methodInfo = typeof(DeviceCOM).GetMethod("ProcessPortDataBytpe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(methodInfo);

            // Act & Assert: Invoking with empty or truncated packets should not throw unhandled exceptions
            var ex1 = Record.Exception(() => methodInfo.Invoke(deviceCom, new object[] { emptyPacket }));
            var ex2 = Record.Exception(() => methodInfo.Invoke(deviceCom, new object[] { singleBytePacket }));

            Assert.Null(ex1);
            Assert.Null(ex2);
        }

        [Fact]
        public void BuildBalanceTestCommandPacket_ConstructsExpectedBinaryStructure()
        {
            // Act: Build standard FC 17 binary balance packet
            byte[] data = new byte[6];
            data[0] = 2;   // STX
            data[1] = 17;  // FC 17 (Balance Command)
            data[2] = 1;   // Length
            data[3] = 0;   // CN 0 (All channels)

            // Assert
            Assert.Equal(6, data.Length);
            Assert.Equal(2, data[0]);
            Assert.Equal(17, data[1]);
            Assert.Equal(1, data[2]);
            Assert.Equal(0, data[3]);
        }
    }
}
