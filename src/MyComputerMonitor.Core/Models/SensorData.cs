namespace MyComputerMonitor.Core.Models;

/// <summary>
/// 传感器数据模型
/// </summary>
public class SensorData
{
    /// <summary>
    /// 传感器名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 传感器类型
    /// </summary>
    public SensorType Type { get; set; }
    
    /// <summary>
    /// 当前值
    /// </summary>
    public double Value { get; set; }
    
    /// <summary>
    /// 最小值
    /// </summary>
    public double? MinValue { get; set; }
    
    /// <summary>
    /// 最大值
    /// </summary>
    public double? MaxValue { get; set; }
    
    /// <summary>
    /// 单位
    /// </summary>
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; } = true;
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 传感器标识符
    /// </summary>
    public string Identifier { get; set; } = string.Empty;
}