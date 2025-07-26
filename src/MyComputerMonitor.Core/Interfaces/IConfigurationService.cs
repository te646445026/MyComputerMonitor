using MyComputerMonitor.Core.Models;
using MyComputerMonitor.Core.Events;
using System.ComponentModel.DataAnnotations;

namespace MyComputerMonitor.Core.Interfaces;

/// <summary>
/// 配置管理服务接口
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// 配置变更事件
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    
    /// <summary>
    /// 加载配置
    /// </summary>
    /// <returns>加载任务</returns>
    Task LoadConfigurationAsync();
    
    /// <summary>
    /// 保存配置
    /// </summary>
    /// <returns>保存任务</returns>
    Task SaveConfigurationAsync();
    
    /// <summary>
    /// 获取配置值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值</returns>
    T GetValue<T>(string key, T defaultValue = default!);
    
    /// <summary>
    /// 设置配置值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    void SetValue<T>(string key, T value);
    
    /// <summary>
    /// 获取监控配置
    /// </summary>
    /// <returns>监控配置</returns>
    MonitoringConfiguration GetMonitoringConfiguration();
    
    /// <summary>
    /// 设置监控配置
    /// </summary>
    /// <param name="configuration">监控配置</param>
    void SetMonitoringConfiguration(MonitoringConfiguration configuration);
    
    /// <summary>
    /// 获取UI配置
    /// </summary>
    /// <returns>UI配置</returns>
    UiConfiguration GetUiConfiguration();
    
    /// <summary>
    /// 设置UI配置
    /// </summary>
    /// <param name="configuration">UI配置</param>
    void SetUiConfiguration(UiConfiguration configuration);
    
    /// <summary>
    /// 获取通知配置
    /// </summary>
    /// <returns>通知配置</returns>
    NotificationConfiguration GetNotificationConfiguration();
    
    /// <summary>
    /// 设置通知配置
    /// </summary>
    /// <param name="configuration">通知配置</param>
    void SetNotificationConfiguration(NotificationConfiguration configuration);
    
    /// <summary>
    /// 重置为默认配置
    /// </summary>
    /// <returns>重置任务</returns>
    Task ResetToDefaultAsync();
}

/// <summary>
/// 监控配置
/// </summary>
public class MonitoringConfiguration
{
    /// <summary>
    /// 监控间隔（毫秒）
    /// </summary>
    [Range(500, 60000, ErrorMessage = "监控间隔必须在500毫秒到60秒之间")]
    public int MonitoringInterval { get; set; } = 1000;
    
    /// <summary>
    /// 是否启用CPU监控
    /// </summary>
    public bool EnableCpuMonitoring { get; set; } = true;
    
    /// <summary>
    /// 是否启用GPU监控
    /// </summary>
    public bool EnableGpuMonitoring { get; set; } = true;
    
    /// <summary>
    /// 是否启用内存监控
    /// </summary>
    public bool EnableMemoryMonitoring { get; set; } = true;
    
    /// <summary>
    /// 是否启用网络监控
    /// </summary>
    public bool EnableNetworkMonitoring { get; set; } = true;
    
    /// <summary>
    /// 是否启用存储设备监控（仅温度）
    /// </summary>
    public bool EnableStorageMonitoring { get; set; } = true;
    
    /// <summary>
    /// CPU温度警告阈值（摄氏度）
    /// </summary>
    [Range(40, 100, ErrorMessage = "CPU温度警告阈值必须在40-100摄氏度之间")]
    public double CpuTemperatureWarningThreshold { get; set; } = 70;
    
    /// <summary>
    /// CPU温度危险阈值（摄氏度）
    /// </summary>
    [Range(50, 110, ErrorMessage = "CPU温度危险阈值必须在50-110摄氏度之间")]
    public double CpuTemperatureCriticalThreshold { get; set; } = 85;
    
