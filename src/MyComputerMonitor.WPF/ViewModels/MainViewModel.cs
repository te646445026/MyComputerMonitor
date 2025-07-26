using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Interfaces;
using MyComputerMonitor.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MyComputerMonitor.WPF.ViewModels
{
    /// <summary>
    /// 主视图模型
    /// </summary>
    public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IHardwareMonitorService _hardwareMonitorService;
    private readonly ISystemTrayService _systemTrayService;
    private readonly IConfigurationService _configurationService;

    [ObservableProperty]
    private string _title = "电脑硬件监控";

    [ObservableProperty]
    private bool _isMinimizedToTray = false;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    /// <summary>
    /// 导航项集合
    /// </summary>
    public ObservableCollection<NavigationItem> NavigationItems { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    /// <param name="hardwareMonitorService">硬件监控服务</param>
    /// <param name="systemTrayService">系统托盘服务</param>
    /// <param name="configurationService">配置服务</param>
    public MainViewModel(
        ILogger<MainViewModel> logger,
        IHardwareMonitorService hardwareMonitorService,
        ISystemTrayService systemTrayService,
        IConfigurationService configurationService)
    {
        _logger = logger;
        _hardwareMonitorService = hardwareMonitorService;
        _systemTrayService = systemTrayService;
        _configurationService = configurationService;

        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new("仪表板", "Dashboard", "📊"),
            new("CPU", "Cpu", "🖥️"),
            new("GPU", "Gpu", "🎮"),
            new("内存", "Memory", "💾"),
            new("网络", "Network", "🌐"),
            new("设置", "Settings", "⚙️"),
            new("关于", "About", "ℹ️")
        };

        // 初始化命令
        InitializeCommands();
        
        // 启动硬件监控（使用 Task.Run 避免阻塞构造函数）
        Task.Run(async () => await InitializeAsync());
    }

    /// <summary>
    /// 初始化命令
    /// </summary>
    private void InitializeCommands()
    {
        NavigateCommand = new RelayCommand<string>(Navigate);
        MinimizeToTrayCommand = new RelayCommand(MinimizeToTray);
        RestoreFromTrayCommand = new RelayCommand(RestoreFromTray);
        ExitApplicationCommand = new RelayCommand(ExitApplication);
    }

    /// <summary>
    /// 异步初始化
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("正在初始化主视图模型...");
            
            // 启动硬件监控服务
            await _hardwareMonitorService.StartMonitoringAsync();
            
            // 设置默认视图
            Navigate("Dashboard");
            
            _logger.LogInformation("主视图模型初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化主视图模型时发生错误");
        }
    }

    /// <summary>
    /// 导航命令
    /// </summary>
    public ICommand NavigateCommand { get; private set; } = null!;

    /// <summary>
    /// 最小化到托盘命令
    /// </summary>
    public ICommand MinimizeToTrayCommand { get; private set; } = null!;

    /// <summary>
    /// 从托盘恢复命令
    /// </summary>
    public ICommand RestoreFromTrayCommand { get; private set; } = null!;

    /// <summary>
    /// 退出应用程序命令
    /// </summary>
    public ICommand ExitApplicationCommand { get; private set; } = null!;

    /// <summary>
    /// 导航到指定视图
    /// </summary>
    /// <param name="viewName">视图名称</param>
    private void Navigate(string? viewName)
    {
        if (string.IsNullOrEmpty(viewName))
            return;

        try
        {
            _logger.LogDebug("导航到视图: {ViewName}", viewName);
            
            // 这里将在后续实现具体的视图模型创建逻辑
            // CurrentViewModel = CreateViewModel(viewName);
            
            // 更新选中的标签页索引
            var index = NavigationItems.ToList().FindIndex(item => item.ViewName == viewName);
            if (index >= 0)
            {
                SelectedTabIndex = index;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到视图 {ViewName} 时发生错误", viewName);
        }
    }

    /// <summary>
    /// 最小化到系统托盘
    /// </summary>
    private void MinimizeToTray()
    {
        try
        {
            IsMinimizedToTray = true;
            _systemTrayService.ShowTrayIcon();
            _logger.LogDebug("应用程序已最小化到系统托盘");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "最小化到系统托盘时发生错误");
        }
    }

    /// <summary>
    /// 从系统托盘恢复
    /// </summary>
    private void RestoreFromTray()
    {
        try
        {
            IsMinimizedToTray = false;
            _systemTrayService.HideTrayIcon();
            _logger.LogDebug("应用程序已从系统托盘恢复");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从系统托盘恢复时发生错误");
        }
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    private void ExitApplication()
    {
        try
        {
            _logger.LogInformation("正在退出应用程序...");
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退出应用程序时发生错误");
        }
    }
}

/// <summary>
/// 导航项
/// </summary>
/// <param name="DisplayName">显示名称</param>
/// <param name="ViewName">视图名称</param>
/// <param name="Icon">图标</param>
public record NavigationItem(string DisplayName, string ViewName, string Icon);
}