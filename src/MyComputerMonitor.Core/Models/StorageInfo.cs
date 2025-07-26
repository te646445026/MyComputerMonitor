using MyComputerMonitor.Core.Models;

namespace MyComputerMonitor.Core.Models;

/// <summary>
/// 存储设备信息（仅温度监控）
/// </summary>
public class StorageInfo : HardwareInfo
{
    /// <summary>
    /// 存储设备温度 (°C)
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// 是否有温度传感器
    /// </summary>
    public bool HasTemperatureSensor { get; set; }
}