# API接口文档

## 接口概述

本文档描述了MyComputerMonitor项目中各个模块的接口定义，包括硬件监控、配置管理、系统托盘等核心功能的接口规范。

## 硬件监控接口

### IHardwareMonitorService

硬件监控服务的核心接口，负责获取各种硬件信息。

```csharp
public interface IHardwareMonitorService
{
    /// <summary>
    /// 获取所有硬件信息
    /// </summary>
    /// <returns>硬件信息集合</returns>
    Task<HardwareInfo> GetAllHardwareInfoAsync();
    
    /// <summary>
    /// 获取CPU信息
    /// </summary>
    /// <returns>CPU信息</returns>
    Task<CpuInfo> GetCpuInfoAsync();
    
    /// <summary>
    /// 获取GPU信息
    /// </summary>
    /// <returns>GPU信息集合</returns>
    Task<IEnumerable<GpuInfo>> GetGpuInfoAsync();
    
    /// <summary>
    /// 获取内存信息
    /// </summary>
    /// <returns>内存信息</returns>
    Task<MemoryInfo> GetMemoryInfoAsync();
    
    /// <summary>
    /// 获取主板信息
    /// </summary>
    /// <returns>主板信息</returns>
    Task<MotherboardInfo> GetMotherboardInfoAsync();
    
    /// <summary>
    /// 开始监控
    /// </summary>
    void StartMonitoring();
    
    /// <summary>
    /// 停止监控
    /// </summary>
    void StopMonitoring();
    
    /// <summary>
    /// 硬件信息更新事件
    /// </summary>
    event EventHandler<HardwareInfoUpdatedEventArgs> HardwareInfoUpdated;
}
```

### ISensorReader

传感器读取器接口，用于读取特定类型的硬件传感器数据。

```csharp
public interface ISensorReader
{
    /// <summary>
    /// 传感器类型
    /// </summary>
    SensorType SensorType { get; }
    
    /// <summary>
    /// 是否支持当前硬件
    /// </summary>
    bool IsSupported { get; }
    
    /// <summary>
    /// 读取传感器数据
    /// </summary>
    /// <returns>传感器数据</returns>
    Task<SensorData> ReadAsync();
    
    /// <summary>
    /// 初始化传感器
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// 释放资源
    /// </summary>
    void Dispose();
}
```

## 配置管理接口

### IConfigurationService

配置管理服务接口，负责应用程序配置的读取、保存和管理。

```csharp
public interface IConfigurationService
{
    /// <summary>
    /// 获取应用设置
    /// </summary>
    /// <returns>应用设置</returns>
    AppSettings GetAppSettings();
    
    /// <summary>
    /// 保存应用设置
    /// </summary>
    /// <param name="settings">应用设置</param>
    Task SaveAppSettingsAsync(AppSettings settings);
    
    /// <summary>
    /// 获取用户偏好设置
    /// </summary>
    /// <returns>用户偏好设置</returns>
    UserPreferences GetUserPreferences();
    
    /// <summary>
    /// 保存用户偏好设置
    /// </summary>
    /// <param name="preferences">用户偏好设置</param>
    Task SaveUserPreferencesAsync(UserPreferences preferences);
    
    /// <summary>
    /// 重置为默认设置
    /// </summary>
    Task ResetToDefaultAsync();
    
    /// <summary>
    /// 配置变更事件
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
}
```

### ISettingsValidator

设置验证器接口，用于验证配置的有效性。

```csharp
public interface ISettingsValidator
{
    /// <summary>
    /// 验证应用设置
    /// </summary>
    /// <param name="settings">应用设置</param>
    /// <returns>验证结果</returns>
    ValidationResult ValidateAppSettings(AppSettings settings);
    
    /// <summary>
    /// 验证用户偏好设置
    /// </summary>
    /// <param name="preferences">用户偏好设置</param>
    /// <returns>验证结果</returns>
    ValidationResult ValidateUserPreferences(UserPreferences preferences);
}
```

## 系统托盘接口

### ITrayService

系统托盘服务接口，管理托盘图标和相关功能。

```csharp
public interface ITrayService
{
    /// <summary>
    /// 初始化托盘
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 显示托盘图标
    /// </summary>
    void ShowTrayIcon();
    
    /// <summary>
    /// 隐藏托盘图标
    /// </summary>
    void HideTrayIcon();
    
    /// <summary>
    /// 更新托盘图标
    /// </summary>
    /// <param name="iconPath">图标路径</param>
    void UpdateTrayIcon(string iconPath);
    
    /// <summary>
    /// 更新工具提示
    /// </summary>
    /// <param name="tooltip">工具提示文本</param>
    void UpdateTooltip(string tooltip);
    
    /// <summary>
    /// 显示气球提示
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息</param>
    /// <param name="icon">图标类型</param>
    void ShowBalloonTip(string title, string message, BalloonTipIcon icon);
    
    /// <summary>
    /// 托盘图标点击事件
    /// </summary>
    event EventHandler<TrayIconClickedEventArgs> TrayIconClicked;
    
    /// <summary>
    /// 托盘菜单项点击事件
    /// </summary>
    event EventHandler<TrayMenuItemClickedEventArgs> TrayMenuItemClicked;
}
```

### ITrayPopupService

托盘弹出窗口服务接口。

```csharp
public interface ITrayPopupService
{
    /// <summary>
    /// 显示弹出窗口
    /// </summary>
    /// <param name="position">显示位置</param>
    void ShowPopup(Point position);
    
    /// <summary>
    /// 隐藏弹出窗口
    /// </summary>
    void HidePopup();
    
    /// <summary>
    /// 更新弹出窗口内容
    /// </summary>
    /// <param name="content">内容数据</param>
    void UpdateContent(object content);
    
    /// <summary>
    /// 弹出窗口是否可见
    /// </summary>
    bool IsVisible { get; }
}
```

