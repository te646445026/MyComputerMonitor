namespace MyComputerMonitor.Core.Models;

/// <summary>
/// 传感器类型枚举
/// </summary>
public enum SensorType
{
    /// <summary>
    /// 温度传感器 (°C)
    /// </summary>
    Temperature,
    
    /// <summary>
    /// 使用率传感器 (%)
    /// </summary>
    Usage,
    
    /// <summary>
    /// 频率传感器 (MHz)
    /// </summary>
    Frequency,
    
    /// <summary>
    /// 电压传感器 (V)
    /// </summary>
    Voltage,
    
    /// <summary>
    /// 电流传感器 (A)
    /// </summary>
    Current,
    
    /// <summary>
    /// 功耗传感器 (W)
    /// </summary>
    Power,
    
    /// <summary>
    /// 风扇转速传感器 (RPM)
    /// </summary>
    Fan,
    
    /// <summary>
    /// 流量传感器
    /// </summary>
    Flow,
    
    /// <summary>
    /// 控制传感器
    /// </summary>
    Control,
    
    /// <summary>
    /// 级别传感器
    /// </summary>
    Level,
    
    /// <summary>
    /// 因子传感器
    /// </summary>
    Factor,
    
    /// <summary>
    /// 数据传感器
    /// </summary>
    Data,
    
    /// <summary>
    /// 小数据传感器
    /// </summary>
    SmallData,
    
    /// <summary>
    /// 吞吐量传感器
    /// </summary>
    Throughput
}