using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Interfaces;
using MyComputerMonitor.Core.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace MyComputerMonitor.WPF.Views
{
    /// <summary>
    /// CPU 监控窗口
    /// </summary>
    public partial class CpuMonitorWindow : Window
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly ILogger<CpuMonitorWindow> _logger;
        private readonly DispatcherTimer _updateTimer;
        
        // 图表数据
        private readonly Queue<DataPoint> _cpuUsageHistory = new();
        private PlotModel _cpuChart = null!;
        private LineSeries _cpuUsageSeries = null!;
        
        // CPU 核心数据
        public ObservableCollection<CpuCoreInfo> CpuCores { get; } = new();
        
        // 时间范围设置
        private int _timeRangeMinutes = 5;
        
        public CpuMonitorWindow(IHardwareMonitorService hardwareMonitorService, ILogger<CpuMonitorWindow> logger)
        {
            _hardwareMonitorService = hardwareMonitorService;
            _logger = logger;
            
            InitializeComponent();
            InitializeCores();
            
            // 设置数据绑定
            CpuCoresItemsControl.ItemsSource = CpuCores;
            
            // 创建更新定时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            
            // 窗口事件
            Loaded += CpuMonitorWindow_Loaded;
            Closed += CpuMonitorWindow_Closed;
        }
    
        
        /// <summary>
        /// 初始化图表
        /// </summary>
        private void InitializeCharts()
        {
            try
            {
                _logger.LogInformation("开始初始化CPU图表...");

                // 检查控件是否存在
                if (CpuUsageChart == null)
                {
                    _logger.LogError("CpuUsageChart 控件为 null");
                    throw new InvalidOperationException("CpuUsageChart 控件未找到");
                }

                _cpuChart = new PlotModel
                {
                    Title = "CPU 使用率历史",
                    Background = OxyColors.Transparent,
                    PlotAreaBorderColor = OxyColors.Gray,
                    TextColor = OxyColors.Black
                };
                
                _cpuChart.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "HH:mm:ss",
                    Title = "时间",
                    IntervalType = DateTimeIntervalType.Seconds,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                _cpuChart.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "使用率 (%)",
                    Minimum = 0,
                    Maximum = 100,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                _cpuUsageSeries = new LineSeries
                {
                    Title = "CPU使用率",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                
                _cpuChart.Series.Add(_cpuUsageSeries);
                CpuUsageChart.Model = _cpuChart;
                
                _logger.LogInformation("CPU图表初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化CPU图表时发生错误");
                throw;
            }
        }
        
        /// <summary>
        /// 窗口加载事件
        /// </summary>
        private async void CpuMonitorWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("CPU监控窗口开始加载...");
                
                InitializeCharts();
                await UpdateDataAsync();
                _updateTimer.Start();
                
                _logger.LogInformation("CPU监控窗口加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载CPU监控窗口时发生错误");
                MessageBox.Show($"加载CPU监控窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void CpuMonitorWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                _updateTimer?.Stop();
                _logger.LogInformation("CPU监控窗口已关闭");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭CPU监控窗口时发生错误");
            }
        }
        
        /// <summary>
        /// 定时器更新事件
        /// </summary>
        private async void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            await UpdateDataAsync();
        }
    
    /// <summary>
    /// 初始化CPU核心
    /// </summary>
    private void InitializeCores()
    {
        // 初始化8个核心（实际数量会在更新时调整）
        for (int i = 0; i < 8; i++)
        {
            CpuCores.Add(new CpuCoreInfo
            {
                CoreName = $"核心 {i}",
                Usage = 0
            });
        }
    }
    
    /// <summary>
    /// 更新数据
    /// </summary>
    private async Task UpdateDataAsync()
    {
        try
        {
            var hardwareData = await _hardwareMonitorService.GetHardwareDataAsync();
            var cpuInfo = hardwareData.GetPrimaryCpu();
            
            if (cpuInfo != null)
            {
                // 更新基本信息
                Dispatcher.Invoke(() =>
                {
                    CpuNameTextBlock.Text = $"名称: {cpuInfo.Name ?? "未知"}";
                    // CpuArchitectureText.Text = $"架构: {cpuInfo.Architecture ?? "未知"}"; // 控件不存在，注释掉
                    CpuCoresTextBlock.Text = $"核心数: {cpuInfo.CoreCount}";
                    CpuThreadsTextBlock.Text = $"线程数: {cpuInfo.ThreadCount}";
                    
                    // 更新实时状态
                    var usage = cpuInfo.UsagePercentage;
                    var frequency = cpuInfo.Frequency;
                    var temperature = cpuInfo.Temperature;
                    var power = cpuInfo.PowerConsumption;
                    
                    CpuUsageTextBlock.Text = $"使用率: {usage:F1}%";
                    CpuCurrentFrequencyTextBlock.Text = $"频率: {frequency:F0} MHz";
                    CpuTemperatureTextBlock.Text = temperature > 0 ? $"温度: {temperature:F1}°C" : "温度: --°C";
                    CpuPowerTextBlock.Text = power > 0 ? $"功耗: {power:F1} W" : "功耗: -- W";
                    
                    // 更新进度条
                    CpuUsageProgressBarMain.Value = usage;
                    
                    // 更新图表数据
                    UpdateChart(usage);
                    
                    // 更新核心信息
                    UpdateCoreInfo(cpuInfo);
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新CPU数据时发生错误");
        }
    }
    
    /// <summary>
    /// 更新图表
    /// </summary>
    private void UpdateChart(double usage)
    {
        var now = DateTime.Now;
        var dataPoint = new DataPoint(DateTimeAxis.ToDouble(now), usage);
        
        _cpuUsageSeries.Points.Add(dataPoint);
        
        // 保持指定时间范围内的数据
        var cutoffTime = now.AddMinutes(-_timeRangeMinutes);
        while (_cpuUsageSeries.Points.Count > 0 && 
               DateTime.FromOADate(_cpuUsageSeries.Points[0].X) < cutoffTime)
        {
            _cpuUsageSeries.Points.RemoveAt(0);
        }
        
        // 更新X轴范围
        var xAxis = _cpuChart.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom) as DateTimeAxis;
        if (xAxis != null)
        {
            xAxis.Minimum = DateTimeAxis.ToDouble(cutoffTime);
            xAxis.Maximum = DateTimeAxis.ToDouble(now);
        }
        
        CpuUsageChart.InvalidatePlot(true);
    }
    
    /// <summary>
    /// 更新核心信息
    /// </summary>
    private void UpdateCoreInfo(CpuInfo cpuInfo)
    {
        // 调整核心数量
        while (CpuCores.Count < cpuInfo.CoreCount)
        {
            CpuCores.Add(new CpuCoreInfo
            {
                CoreName = $"核心 {CpuCores.Count}",
                Usage = 0
            });
        }
        
        while (CpuCores.Count > cpuInfo.CoreCount)
        {
            CpuCores.RemoveAt(CpuCores.Count - 1);
        }
        
        // 更新核心使用率（使用真实数据）
        for (int i = 0; i < CpuCores.Count; i++)
        {
            if (i < cpuInfo.CoreUsages.Count)
            {
                // 使用真实的核心使用率数据
                CpuCores[i].Usage = cpuInfo.CoreUsages[i];
            }
            else
            {
                // 如果没有对应的核心数据，使用总体使用率作为备用
                CpuCores[i].Usage = cpuInfo.UsagePercentage;
            }
        }
    }
    
    #region 事件处理
    
    /// <summary>
    /// 刷新按钮点击事件
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await UpdateDataAsync();
            _logger.LogDebug("手动刷新CPU数据");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动刷新CPU数据时发生错误");
        }
    }
    
    #endregion
}

/// <summary>
/// CPU 核心信息
/// </summary>
public class CpuCoreInfo : INotifyPropertyChanged
{
    private string _coreName = string.Empty;
    private double _usage;
    
    public string CoreName
    {
        get => _coreName;
        set
        {
            _coreName = value;
            OnPropertyChanged(nameof(CoreName));
        }
    }
    
    public double Usage
    {
        get => _usage;
        set
        {
            _usage = value;
            OnPropertyChanged(nameof(Usage));
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
}