## 自启动管理接口

### IAutoStartService

自启动管理服务接口。

```csharp
public interface IAutoStartService
{
    /// <summary>
    /// 启用自启动
    /// </summary>
    /// <param name="method">自启动方式</param>
    Task EnableAutoStartAsync(AutoStartMethod method);
    
    /// <summary>
    /// 禁用自启动
    /// </summary>
    Task DisableAutoStartAsync();
    
    /// <summary>
    /// 检查自启动状态
    /// </summary>
    /// <returns>是否已启用自启动</returns>
    Task<bool> IsAutoStartEnabledAsync();
    
    /// <summary>
    /// 获取当前自启动方式
    /// </summary>
    /// <returns>自启动方式</returns>
    Task<AutoStartMethod> GetAutoStartMethodAsync();
    
    /// <summary>
    /// 获取支持的自启动方式
    /// </summary>
    /// <returns>支持的自启动方式列表</returns>
    IEnumerable<AutoStartMethod> GetSupportedMethods();
}
```

## 日志记录接口

### ILogService

日志记录服务接口。

```csharp
public interface ILogService
{
    /// <summary>
    /// 记录调试信息
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogDebug(string message, params object[] args);
    
    /// <summary>
    /// 记录信息
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogInfo(string message, params object[] args);
    
    /// <summary>
    /// 记录警告
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    void LogWarning(string message, params object[] args);
    
    /// <summary>
    /// 记录错误
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="exception">异常</param>
    /// <param name="args">参数</param>
    void LogError(string message, Exception exception = null, params object[] args);
    
    /// <summary>
    /// 记录致命错误
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="exception">异常</param>
    /// <param name="args">参数</param>
    void LogFatal(string message, Exception exception = null, params object[] args);
}
```

## 数据模型

### HardwareInfo

硬件信息基类。

```csharp
public class HardwareInfo
{
    /// <summary>
    /// 硬件名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 硬件类型
    /// </summary>
    public HardwareType Type { get; set; }
    
    /// <summary>
    /// 传感器数据
    /// </summary>
    public List<SensorData> Sensors { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
```

### CpuInfo

CPU信息模型。

```csharp
public class CpuInfo : HardwareInfo
{
    /// <summary>
    /// CPU使用率 (%)
    /// </summary>
    public double UsagePercentage { get; set; }
    
    /// <summary>
    /// CPU温度 (°C)
    /// </summary>
    public double Temperature { get; set; }
    
    /// <summary>
    /// CPU频率 (MHz)
    /// </summary>
    public double Frequency { get; set; }
    
    /// <summary>
    /// 核心数量
    /// </summary>
    public int CoreCount { get; set; }
    
    /// <summary>
    /// 线程数量
    /// </summary>
    public int ThreadCount { get; set; }
    
    /// <summary>
    /// 每个核心的使用率
    /// </summary>
    public List<double> CoreUsages { get; set; }
}
```

### GpuInfo

GPU信息模型。

```csharp
public class GpuInfo : HardwareInfo
{
    /// <summary>
    /// GPU使用率 (%)
    /// </summary>
    public double UsagePercentage { get; set; }
    
    /// <summary>
    /// GPU温度 (°C)
    /// </summary>
    public double Temperature { get; set; }
    
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
    public double MemoryUsagePercentage { get; set; }
    
    /// <summary>
    /// GPU频率 (MHz)
    /// </summary>
    public double CoreClock { get; set; }
    
    /// <summary>
    /// 显存频率 (MHz)
    /// </summary>
    public double MemoryClock { get; set; }
    
    /// <summary>
    /// 功耗 (W)
    /// </summary>
    public double PowerConsumption { get; set; }
}
```

### MemoryInfo

内存信息模型。

```csharp
public class MemoryInfo : HardwareInfo
{
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
    /// 内存使用率 (%)
    /// </summary>
    public double UsagePercentage { get; set; }
    
    /// <summary>
    /// 缓存内存 (MB)
    /// </summary>
    public long CachedMemory { get; set; }
    
    /// <summary>
    /// 缓冲内存 (MB)
    /// </summary>
    public long BufferedMemory { get; set; }
}
```

## 事件参数

### HardwareInfoUpdatedEventArgs

硬件信息更新事件参数。

```csharp
public class HardwareInfoUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// 更新的硬件信息
    /// </summary>
    public HardwareInfo HardwareInfo { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; }
}
```

### ConfigurationChangedEventArgs

配置变更事件参数。

```csharp
public class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// 变更的配置键
    /// </summary>
    public string ConfigurationKey { get; set; }
    
    /// <summary>
    /// 旧值
    /// </summary>
    public object OldValue { get; set; }
    
    /// <summary>
    /// 新值
    /// </summary>
    public object NewValue { get; set; }
}
```

## 枚举定义

### HardwareType

硬件类型枚举。

```csharp
public enum HardwareType
{
    CPU,
    GPU,
    Memory,
    Motherboard,
    Storage,    // 存储设备（仅温度监控）
    Network
}
```

### SensorType

传感器类型枚举。

```csharp
public enum SensorType
{
    Temperature,
    Usage,
    Frequency,
    Voltage,
    Current,
    Power,
    Fan,
    Flow,
    Control,
    Level,
    Factor,
    Data,
    SmallData,
    Throughput
}
```

### AutoStartMethod

自启动方式枚举。

```csharp
public enum AutoStartMethod
{
    Registry,
    TaskScheduler,
    StartupFolder
}
```

---

**接口版本**: v1.0  
**文档更新**: 2024年  
**兼容性**: .NET 9.0+