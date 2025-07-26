using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Interfaces;
using MyComputerMonitor.Core.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MyComputerMonitor.WPF.ViewModels
{
    /// <summary>
    /// 温度监控视图模型
    /// </summary>
    public partial class TemperatureMonitorViewModel : ObservableObject
{
    private readonly ILogger<TemperatureMonitorViewModel> _logger;
    private readonly IHardwareMonitorService _hardwareMonitorService;
    private readonly Timer _updateTimer;

    [ObservableProperty]
    private string _title = "硬件温度监控";

    [ObservableProperty]
    private DateTime _lastUpdated = DateTime.Now;

    [ObservableProperty]
    private bool _isMonitoring = false;

    /// <summary>
    /// 温度信息集合
    /// </summary>
    public ObservableCollection<TemperatureInfo> TemperatureItems { get; }

    /// <summary>
    /// 风扇信息集合
    /// </summary>
    public ObservableCollection<FanInfo> FanItems { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public TemperatureMonitorViewModel(
        ILogger<TemperatureMonitorViewModel> logger,
        IHardwareMonitorService hardwareMonitorService)
    {
        _logger = logger;
        _hardwareMonitorService = hardwareMonitorService;
        
        TemperatureItems = new ObservableCollection<TemperatureInfo>();
        FanItems = new ObservableCollection<FanInfo>();

        // 创建更新定时器
        _updateTimer = new Timer(UpdateTemperatureData, null, Timeout.Infinite, Timeout.Infinite);

        // 初始化命令
        StartMonitoringCommand = new RelayCommand(StartMonitoring);
        StopMonitoringCommand = new RelayCommand(StopMonitoring);
        RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());

        // 启动监控
        _ = InitializeAsync();
    }

    /// <summary>
    /// 开始监控命令
    /// </summary>
    public ICommand StartMonitoringCommand { get; }

    /// <summary>
    /// 停止监控命令
    /// </summary>
    public ICommand StopMonitoringCommand { get; }

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 初始化
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            await _hardwareMonitorService.InitializeAsync();
            StartMonitoring();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化温度监控时发生错误");
        }
    }

    /// <summary>
    /// 开始监控
    /// </summary>
    private void StartMonitoring()
    {
        if (IsMonitoring) return;

        try
        {
            IsMonitoring = true;
            _updateTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(2)); // 每2秒更新一次
            _logger.LogInformation("温度监控已启动");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动温度监控时发生错误");
            IsMonitoring = false;
        }
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    private void StopMonitoring()
    {
        if (!IsMonitoring) return;

        try
        {
            IsMonitoring = false;
            _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("温度监控已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止温度监控时发生错误");
        }
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    private async Task RefreshDataAsync()
    {
        try
        {
            var hardwareData = await _hardwareMonitorService.GetHardwareDataAsync();
            
            // 更新UI
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateTemperatureItems(hardwareData);
                UpdateFanItems(hardwareData);
                LastUpdated = DateTime.Now;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新温度数据时发生错误");
        }
    }

    /// <summary>
    /// 定时器更新回调
    /// </summary>
    private async void UpdateTemperatureData(object? state)
    {
        if (!IsMonitoring) return;
        await RefreshDataAsync();
    }

    /// <summary>
    /// 更新温度信息
    /// </summary>
    private void UpdateTemperatureItems(SystemHardwareData hardwareData)
    {
        TemperatureItems.Clear();
        var addedSensors = new HashSet<string>(); // 用于去重

        // CPU 温度
        foreach (var cpu in hardwareData.Cpus ?? [])
        {
            var tempSensor = cpu.GetSensor(SensorType.Temperature);
            if (tempSensor != null)
            {
                var sensorKey = $"{cpu.Name}_{tempSensor.Name}";
                if (!addedSensors.Contains(sensorKey))
                {
                    TemperatureItems.Add(new TemperatureInfo
                    {
                        HardwareName = cpu.Name ?? "CPU",
                        SensorName = tempSensor.Name,
                        Temperature = tempSensor.Value,
                        Unit = tempSensor.Unit,
                        Status = GetTemperatureStatus(tempSensor.Value, 70, 85),
                        HardwareType = "CPU"
                    });
                    addedSensors.Add(sensorKey);
                }
            }
        }

        // GPU 温度
        foreach (var gpu in hardwareData.Gpus ?? [])
        {
            var tempSensor = gpu.GetSensor(SensorType.Temperature);
            if (tempSensor != null)
            {
                var sensorKey = $"{gpu.Name}_{tempSensor.Name}";
                if (!addedSensors.Contains(sensorKey))
                {
                    TemperatureItems.Add(new TemperatureInfo
                    {
                        HardwareName = gpu.Name ?? "GPU",
                        SensorName = tempSensor.Name,
                        Temperature = tempSensor.Value,
                        Unit = tempSensor.Unit,
                        Status = GetTemperatureStatus(tempSensor.Value, 75, 90),
                        HardwareType = "GPU"
                    });
                    addedSensors.Add(sensorKey);
                }
            }
        }

        // 主板温度
        if (hardwareData.Motherboard != null)
        {
            foreach (var sensor in hardwareData.Motherboard.Sensors.Where(s => s.Type == SensorType.Temperature))
            {
                var sensorKey = $"{hardwareData.Motherboard.Name}_{sensor.Name}";
                if (!addedSensors.Contains(sensorKey))
                {
                    TemperatureItems.Add(new TemperatureInfo
                    {
                        HardwareName = hardwareData.Motherboard.Name ?? "主板",
                        SensorName = sensor.Name,
                        Temperature = sensor.Value,
                        Unit = sensor.Unit,
                        Status = GetTemperatureStatus(sensor.Value, 60, 75),
                        HardwareType = "主板"
                    });
                    addedSensors.Add(sensorKey);
                }
            }
        }

        // 存储设备温度
        foreach (var storage in hardwareData.StorageDevices ?? [])
        {
            if (storage.HasTemperatureSensor && storage.Temperature > 0)
            {
                var sensorKey = $"{storage.Name}_Temperature";
                if (!addedSensors.Contains(sensorKey))
                {
                    TemperatureItems.Add(new TemperatureInfo
                    {
                        HardwareName = storage.Name ?? "存储设备",
                        SensorName = "温度",
                        Temperature = storage.Temperature,
                        Unit = "°C",
                        Status = GetTemperatureStatus(storage.Temperature, 50, 60),
                        HardwareType = "存储"
                    });
                    addedSensors.Add(sensorKey);
                }
            }
        }


    }

    /// <summary>
    /// 更新风扇信息
    /// </summary>
    private void UpdateFanItems(SystemHardwareData hardwareData)
    {
        FanItems.Clear();

        // 收集所有硬件的风扇信息
        var allHardware = new List<HardwareInfo>();
        allHardware.AddRange(hardwareData.Cpus ?? []);
        allHardware.AddRange(hardwareData.Gpus ?? []);
        if (hardwareData.Motherboard != null)
            allHardware.Add(hardwareData.Motherboard);

        foreach (var hardware in allHardware)
        {
            foreach (var sensor in hardware.Sensors.Where(s => s.Type == SensorType.Fan))
            {
                FanItems.Add(new FanInfo
                {
                    HardwareName = hardware.Name ?? "未知设备",
                    FanName = sensor.Name,
                    Speed = sensor.Value,
                    Unit = sensor.Unit,
                    Status = GetFanStatus(sensor.Value),
                    HardwareType = GetHardwareTypeString(hardware.Type)
                });
            }
        }
    }

    /// <summary>
    /// 获取温度状态
    /// </summary>
    private static string GetTemperatureStatus(double temperature, double warningThreshold, double criticalThreshold)
    {
        if (temperature >= criticalThreshold)
            return "危险";
        if (temperature >= warningThreshold)
            return "警告";
        return "正常";
    }

    /// <summary>
    /// 获取风扇状态
    /// </summary>
    private static string GetFanStatus(double speed)
    {
        return speed switch
        {
            0 => "停转",
            < 500 => "低速",
            < 1500 => "正常",
            _ => "高速"
        };
    }

    /// <summary>
    /// 获取硬件类型字符串
    /// </summary>
    private static string GetHardwareTypeString(HardwareType type)
    {
        return type switch
        {
            HardwareType.CPU => "CPU",
            HardwareType.GPU => "GPU",
            HardwareType.Memory => "内存",
            HardwareType.Motherboard => "主板",
            HardwareType.Storage => "存储",
            HardwareType.Network => "网络",
            _ => "其他"
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        StopMonitoring();
        _updateTimer?.Dispose();
    }
}

/// <summary>
/// 温度信息
/// </summary>
public class TemperatureInfo
{
    public string HardwareName { get; set; } = string.Empty;
    public string SensorName { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string Unit { get; set; } = "°C";
    public string Status { get; set; } = "正常";
    public string HardwareType { get; set; } = string.Empty;
}

/// <summary>
/// 风扇信息
/// </summary>
public class FanInfo
{
    public string HardwareName { get; set; } = string.Empty;
    public string FanName { get; set; } = string.Empty;
    public double Speed { get; set; }
    public string Unit { get; set; } = "RPM";
    public string Status { get; set; } = "正常";
    public string HardwareType { get; set; } = string.Empty;
}
}