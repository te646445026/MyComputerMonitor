namespace MyComputerMonitor.Core.Models;

/// <summary>
/// 内存信息模型
/// </summary>
public class MemoryInfo : HardwareInfo
{
    public MemoryInfo()
    {
        Type = HardwareType.Memory;
    }
    
    /// <summary>
    /// 内存使用率 (%)
    /// </summary>
    public double UsagePercentage => GetSensor(SensorType.Usage)?.Value ?? 0.0;
    
    /// <summary>
    /// 已使用内存 (MB)
    /// </summary>
    public long UsedMemory { get; set; }
    
    /// <summary>
    /// 可用内存 (MB)
    /// </summary>
    public long AvailableMemory { get; set; }
    
    /// <summary>
    /// 总内存 (MB)
    /// </summary>
    public long TotalMemory { get; set; }
    
    /// <summary>
    /// 缓存内存 (MB)
    /// </summary>
    public long CachedMemory { get; set; }
    
    /// <summary>
    /// 缓冲内存 (MB)
    /// </summary>
    public long BufferedMemory { get; set; }
    
    /// <summary>
    /// 交换文件使用量 (MB)
    /// </summary>
    public long SwapUsed { get; set; }
    
    /// <summary>
    /// 交换文件总量 (MB)
    /// </summary>
    public long SwapTotal { get; set; }
    
    /// <summary>
    /// 交换文件使用率 (%)
    /// </summary>
    public double SwapUsagePercentage => SwapTotal > 0 ? (double)SwapUsed / SwapTotal * 100 : 0.0;
    
    /// <summary>
    /// 内存频率 (MHz)
    /// </summary>
    public double Frequency { get; set; }
    
    /// <summary>
    /// 内存类型 (DDR4, DDR5等)
    /// </summary>
    public string MemoryType { get; set; } = string.Empty;
    
    /// <summary>
    /// 内存插槽数量
    /// </summary>
    public int SlotCount { get; set; }
    
    /// <summary>
    /// 已使用插槽数量
    /// </summary>
    public int UsedSlots { get; set; }
    
    /// <summary>
    /// 内存条信息列表
    /// </summary>
    public List<MemoryModuleInfo> Modules { get; set; } = [];
    
    /// <summary>
    /// 获取内存使用情况描述
    /// </summary>
    /// <returns>内存使用描述</returns>
    public string GetMemoryUsageDescription()
    {
        return $"{UsedMemory:N0} MB / {TotalMemory:N0} MB ({UsagePercentage:F1}%)";
    }
    
    /// <summary>
    /// 获取可用内存百分比
    /// </summary>
    /// <returns>可用内存百分比</returns>
    public double GetAvailablePercentage()
    {
        return TotalMemory > 0 ? (double)AvailableMemory / TotalMemory * 100 : 0.0;
    }
}

/// <summary>
/// 内存条信息
/// </summary>
public class MemoryModuleInfo
{
    /// <summary>
    /// 内存条名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 容量 (MB)
    /// </summary>
    public long Capacity { get; set; }
    
    /// <summary>
    /// 频率 (MHz)
    /// </summary>
    public double Frequency { get; set; }
    
    /// <summary>
    /// 制造商
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;
    
    /// <summary>
    /// 型号
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// 插槽位置
    /// </summary>
    public string SlotLocation { get; set; } = string.Empty;
    
    /// <summary>
    /// 内存类型
    /// </summary>
    public string MemoryType { get; set; } = string.Empty;
}