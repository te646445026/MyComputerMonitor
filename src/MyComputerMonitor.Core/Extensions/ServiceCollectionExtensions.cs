using Microsoft.Extensions.DependencyInjection;
using MyComputerMonitor.Core.Interfaces;

namespace MyComputerMonitor.Core.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加核心服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // 注册核心服务
        services.AddHardwareMonitoring();
        services.AddConfigurationServices();
        services.AddSystemServices();
        
        return services;
    }
    
    /// <summary>
    /// 添加硬件监控服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHardwareMonitoring(this IServiceCollection services)
    {
        // 注册硬件监控服务接口
        // 具体实现将在Infrastructure层中提供
        // services.AddSingleton<IHardwareMonitorService, HardwareMonitorService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加配置服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        // 注册配置服务接口
        // 具体实现将在Infrastructure层中提供
        // services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加系统服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSystemServices(this IServiceCollection services)
    {
        // 注册系统相关服务接口
        // 具体实现将在Infrastructure层中提供
        // services.AddSingleton<ISystemTrayService, SystemTrayService>();
        // services.AddSingleton<IAutoStartService, AutoStartService>();
        // services.AddSingleton<ISystemInfoService, SystemInfoService>();
        // services.AddSingleton<IPerformanceCounterService, PerformanceCounterService>();
        
        return services;
    }
}