using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Extensions;
using MyComputerMonitor.Infrastructure.Extensions;
using MyComputerMonitor.Infrastructure.Services;
using MyComputerMonitor.WPF.Views;
using MyComputerMonitor.WPF.ViewModels;

namespace MyComputerMonitor.WPF
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// 服务提供者
    /// </summary>
    public IServiceProvider ServiceProvider => _host?.Services ?? throw new InvalidOperationException("服务提供者未初始化");

    /// <summary>
    /// 应用程序启动时
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // 构建主机和服务容器
            _host = CreateHostBuilder().Build();

            // 启动主机
            _host.Start();

            // 创建并显示主窗口
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"应用程序启动失败: {ex.Message}\n\n详细信息:\n{ex}", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// 应用程序退出时
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _host?.Dispose();
        }
        catch (Exception ex)
        {
            // 记录错误但不阻止退出
            System.Diagnostics.Debug.WriteLine($"应用程序退出时发生错误: {ex.Message}");
        }
        finally
        {
            base.OnExit(e);
        }
    }

    /// <summary>
    /// 创建主机构建器
    /// </summary>
    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // 注册核心服务
                services.AddCoreServices();
                
                // 注册基础设施服务
                services.AddInfrastructureServices();
                
                // 注册WPF相关服务
                services.AddWpfServices();
                
                // 配置日志
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
            });
    }
}

/// <summary>
/// WPF服务注册扩展方法
/// </summary>
public static class WpfServiceExtensions
{
    /// <summary>
    /// 添加WPF相关服务
    /// </summary>
    public static IServiceCollection AddWpfServices(this IServiceCollection services)
    {
        // 注册窗口
        services.AddTransient<MainWindow>();
        services.AddTransient<TrayPopupWindow>();
        services.AddTransient<CpuMonitorWindow>();
        services.AddTransient<GpuMonitorWindow>();
        services.AddTransient<MemoryMonitorWindow>();
        services.AddTransient<NetworkMonitorWindow>();
        services.AddTransient<TemperatureMonitorWindow>();
        services.AddTransient<SettingsWindow>();
        
        // 注册ViewModel
        services.AddTransient<MainViewModel>();
        services.AddTransient<TrayPopupViewModel>();
        services.AddTransient<TemperatureMonitorViewModel>();
        
        return services;
    }
}
}