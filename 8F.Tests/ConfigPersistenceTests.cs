using System;
using System.Collections.Generic;
using Xunit;
using _8F;
using _8F.Services;

namespace _8F.Tests
{
    public class ConfigPersistenceTests
    {
        [Fact]
        public void ConfigProfileSummary_PropertiesCanBeAssigned()
        {
            var summary = new ConfigProfileSummary
            {
                Id = 1,
                Name = "Test Profile",
                OperatorName = "Operator 1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Assert.Equal(1, summary.Id);
            Assert.Equal("Test Profile", summary.Name);
            Assert.Equal("Operator 1", summary.OperatorName);
        }

        [Fact]
        public void ChannelData_StructureIsLosslessForPersistence()
        {
            var channel = new ChannelData
            {
                Id = 1,
                IsSeleted = true,
                TxStrength = 80,
                graphDatas = new List<GraphData>
                {
                    new GraphData
                    {
                        Id = 1,
                        Name = "D1",
                        freq = 400,
                        gain = 20,
                        phase = 45,
                        isEnable = true,
                        strength = 90,
                        postGain = 60,
                        height = 120,
                        width = 120,
                        ex = 10,
                        ey = 10,
                        angel = 30,
                        height_O = 650,
                        width_O = 650,
                        ex_O = 0,
                        ey_O = 0,
                        angel_O = 30,
                        ellipses = new List<Ellips>
                        {
                            new Ellips { Id = 1, height = 120, width = 120, ex = 5, ey = 5, angel = 15 }
                        }
                    }
                }
            };

            Assert.Equal(1, channel.Id);
            Assert.True(channel.IsSeleted);
            Assert.Single(channel.graphDatas);
            Assert.Equal("D1", channel.graphDatas[0].Name);
            Assert.Single(channel.graphDatas[0].ellipses);
            Assert.Equal(120, channel.graphDatas[0].ellipses[0].height);
        }



        [Fact]
        public void ImportFromJson_ValidJsonContent_ParsesCorrectly()
        {
            string jsonContent = @"[
  {
    ""ChannelNumber"": 1,
    ""IsSelected"": true,
    ""TxStrength"": 100,
    ""Frequencies"": [
      {
        ""FrequencyNumber"": 1,
        ""FrequencyName"": ""D1"",
        ""FrequencyHz"": 400,
        ""Gain"": 20,
        ""Phase"": 45,
        ""IsEnabled"": true,
        ""Strength"": 100,
        ""PostGain"": 60,
        ""DefaultCenterX"": 15.0,
        ""DefaultCenterY"": 25.0,
        ""DefaultWidth"": 130.0,
        ""DefaultHeight"": 130.0,
        ""DefaultRotationAngle"": 40.0
      }
    ]
  }
]";

            var channels = ConfigurationImporter.ImportFromJson(jsonContent);

            Assert.NotNull(channels);
            Assert.Single(channels);
            Assert.Equal(1, channels[0].Id);
            Assert.Single(channels[0].graphDatas);
            Assert.Equal("D1", channels[0].graphDatas[0].Name);
            Assert.Equal(15.0, channels[0].graphDatas[0].ex);
            Assert.Equal(25.0, channels[0].graphDatas[0].ey);
        }

        [Fact]
        public void ApplyRemapping_RemapsSourceChannelToTargetChannel()
        {
            var sourceChannels = new List<ChannelData>
            {
                new ChannelData
                {
                    Id = 1,
                    IsSeleted = true,
                    TxStrength = 100,
                    graphDatas = new List<GraphData>
                    {
                        new GraphData
                        {
                            Id = 1,
                            Name = "D1",
                            freq = 500,
                            gain = 25,
                            phase = 60,
                            ex = 42,
                            ey = 84,
                            width = 150,
                            height = 100,
                            angel = 45
                        }
                    }
                }
            };

            // Map Source Channel 1 to Target Channel 2
            var mappings = new Dictionary<int, List<int>>
            {
                { 1, new List<int> { 2 } }
            };

            var remapped = ConfigurationImporter.ApplyRemapping(sourceChannels, mappings, isImportAsIs: false);

            Assert.NotNull(remapped);
            var ch2 = remapped.FirstOrDefault(c => c.Id == 2);
            Assert.NotNull(ch2);
            Assert.Single(ch2.graphDatas);
            Assert.Equal(500, ch2.graphDatas[0].freq);
            Assert.Equal(25, ch2.graphDatas[0].gain);
            Assert.Equal(42, ch2.graphDatas[0].ex);
            Assert.Equal(84, ch2.graphDatas[0].ey);
            Assert.Equal(150, ch2.graphDatas[0].width);
            Assert.Equal(45, ch2.graphDatas[0].angel);
        }

        [Fact]
        public void FitEllipse_ReferenceDataset_Version2MajorAxisExtension()
        {
            var referencePoints = new List<(double X, double Y)>
            {
                (1, 1), (2, 2), (3, 2), (4, 4), (5, 8), (6, 6), (7, 7), (8, 8), (9, 9), (10, 6)
            };

            var result = EllipseFitter.FitEllipse("D1", 1, referencePoints);

            Assert.True(result.IsValid);
            Assert.True(result.Width > 12.56, $"Width ({result.Width}) should be larger than 12.56 due to major axis extension");
        }

        [Fact]
        public void MenuItemViewModel_ConfigMenuPassword_IsConfiguredCorrectly()
        {
            Assert.Equal("best@123", MenuItemViewModel.CONFIG_MENU_PASSWORD);
        }
    }
}
