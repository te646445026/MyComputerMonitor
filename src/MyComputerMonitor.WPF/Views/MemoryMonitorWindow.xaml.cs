using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Interfaces;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace MyComputerMonitor.WPF.Views
{
    /// <summary>
    /// 内存监控窗口
    /// </summary>
    public partial class MemoryMonitorWindow : Window
    {
        private readonly IHardwareMonitorService _hardwareService;
        private readonly ILogger<MemoryMonitorWindow> _logger;
        private readonly DispatcherTimer _updateTimer;
        
        // 图表数据
        private readonly List<DataPoint> _memoryUsageData = new();
        private readonly List<DataPoint> _committedMemoryData = new();
        private readonly List<DataPoint> _cachedMemoryData = new();
        
        // 图表模型
        private PlotModel _memoryUsagePlotModel = null!;
        private PlotModel _memoryDetailPlotModel = null!;
        
        // 图表系列
        private LineSeries _memoryUsageSeries = null!;
        private LineSeries _committedMemorySeries = null!;
        private LineSeries _cachedMemorySeries = null!;
        
        // 内存模块和进程数据
        public ObservableCollection<MemoryModuleInfo> MemoryModules { get; set; } = new();
        public ObservableCollection<MemoryProcessInfo> MemoryProcesses { get; set; } = new();
        
        // 时间范围（分钟）
        private int _timeRangeMinutes = 30;
        
        public MemoryMonitorWindow(IHardwareMonitorService hardwareService, ILogger<MemoryMonitorWindow> logger)
        {
            _hardwareService = hardwareService;
            _logger = logger;
            
            InitializeComponent();
            
            // 初始化定时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            
            Loaded += MemoryMonitorWindow_Loaded;
            Closed += MemoryMonitorWindow_Closed;
        }
        
        /// <summary>
        /// 初始化图表
        /// </summary>
        private void InitializeCharts()
        {
            try
            {
                _logger.LogInformation("开始初始化图表...");

                // 检查控件是否存在
                if (MemoryUsagePlot == null)
                {
                    _logger.LogError("MemoryUsagePlot 控件为 null");
                    throw new InvalidOperationException("MemoryUsagePlot 控件未找到");
                }

                if (MemoryDetailPlot == null)
                {
                    _logger.LogError("MemoryDetailPlot 控件为 null");
                    throw new InvalidOperationException("MemoryDetailPlot 控件未找到");
                }

                _logger.LogInformation("图表控件检查通过，开始创建图表模型...");

                // 内存使用率图表
                _memoryUsagePlotModel = new PlotModel
                {
                    Title = "内存使用率 (%)",
                    Background = OxyColors.Transparent,
                    PlotAreaBorderColor = OxyColors.Gray,
                    TextColor = OxyColors.Black
                };
                
                _memoryUsagePlotModel.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "HH:mm:ss",
                    Title = "时间",
                    IntervalType = DateTimeIntervalType.Minutes,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                _memoryUsagePlotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "使用率 (%)",
                    Minimum = 0,
                    Maximum = 100,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                var memoryUsageSeries = new LineSeries
                {
                    Title = "内存使用率",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                _memoryUsageSeries = memoryUsageSeries;
                _memoryUsagePlotModel.Series.Add(_memoryUsageSeries);
                
                _logger.LogInformation("内存使用率图表模型创建完成，设置到控件...");
                MemoryUsagePlot.Model = _memoryUsagePlotModel;
                _logger.LogInformation("内存使用率图表设置完成");
                
                // 内存详细使用情况图表
                _memoryDetailPlotModel = new PlotModel
                {
                    Title = "内存详细使用情况 (GB)",
                    Background = OxyColors.Transparent,
                    PlotAreaBorderColor = OxyColors.Gray,
                    TextColor = OxyColors.Black
                };
                
                _memoryDetailPlotModel.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "HH:mm:ss",
                    Title = "时间",
                    IntervalType = DateTimeIntervalType.Minutes,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                _memoryDetailPlotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "内存 (GB)",
                    Minimum = 0,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                var committedSeries = new LineSeries
                {
                    Title = "提交内存",
                    Color = OxyColors.Green,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                _committedMemorySeries = committedSeries;
                
                var cachedSeries = new LineSeries
                {
                    Title = "缓存内存",
                    Color = OxyColors.Orange,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                _cachedMemorySeries = cachedSeries;
                
                _memoryDetailPlotModel.Series.Add(_committedMemorySeries);
                _memoryDetailPlotModel.Series.Add(_cachedMemorySeries);
                
                _logger.LogInformation("内存详细图表模型创建完成，设置到控件...");
                MemoryDetailPlot.Model = _memoryDetailPlotModel;
                _logger.LogInformation("内存详细图表设置完成");

                _logger.LogInformation("图表初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化图表时发生错误");
                throw;
            }
        }
        
        /// <summary>
        /// 窗口加载事件
        /// </summary>
        private async void MemoryMonitorWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 初始化图表
                InitializeCharts();
                
                // 设置数据上下文
                MemoryModulesDataGrid.ItemsSource = MemoryModules;
                MemoryProcessesDataGrid.ItemsSource = MemoryProcesses;
                
                await UpdateMemoryInfo();
                await LoadMemoryModules();
                _updateTimer.Start();
                _logger?.LogInformation("内存监控窗口已加载并开始更新");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载内存监控窗口时发生错误");
            }
        }
        
        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void MemoryMonitorWindow_Closed(object? sender, EventArgs e)
        {
            _updateTimer?.Stop();
            _logger?.LogInformation("内存监控窗口已关闭");
        }
        
        /// <summary>
        /// 定时器更新事件
        /// </summary>
        private async void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            await UpdateMemoryInfo();
            await UpdateMemoryProcesses();
        }
        
        /// <summary>
        /// 更新内存信息
        /// </summary>
        private async Task UpdateMemoryInfo()
        {
            try
            {
                if (_hardwareService == null) return;
                
                var hardwareData = await _hardwareService.GetHardwareDataAsync();
                var memoryInfo = hardwareData.Memory;
                if (memoryInfo == null) return;
                
                var currentTime = DateTime.Now;
                
                // 更新基本信息
                Dispatcher.Invoke(() =>
                {
                    if (TotalMemoryTextBlock != null)
                        TotalMemoryTextBlock.Text = FormatBytes(memoryInfo.TotalMemory * 1024 * 1024); // 转换为字节
                    if (UsedMemoryTextBlock != null)
                        UsedMemoryTextBlock.Text = FormatBytes(memoryInfo.UsedMemory * 1024 * 1024); // 转换为字节
                    if (AvailableMemoryTextBlock != null)
                        AvailableMemoryTextBlock.Text = FormatBytes(memoryInfo.AvailableMemory * 1024 * 1024); // 转换为字节
                    if (MemorySpeedTextBlock != null)
                        MemorySpeedTextBlock.Text = $"{memoryInfo.Frequency} MHz";
                    if (MemoryTypeTextBlock != null)
                        MemoryTypeTextBlock.Text = memoryInfo.MemoryType ?? "未知";
                    
                    // 更新实时状态
                    var usagePercent = memoryInfo.UsagePercentage;
                    if (MemoryUsageTextBlock != null)
                        MemoryUsageTextBlock.Text = $"{usagePercent:F1}%";
                    if (CommittedMemoryTextBlock != null)
                        CommittedMemoryTextBlock.Text = FormatBytes(memoryInfo.UsedMemory * 1024 * 1024); // 使用已用内存
                    if (CachedMemoryTextBlock != null)
                        CachedMemoryTextBlock.Text = FormatBytes(memoryInfo.CachedMemory * 1024 * 1024); // 转换为字节
                    if (PageFileTextBlock != null)
                        PageFileTextBlock.Text = FormatBytes(memoryInfo.SwapUsed * 1024 * 1024); // 使用交换文件
                    if (KernelMemoryTextBlock != null)
                        KernelMemoryTextBlock.Text = FormatBytes(memoryInfo.BufferedMemory * 1024 * 1024); // 使用缓冲内存
                });
                
                // 添加图表数据点
                var timeValue = DateTimeAxis.ToDouble(currentTime);
                var usagePercent = memoryInfo.UsagePercentage;
                
                _memoryUsageData.Add(new DataPoint(timeValue, usagePercent));
                _committedMemoryData.Add(new DataPoint(timeValue, BytesToGB(memoryInfo.UsedMemory * 1024 * 1024)));
                _cachedMemoryData.Add(new DataPoint(timeValue, BytesToGB(memoryInfo.CachedMemory * 1024 * 1024)));
                
                // 清理过期数据
                var cutoffTime = currentTime.AddMinutes(-_timeRangeMinutes);
                var cutoffValue = DateTimeAxis.ToDouble(cutoffTime);
                
                _memoryUsageData.RemoveAll(p => p.X < cutoffValue);
                _committedMemoryData.RemoveAll(p => p.X < cutoffValue);
                _cachedMemoryData.RemoveAll(p => p.X < cutoffValue);
                
                // 更新图表
                Dispatcher.Invoke(() =>
                {
                    // 检查图表是否已初始化
                    if (_memoryUsageSeries != null && _memoryUsagePlotModel != null)
                    {
                        // 更新内存使用率图表
                        _memoryUsageSeries.Points.Clear();
                        _memoryUsageSeries.Points.AddRange(_memoryUsageData);
                        _memoryUsagePlotModel.InvalidatePlot(true);
                    }
                    
                    if (_committedMemorySeries != null && _cachedMemorySeries != null && _memoryDetailPlotModel != null)
                    {
                        // 更新详细内存图表
                        _committedMemorySeries.Points.Clear();
                        _committedMemorySeries.Points.AddRange(_committedMemoryData);
                        
                        _cachedMemorySeries.Points.Clear();
                        _cachedMemorySeries.Points.AddRange(_cachedMemoryData);
                        
                        _memoryDetailPlotModel.InvalidatePlot(true);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新内存信息时发生错误");
            }
        }
        
        /// <summary>
        /// 加载内存模块信息
        /// </summary>
        private async Task LoadMemoryModules()
        {
            try
            {
                await Task.Run(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        MemoryModules.Clear();
                        
                        // 模拟内存模块数据
                        var modules = new[]
                        {
                            new MemoryModuleInfo { Slot = "DIMM_A1", Capacity = "8 GB", Speed = "3200 MHz", Type = "DDR4", Manufacturer = "Corsair", PartNumber = "CMK16GX4M2B3200C16" },
                            new MemoryModuleInfo { Slot = "DIMM_A2", Capacity = "8 GB", Speed = "3200 MHz", Type = "DDR4", Manufacturer = "Corsair", PartNumber = "CMK16GX4M2B3200C16" },
                            new MemoryModuleInfo { Slot = "DIMM_B1", Capacity = "空", Speed = "-", Type = "-", Manufacturer = "-", PartNumber = "-" },
                            new MemoryModuleInfo { Slot = "DIMM_B2", Capacity = "空", Speed = "-", Type = "-", Manufacturer = "-", PartNumber = "-" }
                        };
                        
                        foreach (var module in modules)
                        {
                            MemoryModules.Add(module);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载内存模块信息时发生错误");
            }
        }
        
        /// <summary>
        /// 更新内存进程信息
        /// </summary>
        private async Task UpdateMemoryProcesses()
        {
            try
            {
                await Task.Run(() =>
                {
                    var processes = Process.GetProcesses()
                        .Where(p => p.WorkingSet64 > 0)
                        .OrderByDescending(p => p.WorkingSet64)
                        .Take(10)
                        .Select(p => new MemoryProcessInfo
                        {
                            ProcessName = p.ProcessName,
                            ProcessId = p.Id,
                            MemoryUsage = FormatBytes(p.WorkingSet64),
                            WorkingSet = FormatBytes(p.WorkingSet64),
                            PrivateBytes = FormatBytes(p.PrivateMemorySize64)
                        })
                        .ToList();
                    
                    Dispatcher.Invoke(() =>
                    {
                        MemoryProcesses.Clear();
                        foreach (var process in processes)
                        {
                            MemoryProcesses.Add(process);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新内存进程信息时发生错误");
            }
        }
        
        /// <summary>
        /// 格式化字节数
        /// </summary>
        private string FormatBytes(long bytes)
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
        /// 字节转GB
        /// </summary>
        private double BytesToGB(long bytes)
        {
            return bytes / (1024.0 * 1024.0 * 1024.0);
        }
        
        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private async void RefreshButton_Click(object? sender, RoutedEventArgs e)
        {
            await UpdateMemoryInfo();
            await UpdateMemoryProcesses();
        }
        
        /// <summary>
        /// 时间范围选择改变事件
        /// </summary>
        private void TimeRangeComboBox_SelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
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
                
                // 检查图表是否已初始化
                if (_memoryUsageSeries == null || _committedMemorySeries == null || _cachedMemorySeries == null ||
                    _memoryUsagePlotModel == null || _memoryDetailPlotModel == null)
                {
                    _logger?.LogWarning("图表尚未初始化，跳过时间范围更新");
                    return;
                }
                
                // 清理超出时间范围的数据
                var cutoffTime = DateTime.Now.AddMinutes(-_timeRangeMinutes);
                var cutoffValue = DateTimeAxis.ToDouble(cutoffTime);
                
                _memoryUsageData.RemoveAll(p => p.X < cutoffValue);
                _committedMemoryData.RemoveAll(p => p.X < cutoffValue);
                _cachedMemoryData.RemoveAll(p => p.X < cutoffValue);
                
                // 更新图表
                _memoryUsageSeries.Points.Clear();
                _memoryUsageSeries.Points.AddRange(_memoryUsageData);
                _memoryUsagePlotModel.InvalidatePlot(true);
                
                _committedMemorySeries.Points.Clear();
                _committedMemorySeries.Points.AddRange(_committedMemoryData);
                
                _cachedMemorySeries.Points.Clear();
                _cachedMemorySeries.Points.AddRange(_cachedMemoryData);
                
                _memoryDetailPlotModel.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "时间范围选择改变时发生错误");
            }
        }
    }
    
    /// <summary>
    /// 内存模块信息
    /// </summary>
    public class MemoryModuleInfo
    {
        public string Slot { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string Speed { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 内存进程信息
    /// </summary>
    public class MemoryProcessInfo
    {
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string MemoryUsage { get; set; } = string.Empty;
        public string WorkingSet { get; set; } = string.Empty;
        public string PrivateBytes { get; set; } = string.Empty;
    }
}