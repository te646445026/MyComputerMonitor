namespace MyComputerMonitor.Core.Models;

/// <summary>
/// 硬件类型枚举
/// </summary>
public enum HardwareType
{
    /// <summary>
    /// CPU处理器
    /// </summary>
    CPU,
    
    /// <summary>
    /// GPU显卡
    /// </summary>
    GPU,
    
    /// <summary>
    /// 内存
    /// </summary>
    Memory,
    
    /// <summary>
    /// 主板
    /// </summary>
    Motherboard,
    
    /// <summary>
    /// 存储设备（仅温度监控）
    /// </summary>
    Storage,
    
    /// <summary>
    /// 网络设备
    /// </summary>
    Network,
    
    /// <summary>
    /// 风扇
    /// </summary>
    Fan
}