namespace MyComputerMonitor.Core.Models;

/// <summary>
/// 温度监控配置
/// </summary>
public class TemperatureMonitorConfig
{
    /// <summary>
    /// 更新间隔（秒）
    /// </summary>
    public int UpdateIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// CPU温度阈值配置
    /// </summary>
    public TemperatureThresholds CpuThresholds { get; set; } = new()
    {
        WarningThreshold = 70,
        CriticalThreshold = 85
    };

    /// <summary>
    /// GPU温度阈值配置
    /// </summary>
    public TemperatureThresholds GpuThresholds { get; set; } = new()
    {
        WarningThreshold = 75,
        CriticalThreshold = 90
    };

    /// <summary>
    /// 存储设备温度阈值配置
    /// </summary>
    public TemperatureThresholds StorageThresholds { get; set; } = new()
    {
        WarningThreshold = 50,
        CriticalThreshold = 65
    };

    /// <summary>
    /// 主板温度阈值配置
    /// </summary>
    public TemperatureThresholds MotherboardThresholds { get; set; } = new()
    {
        WarningThreshold = 60,
        CriticalThreshold = 75
    };

    /// <summary>
    /// 是否显示多传感器设备的详细信息
    /// </summary>
    public bool ShowDetailedMultiSensorInfo { get; set; } = true;

    /// <summary>
    /// 是否在托盘提示中显示温度信息
    /// </summary>
    public bool ShowTemperatureInTrayTooltip { get; set; } = true;
}

/// <summary>
/// 温度阈值配置
/// </summary>
public class TemperatureThresholds
{
    /// <summary>
    /// 警告阈值
    /// </summary>
    public double WarningThreshold { get; set; }

    /// <summary>
    /// 危险阈值
    /// </summary>
    public double CriticalThreshold { get; set; }
}