using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Interfaces;
using MyComputerMonitor.Core.Models;
using MyComputerMonitor.Infrastructure.Services;
using MyComputerMonitor.WPF.ViewModels;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace MyComputerMonitor.WPF.Views
{
    /// <summary>
    /// 主窗口
    /// </summary>
    public partial class MainWindow : Window
{
    private readonly IHardwareMonitorService _hardwareMonitorService;
    private readonly ISystemTrayService _systemTrayService;
    private readonly ILogger<MainWindow> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DispatcherTimer _updateTimer;
    private TrayPopupWindow? _trayPopupWindow;
    private readonly TemperatureMonitorViewModel _temperatureViewModel;
    
    // 图表数据
    private readonly Queue<DataPoint> _cpuData = new();
    private readonly Queue<DataPoint> _gpuData = new();
    private readonly Queue<DataPoint> _memoryData = new();
    
    // 图表模型
    private PlotModel CpuChart = null!;
    private PlotModel GpuChart = null!;
    private PlotModel MemoryChart = null!;
    
    // 图表系列
    private readonly LineSeries _cpuSeries;
    private readonly LineSeries _gpuSeries;
    private readonly LineSeries _memorySeries;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindow(
        IHardwareMonitorService hardwareMonitorService,
        ISystemTrayService systemTrayService,
        ILogger<MainWindow> logger,
        IServiceProvider serviceProvider)
    {
        _hardwareMonitorService = hardwareMonitorService ?? throw new ArgumentNullException(nameof(hardwareMonitorService));
        _systemTrayService = systemTrayService ?? throw new ArgumentNullException(nameof(systemTrayService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        // 初始化温度监控ViewModel
        var temperatureLogger = _serviceProvider.GetRequiredService<ILogger<TemperatureMonitorViewModel>>();
        _temperatureViewModel = new TemperatureMonitorViewModel(temperatureLogger, _hardwareMonitorService);
        
        InitializeComponent();
        
        // 设置主ViewModel作为DataContext
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        DataContext = mainViewModel;
        
        // 设置温度监控数据绑定
        TemperatureDataGrid.DataContext = _temperatureViewModel;
        
        // 初始化图表系列
        _cpuSeries = new LineSeries { Title = "CPU使用率", Color = OxyColors.Blue, StrokeThickness = 2, MarkerType = MarkerType.None };
        _gpuSeries = new LineSeries { Title = "GPU使用率", Color = OxyColors.Green, StrokeThickness = 2, MarkerType = MarkerType.None };
        _memorySeries = new LineSeries { Title = "内存使用率", Color = OxyColors.Red, StrokeThickness = 2, MarkerType = MarkerType.None };

        InitializeCharts();
        InitializeTray();

        // 创建更新定时器
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _updateTimer.Tick += UpdateTimer_Tick;

        // 绑定窗口事件
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    /// <summary>
    /// 窗口加载事件
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 等待温度ViewModel完全初始化
            await Task.Delay(1000);
            
            // 立即更新一次数据
            await UpdateHardwareInfoAsync();
            
            // 开始定时更新
            _updateTimer.Start();
            
            _logger.LogInformation("主窗口已加载并开始更新硬件信息");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载主窗口时发生错误");
            MessageBox.Show($"加载主窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        try
        {
            _updateTimer?.Stop();
            _systemTrayService?.Dispose();
            _temperatureViewModel?.Dispose();
            _logger.LogInformation("主窗口已关闭");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关闭主窗口时发生错误");
        }
    }

    /// <summary>
    /// 定时器更新事件
    /// </summary>
    private async void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            await UpdateHardwareInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定时器更新时发生错误");
        }
    }

    /// <summary>
    /// 初始化图表
    /// </summary>
    private void InitializeCharts()
    {
        // CPU 图表
        CpuChart = new PlotModel
        {
            Title = "CPU 使用率 (%)",
            Background = OxyColors.Transparent,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.Black
        };
        
        CpuChart.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "HH:mm:ss",
            Title = "时间",
            IntervalType = DateTimeIntervalType.Seconds,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        CpuChart.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "使用率 (%)",
            Minimum = 0,
            Maximum = 100,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        CpuChart.Series.Add(_cpuSeries);
        CpuChartView.Model = CpuChart;
        
        // GPU 图表
        GpuChart = new PlotModel
        {
            Title = "GPU 使用率 (%)",
            Background = OxyColors.Transparent,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.Black
        };
        
        GpuChart.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "HH:mm:ss",
            Title = "时间",
            IntervalType = DateTimeIntervalType.Seconds,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        GpuChart.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "使用率 (%)",
            Minimum = 0,
            Maximum = 100,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        GpuChart.Series.Add(_gpuSeries);
        GpuChartView.Model = GpuChart;
        
        // 内存图表
        MemoryChart = new PlotModel
        {
            Title = "内存使用率 (%)",
            Background = OxyColors.Transparent,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.Black
        };
        
        MemoryChart.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "HH:mm:ss",
            Title = "时间",
            IntervalType = DateTimeIntervalType.Seconds,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        MemoryChart.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "使用率 (%)",
            Minimum = 0,
            Maximum = 100,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        MemoryChart.Series.Add(_memorySeries);
        MemoryChartView.Model = MemoryChart;
    }

    /// <summary>
    /// 初始化系统托盘
    /// </summary>
    private void InitializeTray()
    {
        try
        {
            // 初始化系统托盘
            _systemTrayService.Initialize();
            _systemTrayService.SetTooltipText("电脑硬件监控");
            
            // 设置托盘事件处理
            _systemTrayService.TrayIconClicked += OnTrayIconClicked;
            _systemTrayService.TrayIconDoubleClicked += OnTrayIconDoubleClicked;
            _systemTrayService.ShowMainWindowRequested += OnShowMainWindowRequested;
            _systemTrayService.ExitApplicationRequested += OnExitApplicationRequested;
            
            // 显示托盘图标
            _systemTrayService.ShowTrayIcon();
            
            _logger.LogInformation("系统托盘已初始化");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化系统托盘失败");
            MessageBox.Show($"初始化系统托盘失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// 异步更新硬件信息
    /// </summary>
    private async Task UpdateHardwareInfoAsync()
    {
        try
        {
            var now = DateTime.Now;
            
            // 获取硬件数据
            var hardwareData = await _hardwareMonitorService.GetHardwareDataAsync();
            
            var cpuInfo = hardwareData.Cpus?.FirstOrDefault();
            var gpuInfo = hardwareData.Gpus?.FirstOrDefault();
            var memoryInfo = hardwareData.Memory;
            
            // 在UI线程中更新所有UI元素
            await Dispatcher.InvokeAsync(() =>
            {
                // 更新UI信息卡片
                UpdateInfoCards(cpuInfo, gpuInfo, memoryInfo);
                
                // 更新图表数据
                UpdateChartData(now, cpuInfo, gpuInfo, memoryInfo);
                
                // 更新温度监控数据
                _temperatureViewModel.RefreshCommand.Execute(null);
            });
            
            // 更新托盘提示文本（包含温度信息）
            var cpuUsage = cpuInfo?.UsagePercentage ?? 0;
            var cpuTemp = cpuInfo?.Temperature ?? 0;
            var gpuTemp = gpuInfo?.Temperature ?? 0;
            
            var tooltipText = $"CPU: {cpuUsage:F1}%";
            if (cpuTemp > 0)
                tooltipText += $", {cpuTemp:F1}°C";
            if (gpuTemp > 0)
                tooltipText += $" | GPU: {gpuTemp:F1}°C";
                
            Dispatcher.Invoke(() =>
            {
                _systemTrayService?.SetTooltipText(tooltipText);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新硬件信息时发生错误");
        }
    }

    /// <summary>
    /// 更新信息卡片
    /// </summary>
    private void UpdateInfoCards(Core.Models.CpuInfo? cpuInfo, Core.Models.GpuInfo? gpuInfo, Core.Models.MemoryInfo? memoryInfo)
    {
        // 更新CPU信息
        if (cpuInfo != null)
        {
            CpuUsageDisplay.Text = $"{cpuInfo.UsagePercentage:F1}%";
            CpuTempDisplay.Text = cpuInfo.Temperature > 0 ? $"{cpuInfo.Temperature:F1}°C" : "N/A";
            CpuNameDisplay.Text = cpuInfo.Name ?? "未知处理器";
        }
        else
        {
            CpuUsageDisplay.Text = "--";
            CpuTempDisplay.Text = "--";
            CpuNameDisplay.Text = "未知处理器";
        }

        // 更新GPU信息
        if (gpuInfo != null)
        {
            GpuUsageDisplay.Text = $"{gpuInfo.UsagePercentage:F1}%";
            GpuTempDisplay.Text = gpuInfo.Temperature > 0 ? $"{gpuInfo.Temperature:F1}°C" : "N/A";
            GpuNameDisplay.Text = gpuInfo.Name ?? "未知显卡";
        }
        else
        {
            GpuUsageDisplay.Text = "--";
            GpuTempDisplay.Text = "--";
            GpuNameDisplay.Text = "未知显卡";
        }

        // 更新内存信息
        if (memoryInfo != null)
        {
            MemoryUsageDisplay.Text = $"{memoryInfo.UsagePercentage:F1}%";
            MemoryDetailsDisplay.Text = $"{FormatMB(memoryInfo.UsedMemory)} / {FormatMB(memoryInfo.TotalMemory)}";
            MemoryAvailableDisplay.Text = $"可用: {FormatMB(memoryInfo.AvailableMemory)}";
        }
        else
        {
            MemoryUsageDisplay.Text = "--";
            MemoryDetailsDisplay.Text = "-- / --";
            MemoryAvailableDisplay.Text = "可用: --";
        }

        // 更新系统信息
        OsInfoDisplay.Text = Environment.OSVersion.ToString();
        UptimeDisplay.Text = FormatUptime(TimeSpan.FromMilliseconds(Environment.TickCount));
        ProcessCountDisplay.Text = $"{Process.GetProcesses().Length} 个进程";

        LastUpdateDisplay.Text = $"最后更新: {DateTime.Now:HH:mm:ss}";
    }

    /// <summary>
    /// 更新图表数据
    /// </summary>
    private void UpdateChartData(DateTime now, Core.Models.CpuInfo? cpuInfo, Core.Models.GpuInfo? gpuInfo, Core.Models.MemoryInfo? memoryInfo)
    {
        // 更新 CPU 图表
        if (cpuInfo != null)
        {
            _cpuSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(now), cpuInfo.UsagePercentage));
            
            // 保持最近100个数据点
            if (_cpuSeries.Points.Count > 100)
            {
                _cpuSeries.Points.RemoveAt(0);
            }
        }

        // 更新 GPU 图表
        if (gpuInfo != null)
        {
            _gpuSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(now), gpuInfo.UsagePercentage));
            
            // 保持最近100个数据点
            if (_gpuSeries.Points.Count > 100)
            {
                _gpuSeries.Points.RemoveAt(0);
            }
        }

        // 更新内存图表
        if (memoryInfo != null)
        {
            _memorySeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(now), memoryInfo.UsagePercentage));
            
            // 保持最近100个数据点
            if (_memorySeries.Points.Count > 100)
            {
                _memorySeries.Points.RemoveAt(0);
            }
        }

        // 刷新图表
        CpuChartView?.InvalidatePlot(true);
        GpuChartView?.InvalidatePlot(true);
        MemoryChartView?.InvalidatePlot(true);
    }

    /// <summary>
    /// 格式化字节数
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        const long GB = 1024 * 1024 * 1024;
        const long MB = 1024 * 1024;
        
        if (bytes >= GB)
            return $"{bytes / (double)GB:F1} GB";
        else if (bytes >= MB)
            return $"{bytes / (double)MB:F1} MB";
        else
            return $"{bytes / 1024.0:F1} KB";
    }

    /// <summary>
    /// 格式化MB数值
    /// </summary>
    private static string FormatMB(long mb)
    {
        const long GB_IN_MB = 1024;
        
        if (mb >= GB_IN_MB)
            return $"{mb / (double)GB_IN_MB:F1} GB";
        else
            return $"{mb:F0} MB";
    }

    /// <summary>
    /// 格式化运行时间
    /// </summary>
    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{uptime.Days}天 {uptime.Hours}小时 {uptime.Minutes}分钟";
        else if (uptime.TotalHours >= 1)
            return $"{uptime.Hours}小时 {uptime.Minutes}分钟";
        else
            return $"{uptime.Minutes}分钟";
    }

    #region 托盘事件处理

    /// <summary>
    /// 托盘图标单击事件
    /// </summary>
    private async void OnTrayIconClicked(object? sender, EventArgs e)
    {
        try
        {
            // 创建托盘弹出窗口
            if (_trayPopupWindow == null)
            {
                _trayPopupWindow = _serviceProvider.GetRequiredService<TrayPopupWindow>();
                _trayPopupWindow.ShowMainWindowRequested += (s, args) => ShowMainWindow();
            }

            // 显示弹出窗口并确保数据更新
            _trayPopupWindow.Show();
            _trayPopupWindow.Activate();
            
            // 确保数据更新正在进行
            if (_trayPopupWindow.DataContext is TrayPopupViewModel viewModel)
            {
                await viewModel.EnsureUpdatingAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "显示托盘弹出窗口时发生错误");
        }
    }

    /// <summary>
    /// 托盘图标双击事件
    /// </summary>
    private void OnTrayIconDoubleClicked(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    /// <summary>
    /// 显示主窗口请求事件
    /// </summary>
    private void OnShowMainWindowRequested(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    /// <summary>
    /// 退出应用程序请求事件
    /// </summary>
    private void OnExitApplicationRequested(object? sender, EventArgs e)
    {
        ExitApplication();
    }

    /// <summary>
    /// 显示主窗口
    /// </summary>
    private void ShowMainWindow()
    {
        try
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Focus();
            _logger.LogDebug("主窗口已显示");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "显示主窗口时发生错误");
        }
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    private void ExitApplication()
    {
        try
        {
            _updateTimer?.Stop();
            _systemTrayService?.Dispose();
            _trayPopupWindow?.Close();
            Application.Current.Shutdown();
            _logger.LogInformation("应用程序已退出");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退出应用程序时发生错误");
        }
    }

    #endregion

    #region 窗口事件

    /// <summary>
    /// 最小化按钮点击事件
    /// </summary>
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        _systemTrayService?.ShowBalloonTip("电脑硬件监控", "程序已最小化到系统托盘", 2000);
    }

    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.Owner = this;
            var result = settingsWindow.ShowDialog();
            
            if (result == true)
            {
                _logger.LogInformation("设置已更新");
                // 这里可以添加设置更新后的处理逻辑
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开设置窗口时发生错误");
            MessageBox.Show($"打开设置窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// CPU监控按钮点击事件
    /// </summary>
    private void CpuMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cpuMonitorWindow = _serviceProvider.GetRequiredService<CpuMonitorWindow>();
                cpuMonitorWindow.Show();
                _logger.LogInformation("CPU监控窗口已打开");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开CPU监控窗口时发生错误");
                MessageBox.Show($"打开CPU监控窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// GPU监控按钮点击事件
        /// </summary>
        private void GpuMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gpuMonitorWindow = _serviceProvider.GetRequiredService<GpuMonitorWindow>();
                gpuMonitorWindow.Show();
                _logger.LogInformation("GPU监控窗口已打开");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开GPU监控窗口时发生错误");
                MessageBox.Show($"打开GPU监控窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 内存监控按钮点击事件
        /// </summary>
        private void MemoryMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var memoryMonitorWindow = _serviceProvider.GetRequiredService<MemoryMonitorWindow>();
                memoryMonitorWindow.Show();
                _logger.LogInformation("内存监控窗口已打开");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开内存监控窗口时发生错误");
                MessageBox.Show($"打开内存监控窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 网络监控按钮点击事件
        /// </summary>
        private void NetworkMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var networkMonitorWindow = _serviceProvider.GetRequiredService<NetworkMonitorWindow>();
                networkMonitorWindow.Show();
                _logger.LogInformation("网络监控窗口已打开");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开网络监控窗口时发生错误");
                MessageBox.Show($"打开网络监控窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    /// <summary>
    /// 温度监控按钮点击事件
    /// </summary>
    private void TemperatureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var temperatureWindow = _serviceProvider.GetRequiredService<TemperatureMonitorWindow>();
            temperatureWindow.Show();
            _logger.LogInformation("温度监控窗口已打开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开温度监控窗口时发生错误");
            MessageBox.Show($"打开温度监控窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 窗口状态改变事件
    /// </summary>
    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _systemTrayService?.ShowBalloonTip("电脑硬件监控", "程序已最小化到系统托盘", 2000);
        }
        
        base.OnStateChanged(e);
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        // 最小化到托盘而不是关闭
        e.Cancel = true;
        Hide();
        _systemTrayService?.ShowBalloonTip("电脑硬件监控", "程序已最小化到系统托盘，右键托盘图标可退出", 3000);
    }

    #endregion
}
}