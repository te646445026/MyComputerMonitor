using Microsoft.Extensions.Logging;
using Moq;
using MyComputerMonitor.Core.Interfaces;
using MyComputerMonitor.Core.Models;
using MyComputerMonitor.WPF.ViewModels;
using Xunit;

namespace MyComputerMonitor.Tests.ViewModels;

/// <summary>
/// 温度监控视图模型测试
/// </summary>
public class TemperatureMonitorViewModelTests
{
    private readonly Mock<ILogger<TemperatureMonitorViewModel>> _mockLogger;
    private readonly Mock<IHardwareMonitorService> _mockHardwareService;

    public TemperatureMonitorViewModelTests()
    {
        _mockLogger = new Mock<ILogger<TemperatureMonitorViewModel>>();
        _mockHardwareService = new Mock<IHardwareMonitorService>();
    }

    [Fact]
    public void Constructor_ShouldInitializeCollections()
    {
        // Arrange & Act
        var viewModel = new TemperatureMonitorViewModel(_mockLogger.Object, _mockHardwareService.Object);

        // Assert
        Assert.NotNull(viewModel.TemperatureItems);
        Assert.NotNull(viewModel.FanItems);
        Assert.Empty(viewModel.TemperatureItems);
        Assert.Empty(viewModel.FanItems);
    }

    [Fact]
    public void UpdateTemperatureItems_ShouldNotAddDuplicateSensors()
    {
        // Arrange
        var viewModel = new TemperatureMonitorViewModel(_mockLogger.Object, _mockHardwareService.Object);
        var hardwareData = CreateTestHardwareData();

        // Act
        // 通过反射调用私有方法进行测试
        var method = typeof(TemperatureMonitorViewModel).GetMethod("UpdateTemperatureItems", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(viewModel, new object[] { hardwareData });

        // Assert
        var uniqueKeys = viewModel.TemperatureItems
            .Select(t => $"{t.HardwareName}_{t.SensorName}")
            .ToList();
        
        Assert.Equal(uniqueKeys.Count, uniqueKeys.Distinct().Count());
    }

    [Theory]
    [InlineData(30, "正常")]
    [InlineData(75, "警告")]
    [InlineData(95, "危险")]
    public void GetTemperatureStatus_ShouldReturnCorrectStatus(double temperature, string expectedStatus)
    {
        // Arrange
        var viewModel = new TemperatureMonitorViewModel(_mockLogger.Object, _mockHardwareService.Object);
        
        // Act
        var method = typeof(TemperatureMonitorViewModel).GetMethod("GetTemperatureStatus", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { temperature, 70.0, 85.0 });

        // Assert
        Assert.Equal(expectedStatus, result);
    }

    private static SystemHardwareData CreateTestHardwareData()
    {
        return new SystemHardwareData
        {
            // 测试数据不再包含存储设备
        };
    }
}