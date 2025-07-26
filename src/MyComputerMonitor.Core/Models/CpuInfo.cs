namespace MyComputerMonitor.Core.Models;

/// <summary>
/// CPU信息模型
/// </summary>
public class CpuInfo : HardwareInfo
{
    public CpuInfo()
    {
        Type = HardwareType.CPU;
    }
    
    /// <summary>
    /// CPU使用率 (%)
    /// </summary>
    public double UsagePercentage => GetSensor(SensorType.Usage)?.Value ?? 0.0;
    
    /// <summary>
    /// CPU温度 (°C)
    /// </summary>
    public double Temperature => GetSensor(SensorType.Temperature)?.Value ?? 0.0;
    
    /// <summary>
    /// CPU频率 (MHz)
    /// </summary>
    public double Frequency => GetSensor(SensorType.Frequency)?.Value ?? 0.0;
    
    /// <summary>
    /// CPU功耗 (W)
    /// </summary>
    public double PowerConsumption => GetSensor(SensorType.Power)?.Value ?? 0.0;
    
    /// <summary>
    /// 核心数量
    /// </summary>
    public int CoreCount { get; set; }
    
    /// <summary>
    /// 线程数量
    /// </summary>
    public int ThreadCount { get; set; }
    
    /// <summary>
    /// CPU架构
    /// </summary>
    public string Architecture { get; set; } = string.Empty;
    
    /// <summary>
    /// CPU制造商
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;
    
    /// <summary>
    /// CPU型号
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// 基础频率 (MHz)
    /// </summary>
    public double BaseFrequency { get; set; }
    
    /// <summary>
    /// 最大频率 (MHz)
    /// </summary>
    public double MaxFrequency { get; set; }
    
    /// <summary>
    /// 每个核心的使用率
    /// </summary>
    public List<double> CoreUsages { get; set; } = [];
    
    /// <summary>
    /// 每个核心的温度
    /// </summary>
    public List<double> CoreTemperatures { get; set; } = [];
    
    /// <summary>
    /// 获取平均核心使用率
    /// </summary>
    /// <returns>平均使用率</returns>
    public double GetAverageCoreUsage()
    {
        return CoreUsages.Count > 0 ? CoreUsages.Average() : 0.0;
    }
    
    /// <summary>
    /// 获取最高核心温度
    /// </summary>
    /// <returns>最高温度</returns>
    public double GetMaxCoreTemperature()
    {
        return CoreTemperatures.Count > 0 ? CoreTemperatures.Max() : 0.0;
    }
}