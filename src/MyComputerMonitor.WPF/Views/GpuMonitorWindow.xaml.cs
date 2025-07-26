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
    /// GPU 监控窗口
    /// </summary>
    public partial class GpuMonitorWindow : Window
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly ILogger<GpuMonitorWindow> _logger;
        private readonly DispatcherTimer _updateTimer;
        
        // 图表数据
        private readonly Queue<DataPoint> _gpuUsageHistory = new();
        private readonly Queue<DataPoint> _memoryUsageHistory = new();
        private readonly Queue<DataPoint> _temperatureHistory = new();
        
        // 图表模型
        private PlotModel _gpuUsageChart = null!;
        private PlotModel _memoryUsageChart = null!;
        private PlotModel _temperatureChart = null!;
        
        // 图表系列
        private LineSeries _gpuUsageSeries = null!;
        private LineSeries _memoryUsageSeries = null!;
        private LineSeries _temperatureSeries = null!;
        
        // GPU进程数据
        public ObservableCollection<GpuProcessInfo> GpuProcesses { get; } = new();
        
        // 时间范围设置
        private int _timeRangeMinutes = 5;
        
        public GpuMonitorWindow(IHardwareMonitorService hardwareMonitorService, ILogger<GpuMonitorWindow> logger)
        {
            _hardwareMonitorService = hardwareMonitorService;
            _logger = logger;
            
            InitializeComponent();
            
            // 创建更新定时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            
            // 窗口事件
            Loaded += GpuMonitorWindow_Loaded;
            Closed += GpuMonitorWindow_Closed;
        }
    
        
        /// <summary>
        /// 窗口加载事件
        /// </summary>
        private async void GpuMonitorWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("GPU监控窗口开始加载...");
                
                InitializeCharts();
                await UpdateDataAsync();
                _updateTimer.Start();
                
                _logger.LogInformation("GPU监控窗口加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载GPU监控窗口时发生错误");
                MessageBox.Show($"加载GPU监控窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void GpuMonitorWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                _updateTimer?.Stop();
                _logger.LogInformation("GPU监控窗口已关闭");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭GPU监控窗口时发生错误");
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
    /// 初始化图表
    /// </summary>
    private void InitializeCharts()
    {
        // GPU使用率图表
        _gpuUsageChart = new PlotModel
        {
            Title = "GPU 使用率历史",
            Background = OxyColors.Transparent,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.Black
        };
        
        _gpuUsageChart.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "HH:mm:ss",
            Title = "时间",
            IntervalType = DateTimeIntervalType.Seconds,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        _gpuUsageChart.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "使用率 (%)",
            Minimum = 0,
            Maximum = 100,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        _gpuUsageSeries = new LineSeries
        {
            Title = "GPU使用率",
            Color = OxyColors.Blue,
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };
        
        _gpuUsageChart.Series.Add(_gpuUsageSeries);
        GpuUsagePlot.Model = _gpuUsageChart;
        
        // 显存使用率图表
        _memoryUsageChart = new PlotModel
        {
            Title = "显存使用率历史",
            Background = OxyColors.Transparent,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.Black
        };
        
        _memoryUsageChart.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "HH:mm:ss",
            Title = "时间",
            IntervalType = DateTimeIntervalType.Seconds,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        _memoryUsageChart.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "使用率 (%)",
            Minimum = 0,
            Maximum = 100,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        _memoryUsageSeries = new LineSeries
        {
            Title = "显存使用率",
            Color = OxyColors.Green,
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };
        
        _memoryUsageChart.Series.Add(_memoryUsageSeries);
        MemoryUsagePlot.Model = _memoryUsageChart;
        
        // 温度图表
        _temperatureChart = new PlotModel
        {
            Title = "GPU 温度历史",
            Background = OxyColors.Transparent,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.Black
        };
        
        _temperatureChart.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "HH:mm:ss",
            Title = "时间",
            IntervalType = DateTimeIntervalType.Seconds,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        _temperatureChart.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度 (°C)",
            Minimum = 0,
            Maximum = 100,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
        
        _temperatureSeries = new LineSeries
        {
            Title = "GPU温度",
            Color = OxyColors.Red,
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };
        
        _temperatureChart.Series.Add(_temperatureSeries);
        TemperaturePlot.Model = _temperatureChart;
    }
    
    /// <summary>
    /// 更新数据
    /// </summary>
    private async Task UpdateDataAsync()
    {
        try
        {
            var hardwareData = await _hardwareMonitorService.GetHardwareDataAsync();
            var gpuInfo = hardwareData.GetPrimaryGpu();
            
            if (gpuInfo != null)
            {
                // 更新基本信息
                Dispatcher.Invoke(() =>
                {
                    GpuNameTextBlock.Text = $"名称: {gpuInfo.Name ?? "未知"}";
                    DriverVersionTextBlock.Text = $"驱动版本: {gpuInfo.DriverVersion ?? "未知"}";
                    VideoMemoryTextBlock.Text = $"显存总量: {FormatBytes(gpuInfo.MemoryTotal)}";
                    
                    // 更新时钟信息
                    var coreClock = gpuInfo.CoreClock;
                    var memoryClock = gpuInfo.MemoryClock;
                    CoreClockTextBlock.Text = coreClock > 0 ? $"核心时钟: {coreClock:F0} MHz" : "核心时钟: -- MHz";
                    MemoryClockTextBlock.Text = memoryClock > 0 ? $"显存时钟: {memoryClock:F0} MHz" : "显存时钟: -- MHz";
                    
                    // 更新实时状态
                    var usage = gpuInfo.UsagePercentage;
                    var memoryUsage = gpuInfo.MemoryUsagePercentage;
                    var temperature = gpuInfo.Temperature;
                    var fanSpeed = gpuInfo.FanSpeed;
                    var powerConsumption = gpuInfo.PowerConsumption;
                    
                    GpuUsageTextBlock.Text = $"GPU 使用率: {usage:F1}%";
                    MemoryUsageTextBlock.Text = $"显存使用率: {memoryUsage:F1}%";
                    TemperatureTextBlock.Text = temperature > 0 ? $"温度: {temperature:F1}°C" : "温度: --°C";
                    FanSpeedTextBlock.Text = fanSpeed > 0 ? $"风扇转速: {fanSpeed:F0} RPM" : "风扇转速: -- RPM";
                    PowerTextBlock.Text = powerConsumption > 0 ? $"功耗: {powerConsumption:F1} W" : "功耗: -- W";
                    
                    // 更新图表数据
                    UpdateGpuUsageChart(usage);
                    UpdateMemoryUsageChart(memoryUsage);
                    UpdateTemperatureChart(temperature);
                    
                    // 更新进程信息
                    UpdateGpuProcesses();
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新GPU数据时发生错误");
        }
    }
    
    /// <summary>
    /// 更新GPU使用率图表
    /// </summary>
    private void UpdateGpuUsageChart(double usage)
    {
        var now = DateTime.Now;
        var dataPoint = new DataPoint(DateTimeAxis.ToDouble(now), usage);
        
        _gpuUsageHistory.Enqueue(dataPoint);
        _gpuUsageSeries.Points.Add(dataPoint);
        
        // 清理过期数据
        var cutoffTime = now.AddMinutes(-_timeRangeMinutes);
        while (_gpuUsageHistory.Count > 0 && 
               DateTime.FromOADate(_gpuUsageHistory.Peek().X) < cutoffTime)
        {
            _gpuUsageHistory.Dequeue();
            if (_gpuUsageSeries.Points.Count > 0)
                _gpuUsageSeries.Points.RemoveAt(0);
        }
        
        // 更新X轴范围
        var xAxis = _gpuUsageChart.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom) as DateTimeAxis;
        if (xAxis != null)
        {
            xAxis.Minimum = DateTimeAxis.ToDouble(cutoffTime);
            xAxis.Maximum = DateTimeAxis.ToDouble(now);
        }
        
        _gpuUsageChart.InvalidatePlot(true);
    }
    
    /// <summary>
    /// 更新显存使用率图表
    /// </summary>
    private void UpdateMemoryUsageChart(double memoryUsage)
    {
        var now = DateTime.Now;
        var dataPoint = new DataPoint(DateTimeAxis.ToDouble(now), memoryUsage);
        
        _memoryUsageHistory.Enqueue(dataPoint);
        _memoryUsageSeries.Points.Add(dataPoint);
        
        // 清理过期数据
        var cutoffTime = now.AddMinutes(-_timeRangeMinutes);
        while (_memoryUsageHistory.Count > 0 && 
               DateTime.FromOADate(_memoryUsageHistory.Peek().X) < cutoffTime)
        {
            _memoryUsageHistory.Dequeue();
            if (_memoryUsageSeries.Points.Count > 0)
                _memoryUsageSeries.Points.RemoveAt(0);
        }
        
        // 更新X轴范围
        var xAxis = _memoryUsageChart.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom) as DateTimeAxis;
        if (xAxis != null)
        {
            xAxis.Minimum = DateTimeAxis.ToDouble(cutoffTime);
            xAxis.Maximum = DateTimeAxis.ToDouble(now);
        }
        
        _memoryUsageChart.InvalidatePlot(true);
    }
    
    /// <summary>
    /// 更新温度图表
    /// </summary>
    private void UpdateTemperatureChart(double temperature)
    {
        if (temperature <= 0) return;
        
        var now = DateTime.Now;
        var dataPoint = new DataPoint(DateTimeAxis.ToDouble(now), temperature);
        
        _temperatureHistory.Enqueue(dataPoint);
        _temperatureSeries.Points.Add(dataPoint);
        
        // 清理过期数据
        var cutoffTime = now.AddMinutes(-_timeRangeMinutes);
        while (_temperatureHistory.Count > 0 && 
               DateTime.FromOADate(_temperatureHistory.Peek().X) < cutoffTime)
        {
            _temperatureHistory.Dequeue();
            if (_temperatureSeries.Points.Count > 0)
                _temperatureSeries.Points.RemoveAt(0);
        }
        
        // 更新X轴范围
        var xAxis = _temperatureChart.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom) as DateTimeAxis;
        if (xAxis != null)
        {
            xAxis.Minimum = DateTimeAxis.ToDouble(cutoffTime);
            xAxis.Maximum = DateTimeAxis.ToDouble(now);
        }
        
        _temperatureChart.InvalidatePlot(true);
    }
    
    /// <summary>
    /// 更新GPU进程信息
    /// </summary>
    private void UpdateGpuProcesses()
    {
        // 模拟GPU进程数据
        var processes = new List<GpuProcessInfo>
        {
            new GpuProcessInfo { ProcessName = "chrome.exe", ProcessId = 1234, GpuUsage = "15%", MemoryUsage = "512 MB", Engine = "3D" },
            new GpuProcessInfo { ProcessName = "game.exe", ProcessId = 5678, GpuUsage = "85%", MemoryUsage = "2.1 GB", Engine = "3D" },
            new GpuProcessInfo { ProcessName = "video.exe", ProcessId = 9012, GpuUsage = "25%", MemoryUsage = "1.2 GB", Engine = "Video" }
        };

        GpuProcessesDataGrid.ItemsSource = processes;
    }
    
    /// <summary>
    /// 格式化字节数
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:F1} {sizes[order]}";
    }
    
    /// <summary>
    /// 刷新按钮点击事件
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateDataAsync();
    }
    
    /// <summary>
    /// 时间范围选择改变事件
    /// </summary>
    private void TimeRangeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        switch (TimeRangeComboBox.SelectedIndex)
        {
            case 0: _timeRangeMinutes = 5; break;
            case 1: _timeRangeMinutes = 15; break;
            case 2: _timeRangeMinutes = 30; break;
            case 3: _timeRangeMinutes = 60; break;
            case 4: _timeRangeMinutes = 120; break;
            default: _timeRangeMinutes = 30; break;
        }
        
        // 清理超出时间范围的数据
        var cutoffTime = DateTime.Now.AddMinutes(-_timeRangeMinutes);
        
        // 清理GPU使用率图表数据
        while (_gpuUsageHistory.Count > 0 && 
               DateTime.FromOADate(_gpuUsageHistory.Peek().X) < cutoffTime)
        {
            _gpuUsageHistory.Dequeue();
            if (_gpuUsageSeries.Points.Count > 0)
                _gpuUsageSeries.Points.RemoveAt(0);
        }
        
        // 清理显存使用率图表数据
        while (_memoryUsageHistory.Count > 0 && 
               DateTime.FromOADate(_memoryUsageHistory.Peek().X) < cutoffTime)
        {
            _memoryUsageHistory.Dequeue();
            if (_memoryUsageSeries.Points.Count > 0)
                _memoryUsageSeries.Points.RemoveAt(0);
        }
        
        // 清理温度图表数据
        while (_temperatureHistory.Count > 0 && 
               DateTime.FromOADate(_temperatureHistory.Peek().X) < cutoffTime)
        {
            _temperatureHistory.Dequeue();
            if (_temperatureSeries.Points.Count > 0)
                _temperatureSeries.Points.RemoveAt(0);
        }
        
        // 刷新图表
        _gpuUsageChart?.InvalidatePlot(true);
        _memoryUsageChart?.InvalidatePlot(true);
        _temperatureChart?.InvalidatePlot(true);
    }
}

/// <summary>
/// GPU进程信息
/// </summary>
public class GpuProcessInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string GpuUsage { get; set; } = string.Empty;
    public string MemoryUsage { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
}
}