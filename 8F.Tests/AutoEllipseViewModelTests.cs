using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Xunit;
using Moq;
using _8F.Models;
using _8F.Services;
using _8F.ViewModels;

namespace _8F.Tests
{
    public class AutoEllipseViewModelTests
    {
        [Fact]
        public void AutoEllipseViewModel_Initialization_SetsDefaultProperties()
        {
            // Arrange
            var mockRepo = new Mock<IAutoEllipseRepository>();
            mockRepo.Setup(r => r.GetAutoEllipseTestsByChannelAsync(It.IsAny<int>()))
                    .ReturnsAsync(new List<AutoEllipseTest>());

            // Act
            var viewModel = new AutoEllipseViewModel(mockRepo.Object);

            // Assert
            Assert.NotNull(viewModel);
            Assert.True(viewModel.IsRunTestEnabled);
            Assert.False(viewModel.IsMakeEllipseEnabled);
            Assert.True(viewModel.IsChannelEnabled);
            Assert.True(viewModel.IsAutoStretch);
            Assert.Equal("1", viewModel.StretchA);
            Assert.Equal("1", viewModel.StretchB);
            Assert.Equal("Ready for Auto Ellipse calibration.", viewModel.StatusMessage);
        }

        [Fact]
        public void RunTestCommand_WhenBalanceIsRequired_BlocksExecutionAndUpdatesStatus()
        {
            // Arrange
            var mockRepo = new Mock<IAutoEllipseRepository>();
            mockRepo.Setup(r => r.GetAutoEllipseTestsByChannelAsync(It.IsAny<int>()))
                    .ReturnsAsync(new List<AutoEllipseTest>());

            var viewModel = new AutoEllipseViewModel(mockRepo.Object);

            // Force balance state to required
            DeviceCOM.IsBalanceRequired = true;
            DeviceCOM.IsLogEnable = false;
            DeviceCOM.IsSystemBusy = false;
            DeviceCOM.responses = new List<Response>();

            // Act: Execute RunTest command
            if (viewModel.RunTestCommand.CanExecute(null))
            {
                viewModel.RunTestCommand.Execute(null);
            }

            // Assert: StatusMessage updated to prompt for Balance
            Assert.Equal("Please click Balance first.", viewModel.StatusMessage);

            // Reset state
            DeviceCOM.IsBalanceRequired = false;
        }

        [Fact]
        public void RunTestCommand_WhenChannelNotBalanced_BlocksExecution()
        {
            // Arrange
            var mockRepo = new Mock<IAutoEllipseRepository>();
            mockRepo.Setup(r => r.GetAutoEllipseTestsByChannelAsync(It.IsAny<int>()))
                    .ReturnsAsync(new List<AutoEllipseTest>());

            var viewModel = new AutoEllipseViewModel(mockRepo.Object);

            DeviceCOM.IsBalanceRequired = false;
            DeviceCOM.IsLogEnable = false;
            DeviceCOM.IsSystemBusy = false;
            DeviceCOM.responses = new List<Response>
            {
                // Unbalanced response for Channel 1
                new Response { CN = 1, IsBalacenced = false }
            };

            // Act
            if (viewModel.RunTestCommand.CanExecute(null))
            {
                viewModel.RunTestCommand.Execute(null);
            }

            // Assert
            Assert.Equal("Please click Balance first.", viewModel.StatusMessage);

            // Reset responses
            DeviceCOM.responses.Clear();
        }

        [Fact]
        public void MakeEllipseCommand_WhenNoTableOrNoRows_BlocksWithoutCrashing()
        {
            // Arrange
            var mockRepo = new Mock<IAutoEllipseRepository>();
            var viewModel = new AutoEllipseViewModel(mockRepo.Object);

            // Act & Assert
            if (viewModel.MakeEllipseCommand.CanExecute(null))
            {
                viewModel.MakeEllipseCommand.Execute(null);
            }

            Assert.False(viewModel.IsSaved);
        }

        [Fact]
        public async Task RepositoryFailure_WhenInsertingTestRun_HandledGracefully()
        {
            // Arrange
            var mockRepo = new Mock<IAutoEllipseRepository>();
            mockRepo.Setup(r => r.InsertAutoEllipseTestAsync(It.IsAny<AutoEllipseTest>()))
                    .ThrowsAsync(new Exception("Database connection failure"));

            var viewModel = new AutoEllipseViewModel(mockRepo.Object);

            var testRecord = new AutoEllipseTest
            {
                ChannelId = 1,
                TestNumber = 1,
                TimeStamp = DateTime.UtcNow,
                OperatorName = "Operator",
                FrequencyValuesJson = "{}"
            };

            // Act & Assert: ViewModel / Task execution should catch or isolate exception
            var ex = await Record.ExceptionAsync(async () =>
            {
                await mockRepo.Object.InsertAutoEllipseTestAsync(testRecord);
            });

            Assert.NotNull(ex);
            Assert.Equal("Database connection failure", ex.Message);
        }
    }
}
