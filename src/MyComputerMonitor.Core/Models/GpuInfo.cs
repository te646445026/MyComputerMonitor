namespace MyComputerMonitor.Core.Models;

/// <summary>
/// GPU信息模型
/// </summary>
public class GpuInfo : HardwareInfo
{
    public GpuInfo()
    {
        Type = HardwareType.GPU;
    }
    
    /// <summary>
    /// GPU使用率 (%)
    /// </summary>
    public double UsagePercentage => GetSensor(SensorType.Usage)?.Value ?? 0.0;
    
    /// <summary>
    /// GPU温度 (°C)
    /// </summary>
    public double Temperature => GetSensor(SensorType.Temperature)?.Value ?? 0.0;
    
    /// <summary>
    /// GPU核心频率 (MHz)
    /// </summary>
    public double CoreClock => GetSensor(SensorType.Frequency)?.Value ?? 0.0;
    
    /// <summary>
    /// GPU功耗 (W)
    /// </summary>
    public double PowerConsumption => GetSensor(SensorType.Power)?.Value ?? 0.0;
    
    /// <summary>
    /// 显存使用量 (MB)
    /// </summary>
    public long MemoryUsed { get; set; }
    
    /// <summary>
    /// 显存总量 (MB)
    /// </summary>
    public long MemoryTotal { get; set; }
    
    /// <summary>
    /// 显存使用率 (%)
    /// </summary>
    public double MemoryUsagePercentage => MemoryTotal > 0 ? (double)MemoryUsed / MemoryTotal * 100 : 0.0;
    
    /// <summary>
    /// 显存频率 (MHz)
    /// </summary>
    public double MemoryClock { get; set; }
    
    /// <summary>
    /// GPU制造商
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;
    
    /// <summary>
    /// GPU型号
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// 驱动版本
    /// </summary>
    public string DriverVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// BIOS版本
    /// </summary>
    public string BiosVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// 基础频率 (MHz)
    /// </summary>
    public double BaseCoreClock { get; set; }
    
    /// <summary>
    /// 最大频率 (MHz)
    /// </summary>
    public double MaxCoreClock { get; set; }
    
    /// <summary>
    /// 基础显存频率 (MHz)
    /// </summary>
    public double BaseMemoryClock { get; set; }
    
    /// <summary>
    /// 最大显存频率 (MHz)
    /// </summary>
    public double MaxMemoryClock { get; set; }
    
    /// <summary>
    /// 风扇转速 (RPM)
    /// </summary>
    public double FanSpeed => GetSensor(SensorType.Fan)?.Value ?? 0.0;
    
    /// <summary>
    /// 获取显存使用情况描述
    /// </summary>
    /// <returns>显存使用描述</returns>
    public string GetMemoryUsageDescription()
    {
        return $"{MemoryUsed:N0} MB / {MemoryTotal:N0} MB ({MemoryUsagePercentage:F1}%)";
    }
}