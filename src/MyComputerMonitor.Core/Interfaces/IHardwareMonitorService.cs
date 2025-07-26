using MyComputerMonitor.Core.Events;
using MyComputerMonitor.Core.Models;

namespace MyComputerMonitor.Core.Interfaces;

/// <summary>
/// 硬件监控服务接口
/// </summary>
public interface IHardwareMonitorService
{
    /// <summary>
    /// 硬件数据更新事件
    /// </summary>
    event EventHandler<HardwareDataUpdatedEventArgs>? HardwareDataUpdated;
    
    /// <summary>
    /// 硬件状态变化事件
    /// </summary>
    event EventHandler<HardwareStatusChangedEventArgs>? HardwareStatusChanged;
    
    /// <summary>
    /// 初始化硬件监控服务
    /// </summary>
    /// <returns>初始化任务</returns>
    Task InitializeAsync();
    
    /// <summary>
    /// 启动硬件监控
    /// </summary>
    /// <returns>启动任务</returns>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// 停止硬件监控
    /// </summary>
    /// <returns>停止任务</returns>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// 获取当前硬件数据
    /// </summary>
    /// <returns>系统硬件数据</returns>
    Task<SystemHardwareData> GetHardwareDataAsync();
    
    /// <summary>
    /// 获取指定类型的硬件信息
    /// </summary>
    /// <typeparam name="T">硬件信息类型</typeparam>
    /// <returns>硬件信息列表</returns>
    Task<IEnumerable<T>> GetHardwareInfoAsync<T>() where T : HardwareInfo;
    
    /// <summary>
    /// 刷新硬件数据
    /// </summary>
    /// <returns>刷新任务</returns>
    Task RefreshAsync();
    
    /// <summary>
    /// 设置监控间隔
    /// </summary>
    /// <param name="interval">监控间隔</param>
    void SetMonitoringInterval(TimeSpan interval);
    
    /// <summary>
    /// 获取监控状态
    /// </summary>
    /// <returns>是否正在监控</returns>
    bool IsMonitoring { get; }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    void Dispose();
}