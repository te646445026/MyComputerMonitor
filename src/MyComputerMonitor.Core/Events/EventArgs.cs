using MyComputerMonitor.Core.Models;

namespace MyComputerMonitor.Core.Events;

/// <summary>
/// 硬件数据更新事件参数
/// </summary>
public class HardwareDataUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// 更新的硬件数据
    /// </summary>
    public SystemHardwareData HardwareData { get; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="hardwareData">硬件数据</param>
    public HardwareDataUpdatedEventArgs(SystemHardwareData hardwareData)
    {
        HardwareData = hardwareData;
        UpdateTime = DateTime.Now;
    }
}

/// <summary>
/// 硬件状态变化事件参数
/// </summary>
public class HardwareStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// 硬件信息
    /// </summary>
    public HardwareInfo Hardware { get; }
    
    /// <summary>
    /// 状态变化类型
    /// </summary>
    public HardwareStatusChangeType ChangeType { get; }
    
    /// <summary>
    /// 变化描述
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// 变化时间
    /// </summary>
    public DateTime ChangeTime { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="hardware">硬件信息</param>
    /// <param name="changeType">变化类型</param>
    /// <param name="description">变化描述</param>
    public HardwareStatusChangedEventArgs(HardwareInfo hardware, HardwareStatusChangeType changeType, string description)
    {
        Hardware = hardware;
        ChangeType = changeType;
        Description = description;
        ChangeTime = DateTime.Now;
    }
}

/// <summary>
/// 硬件状态变化类型
/// </summary>
public enum HardwareStatusChangeType
{
    /// <summary>
    /// 硬件连接
    /// </summary>
    Connected,
    
    /// <summary>
    /// 硬件断开
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// 温度警告
    /// </summary>
    TemperatureWarning,
    
    /// <summary>
    /// 温度危险
    /// </summary>
    TemperatureCritical,
    
    /// <summary>
    /// 使用率警告
    /// </summary>
    UsageWarning,
    
    /// <summary>
    /// 使用率危险
    /// </summary>
    UsageCritical,
    
    /// <summary>
    /// 硬件错误
    /// </summary>
    Error,
    
    /// <summary>
    /// 硬件恢复正常
    /// </summary>
    Normal
}

/// <summary>
/// 配置变更事件参数
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// 变更的配置键
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// 旧值
    /// </summary>
    public object? OldValue { get; }
    
    /// <summary>
    /// 新值
    /// </summary>
    public object? NewValue { get; }
    
    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime ChangeTime { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    public ConfigurationChangedEventArgs(string key, object? oldValue, object? newValue)
    {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
        ChangeTime = DateTime.Now;
    }
}

/// <summary>
/// 系统托盘事件参数
/// </summary>
public class SystemTrayEventArgs : EventArgs
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public SystemTrayEventType EventType { get; }
    
    /// <summary>
    /// 事件数据
    /// </summary>
    public object? Data { get; }
    
    /// <summary>
    /// 事件时间
    /// </summary>
    public DateTime EventTime { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="data">事件数据</param>
    public SystemTrayEventArgs(SystemTrayEventType eventType, object? data = null)
    {
        EventType = eventType;
        Data = data;
        EventTime = DateTime.Now;
    }
}

/// <summary>
/// 系统托盘事件类型
/// </summary>
public enum SystemTrayEventType
{
    /// <summary>
    /// 单击
    /// </summary>
    Click,
    
    /// <summary>
    /// 双击
    /// </summary>
    DoubleClick,
    
    /// <summary>
    /// 右键单击
    /// </summary>
    RightClick,
    
    /// <summary>
    /// 显示主窗口
    /// </summary>
    ShowMainWindow,
    
    /// <summary>
    /// 隐藏主窗口
    /// </summary>
    HideMainWindow,
    
    /// <summary>
    /// 退出应用程序
    /// </summary>
    ExitApplication,
    
    /// <summary>
    /// 显示设置
    /// </summary>
    ShowSettings,
    
    /// <summary>
    /// 显示关于
    /// </summary>
    ShowAbout
}