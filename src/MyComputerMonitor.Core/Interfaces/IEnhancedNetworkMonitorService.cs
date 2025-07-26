using MyComputerMonitor.Core.Models;

namespace MyComputerMonitor.Core.Interfaces;

/// <summary>
/// 增强网络监控服务接口
/// </summary>
public interface IEnhancedNetworkMonitorService
{
    /// <summary>
    /// 预热网络监控，初始化基准数据
    /// </summary>
    /// <returns>预热任务</returns>
    Task WarmupAsync();
    
    /// <summary>
    /// 获取所有物理网络适配器信息
    /// </summary>
    /// <returns>物理网络适配器信息列表</returns>
    Task<List<NetworkInfo>> GetPhysicalNetworkAdaptersAsync();
    
    /// <summary>
    /// 获取指定网络适配器的详细信息
    /// </summary>
    /// <param name="adapterId">适配器ID或名称</param>
    /// <returns>网络适配器信息，如果未找到则返回null</returns>
    Task<NetworkInfo?> GetNetworkAdapterInfoAsync(string adapterId);
}