using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Interfaces;
using MyComputerMonitor.Infrastructure.Services;

namespace MyComputerMonitor.Infrastructure.Extensions;

/// <summary>
/// Infrastructure层服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加Infrastructure层服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // 添加日志服务
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        
        // 注册硬件监控服务
        services.AddSingleton<IHardwareMonitorService, HardwareMonitorService>();
        
        // 注册配置服务
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // 注册系统托盘服务
        services.AddSingleton<ISystemTrayService, SystemTrayService>();
        
        // 注册自启动管理服务
        services.AddSingleton<IAutoStartService, AutoStartService>();
        
        // 注册系统信息服务
        services.AddSingleton<ISystemInfoService, SystemInfoService>();
        
        // 注册性能计数器服务
        services.AddSingleton<IPerformanceCounterService, PerformanceCounterService>();
        
        // 注册增强网络监控服务
        services.AddSingleton<IEnhancedNetworkMonitorService, EnhancedNetworkMonitorService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加硬件监控服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHardwareMonitoringServices(this IServiceCollection services)
    {
        services.AddSingleton<IHardwareMonitorService, HardwareMonitorService>();
        services.AddSingleton<IPerformanceCounterService, PerformanceCounterService>();
        services.AddSingleton<IEnhancedNetworkMonitorService, EnhancedNetworkMonitorService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加配置管理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddConfigurationManagementServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加系统集成服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSystemIntegrationServices(this IServiceCollection services)
    {
        services.AddSingleton<ISystemTrayService, SystemTrayService>();
        services.AddSingleton<IAutoStartService, AutoStartService>();
        services.AddSingleton<ISystemInfoService, SystemInfoService>();
        
        return services;
    }
}