    /// <summary>
    /// GPU温度警告阈值（摄氏度）
    /// </summary>
    [Range(40, 100, ErrorMessage = "GPU温度警告阈值必须在40-100摄氏度之间")]
    public double GpuTemperatureWarningThreshold { get; set; } = 75;
    
    /// <summary>
    /// GPU温度危险阈值（摄氏度）
    /// </summary>
    [Range(50, 110, ErrorMessage = "GPU温度危险阈值必须在50-110摄氏度之间")]
    public double GpuTemperatureCriticalThreshold { get; set; } = 90;
    
    /// <summary>
    /// 内存使用率警告阈值（百分比）
    /// </summary>
    [Range(50, 95, ErrorMessage = "内存使用率警告阈值必须在50-95%之间")]
    public double MemoryUsageWarningThreshold { get; set; } = 80;
    
    /// <summary>
    /// 内存使用率危险阈值（百分比）
    /// </summary>
    [Range(70, 99, ErrorMessage = "内存使用率危险阈值必须在70-99%之间")]
    public double MemoryUsageCriticalThreshold { get; set; } = 90;
}

/// <summary>
/// UI配置
/// </summary>
public class UiConfiguration
{
    /// <summary>
    /// 主题名称
    /// </summary>
    public string Theme { get; set; } = "Light";
    
    /// <summary>
    /// 语言设置
    /// </summary>
    public string Language { get; set; } = "zh-CN";
    
    /// <summary>
    /// 窗口宽度
    /// </summary>
    public double WindowWidth { get; set; } = 1200;
    
    /// <summary>
    /// 窗口高度
    /// </summary>
    public double WindowHeight { get; set; } = 800;
    
    /// <summary>
    /// 窗口位置X
    /// </summary>
    public double WindowLeft { get; set; } = 100;
    
    /// <summary>
    /// 窗口位置Y
    /// </summary>
    public double WindowTop { get; set; } = 100;
    
    /// <summary>
    /// 是否最大化窗口
    /// </summary>
    public bool IsWindowMaximized { get; set; } = false;
    
    /// <summary>
    /// 是否显示系统托盘图标
    /// </summary>
    public bool ShowSystemTrayIcon { get; set; } = true;
    
    /// <summary>
    /// 是否最小化到系统托盘
    /// </summary>
    public bool MinimizeToSystemTray { get; set; } = true;
    
    /// <summary>
    /// 是否开机自启动
    /// </summary>
    public bool StartWithWindows { get; set; } = false;
    
    /// <summary>
    /// 图表刷新间隔 (毫秒)
    /// </summary>
    public int ChartRefreshInterval { get; set; } = 1000;
    
    /// <summary>
    /// 图表历史数据点数
    /// </summary>
    public int ChartHistoryPoints { get; set; } = 60;
}

/// <summary>
/// 通知配置
/// </summary>
public class NotificationConfiguration
{
    /// <summary>
    /// 是否启用通知
    /// </summary>
    public bool EnableNotifications { get; set; } = true;
    
    /// <summary>
    /// 是否启用声音通知
    /// </summary>
    public bool EnableSoundNotifications { get; set; } = true;
    
    /// <summary>
    /// 是否启用桌面通知
    /// </summary>
    public bool EnableDesktopNotifications { get; set; } = true;
    
    /// <summary>
    /// 是否启用系统托盘通知
    /// </summary>
    public bool EnableTrayNotifications { get; set; } = true;
    
    /// <summary>
    /// 通知显示时间 (毫秒)
    /// </summary>
    public int NotificationDisplayTime { get; set; } = 5000;
    
    /// <summary>
    /// 是否启用温度警告通知
    /// </summary>
    public bool EnableTemperatureWarnings { get; set; } = true;
    
    /// <summary>
    /// 是否启用使用率警告通知
    /// </summary>
    public bool EnableUsageWarnings { get; set; } = true;
    
    /// <summary>
    /// 是否启用硬件状态变化通知
    /// </summary>
    public bool EnableHardwareStatusNotifications { get; set; } = true;
}