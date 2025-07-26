using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// 网络监控窗口
    /// </summary>
    public partial class NetworkMonitorWindow : Window
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly ILogger<NetworkMonitorWindow> _logger;
        private readonly DispatcherTimer _updateTimer;
        
        // 图表数据
        private readonly Queue<DataPoint> _downloadSpeedHistory = new();
        private readonly Queue<DataPoint> _uploadSpeedHistory = new();
        private readonly Queue<DataPoint> _networkUsageHistory = new();
        
        private PlotModel _networkSpeedChart = null!;
        private PlotModel _networkUsageChart = null!;
        private LineSeries _downloadSpeedSeries = null!;
        private LineSeries _uploadSpeedSeries = null!;
        private LineSeries _networkUsageSeries = null!;
        
        // 网络适配器数据
        public ObservableCollection<NetworkInfo> NetworkAdapters { get; } = new();
        
        // 时间范围设置
        private int _timeRangeMinutes = 30;
        
        // 最大速度记录（用于进度条缩放）
        private double _maxDownloadSpeed = 1.0; // MB/s
        private double _maxUploadSpeed = 1.0; // MB/s
        
        // 累计流量统计
        private double _totalDownloadBytes = 0;
        private double _totalUploadBytes = 0;
        
        // 下拉框更新控制
        private bool _isUpdatingComboBox = false;
        private List<string> _lastAdapterNames = new();
        
        public NetworkMonitorWindow(IHardwareMonitorService hardwareMonitorService, ILogger<NetworkMonitorWindow> logger)
        {
            _hardwareMonitorService = hardwareMonitorService;
            _logger = logger;
            
            InitializeComponent();
            
            // 设置数据绑定
            NetworkAdaptersItemsControl.ItemsSource = NetworkAdapters;
            
            // 创建更新定时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            
            // 窗口事件
            Loaded += NetworkMonitorWindow_Loaded;
            Closed += NetworkMonitorWindow_Closed;
        }
        
        /// <summary>
        /// 初始化图表
        /// </summary>
        private void InitializeCharts()
        {
            try
            {
                _logger.LogInformation("开始初始化网络图表...");

                // 初始化网络速度图表
                _networkSpeedChart = new PlotModel
                {
                    Title = "网络速度历史",
                    Background = OxyColors.Transparent,
                    PlotAreaBorderColor = OxyColors.Gray,
                    TextColor = OxyColors.Black
                };
                
                _networkSpeedChart.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "HH:mm:ss",
                    Title = "时间",
                    IntervalType = DateTimeIntervalType.Seconds,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                _networkSpeedChart.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "速度 (MB/s)",
                    Minimum = 0,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                _downloadSpeedSeries = new LineSeries
                {
                    Title = "下载速度",
                    Color = OxyColors.Green,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                
                _uploadSpeedSeries = new LineSeries
                {
                    Title = "上传速度",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                
                _networkSpeedChart.Series.Add(_downloadSpeedSeries);
                _networkSpeedChart.Series.Add(_uploadSpeedSeries);
                NetworkSpeedChart.Model = _networkSpeedChart;
                
                // 初始化网络使用率图表
                _networkUsageChart = new PlotModel
                {
                    Title = "网络使用率历史",
                    Background = OxyColors.Transparent,
                    PlotAreaBorderColor = OxyColors.Gray,
                    TextColor = OxyColors.Black
                };
                
                _networkUsageChart.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "HH:mm:ss",
                    Title = "时间",
                    IntervalType = DateTimeIntervalType.Seconds,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                _networkUsageChart.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "使用率 (%)",
                    Minimum = 0,
                    Maximum = 100,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                _networkUsageSeries = new LineSeries
                {
                    Title = "网络使用率",
                    Color = OxyColors.Orange,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };
                
                _networkUsageChart.Series.Add(_networkUsageSeries);
                NetworkUsageChart.Model = _networkUsageChart;
                
                _logger.LogInformation("网络图表初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化网络图表时发生错误");
                throw;
            }
        }
        
        /// <summary>
        /// 窗口加载事件
        /// </summary>
        private async void NetworkMonitorWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("网络监控窗口开始加载...");
                
                InitializeCharts();
                await UpdateDataAsync();
                _updateTimer.Start();
                
                // 加载保存的网卡选择
                LoadSelectedNetworkAdapter();
                
                _logger.LogInformation("网络监控窗口加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载网络监控窗口时发生错误");
                MessageBox.Show($"加载网络监控窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void NetworkMonitorWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                _updateTimer?.Stop();
                _logger.LogInformation("网络监控窗口已关闭");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭网络监控窗口时发生错误");
            }
        }
        
        /// <summary>
        /// 定时器更新事件
        /// </summary>
        private async void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            await UpdateDataAsync(false); // 定时器更新时不更新下拉框，避免跳动
        }
        
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="updateComboBox">是否更新下拉框</param>
        private async Task UpdateDataAsync(bool updateComboBox = true)
        {
            try
            {
                var hardwareData = await _hardwareMonitorService.GetHardwareDataAsync();
                var networkAdapters = hardwareData.NetworkAdapters;
                
                if (networkAdapters.Any())
                {
                    // 更新网络适配器集合
                    UpdateNetworkAdaptersCollection(networkAdapters);
                    
                    // 只在需要时更新下拉框选项
                    if (updateComboBox)
                    {
                        UpdateNetworkAdapterComboBox(networkAdapters);
                    }
                    
                    // 获取选中的网络适配器
                    var selectedAdapter = GetSelectedNetworkAdapter(networkAdapters);
                    
                    if (selectedAdapter != null)
                    {
                        // 更新基本信息
                        UpdateBasicInfo(selectedAdapter);
                        
                        // 更新实时状态
                        UpdateRealtimeStatus(selectedAdapter);
                        
                        // 更新图表数据
                        UpdateChartData(selectedAdapter);
                        
                        // 更新进度条
                        UpdateProgressBars(selectedAdapter);
                        
                        // 更新累计流量
                        UpdateTotalTraffic(selectedAdapter);
                    }
                }
                else
                {
                    // 没有网络适配器时显示默认信息
                    UpdateDefaultInfo();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新网络数据时发生错误");
            }
        }
        
        /// <summary>
        /// 更新网络适配器集合
        /// </summary>
        private void UpdateNetworkAdaptersCollection(List<NetworkInfo> networkAdapters)
        {
            // 清空现有数据
            NetworkAdapters.Clear();
            
            // 添加新数据
            foreach (var adapter in networkAdapters)
            {
                NetworkAdapters.Add(adapter);
            }
        }
        
        /// <summary>
        /// 更新基本信息
        /// </summary>
        private void UpdateBasicInfo(NetworkInfo adapter)
        {
            AdapterNameTextBlock.Text = adapter.Name;
            NetworkTypeTextBlock.Text = adapter.NetworkType;
            MacAddressTextBlock.Text = adapter.MacAddress;
            IpAddressTextBlock.Text = adapter.IpAddress;
            ConnectionStatusTextBlock.Text = adapter.IsConnected ? "已连接" : "未连接";
        }
        
        /// <summary>
        /// 更新实时状态
        /// </summary>
        private void UpdateRealtimeStatus(NetworkInfo adapter)
        {
            NetworkUsageTextBlock.Text = $"{adapter.UsagePercentage:F1}%";
            DownloadSpeedTextBlock.Text = $"{adapter.DownloadSpeed:F2} MB/s";
            UploadSpeedTextBlock.Text = $"{adapter.UploadSpeed:F2} MB/s";
        }
        
        /// <summary>
        /// 更新图表数据
        /// </summary>
        private void UpdateChartData(NetworkInfo adapter)
        {
            var now = DateTime.Now;
            var timeStamp = DateTimeAxis.ToDouble(now);
            
            // 添加新数据点
            _downloadSpeedHistory.Enqueue(new DataPoint(timeStamp, adapter.DownloadSpeed));
            _uploadSpeedHistory.Enqueue(new DataPoint(timeStamp, adapter.UploadSpeed));
            _networkUsageHistory.Enqueue(new DataPoint(timeStamp, adapter.UsagePercentage));
            
            // 移除过期数据
            var cutoffTime = DateTimeAxis.ToDouble(now.AddMinutes(-_timeRangeMinutes));
            RemoveOldDataPoints(_downloadSpeedHistory, cutoffTime);
            RemoveOldDataPoints(_uploadSpeedHistory, cutoffTime);
            RemoveOldDataPoints(_networkUsageHistory, cutoffTime);
            
            // 更新图表系列
            _downloadSpeedSeries.Points.Clear();
            _downloadSpeedSeries.Points.AddRange(_downloadSpeedHistory);
            
            _uploadSpeedSeries.Points.Clear();
            _uploadSpeedSeries.Points.AddRange(_uploadSpeedHistory);
            
            _networkUsageSeries.Points.Clear();
            _networkUsageSeries.Points.AddRange(_networkUsageHistory);
            
            // 刷新图表
            _networkSpeedChart.InvalidatePlot(true);
            _networkUsageChart.InvalidatePlot(true);
        }
        
        /// <summary>
        /// 更新进度条
        /// </summary>
        private void UpdateProgressBars(NetworkInfo adapter)
        {
            // 动态调整最大速度范围
            if (adapter.DownloadSpeed > _maxDownloadSpeed)
                _maxDownloadSpeed = Math.Max(adapter.DownloadSpeed * 1.2, 1.0);
            
            if (adapter.UploadSpeed > _maxUploadSpeed)
                _maxUploadSpeed = Math.Max(adapter.UploadSpeed * 1.2, 1.0);
            
            // 更新进度条
            DownloadProgressBar.Maximum = _maxDownloadSpeed;
            DownloadProgressBar.Value = adapter.DownloadSpeed;
            DownloadProgressText.Text = $"{adapter.DownloadSpeed:F2} MB/s";
            
            UploadProgressBar.Maximum = _maxUploadSpeed;
            UploadProgressBar.Value = adapter.UploadSpeed;
            UploadProgressText.Text = $"{adapter.UploadSpeed:F2} MB/s";
        }
        
        /// <summary>
        /// 更新累计流量
        /// </summary>
        private void UpdateTotalTraffic(NetworkInfo adapter)
        {
            // 累计流量（简化计算，实际应该基于时间间隔）
            _totalDownloadBytes += adapter.DownloadSpeed * 1024 * 1024; // 转换为字节
            _totalUploadBytes += adapter.UploadSpeed * 1024 * 1024;
            
            TotalDownloadTextBlock.Text = $"{_totalDownloadBytes / (1024 * 1024 * 1024):F2} GB";
            TotalUploadTextBlock.Text = $"{_totalUploadBytes / (1024 * 1024 * 1024):F2} GB";
        }
        
        /// <summary>
        /// 更新默认信息
        /// </summary>
        private void UpdateDefaultInfo()
        {
            AdapterNameTextBlock.Text = "未检测到网络适配器";
            NetworkTypeTextBlock.Text = "N/A";
            MacAddressTextBlock.Text = "N/A";
            IpAddressTextBlock.Text = "N/A";
            ConnectionStatusTextBlock.Text = "未连接";
            
            NetworkUsageTextBlock.Text = "0%";
            DownloadSpeedTextBlock.Text = "0 MB/s";
            UploadSpeedTextBlock.Text = "0 MB/s";
            TotalDownloadTextBlock.Text = "0 GB";
            TotalUploadTextBlock.Text = "0 GB";
            
            DownloadProgressBar.Value = 0;
            UploadProgressBar.Value = 0;
            DownloadProgressText.Text = "0 MB/s";
            UploadProgressText.Text = "0 MB/s";
        }
        
        /// <summary>
        /// 移除过期数据点
        /// </summary>
        private static void RemoveOldDataPoints(Queue<DataPoint> dataPoints, double cutoffTime)
        {
            while (dataPoints.Count > 0 && dataPoints.Peek().X < cutoffTime)
            {
                dataPoints.Dequeue();
            }
        }
        
        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await UpdateDataAsync(true); // 手动刷新时更新下拉框
                _logger.LogInformation("手动刷新网络数据完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动刷新网络数据时发生错误");
                MessageBox.Show($"刷新数据时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 时间范围选择变更事件
        /// </summary>
        private void TimeRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TimeRangeComboBox.SelectedIndex >= 0)
            {
                _timeRangeMinutes = TimeRangeComboBox.SelectedIndex switch
                {
                    0 => 5,
                    1 => 15,
                    2 => 30,
                    3 => 60,
                    4 => 120,
                    _ => 30
                };
                
                _logger.LogInformation($"时间范围已更改为 {_timeRangeMinutes} 分钟");
            }
        }
        
        /// <summary>
        /// 网络适配器选择变更事件
        /// </summary>
        private void NetworkAdapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // 如果正在更新下拉框，忽略此事件
                if (_isUpdatingComboBox)
                    return;
                
                if (NetworkAdapterComboBox.SelectedItem is NetworkInfo selectedAdapter)
                {
                    // 保存用户选择
                    Properties.Settings.Default.SelectedNetworkAdapter = selectedAdapter.Name;
                    Properties.Settings.Default.Save();
                    
                    _logger.LogInformation($"网络适配器已切换到: {selectedAdapter.Name}");
                    
                    // 立即更新显示（但不更新下拉框）
                    Task.Run(async () => await UpdateDataAsync(false));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换网络适配器时发生错误");
            }
        }
        
        /// <summary>
        /// 更新网络适配器下拉框
        /// </summary>
        private void UpdateNetworkAdapterComboBox(List<NetworkInfo> networkAdapters)
        {
            try
            {
                // 检查网卡列表是否发生变化
                var currentAdapterNames = networkAdapters.Select(a => a.Name).ToList();
                if (_lastAdapterNames.SequenceEqual(currentAdapterNames))
                {
                    // 网卡列表没有变化，不需要更新下拉框
                    return;
                }
                
                // 设置更新标志，防止触发选择变更事件
                _isUpdatingComboBox = true;
                
                try
                {
                    // 保存当前选择
                    var currentSelection = NetworkAdapterComboBox.SelectedItem as NetworkInfo;
                    
                    // 清空并重新填充
                    NetworkAdapterComboBox.Items.Clear();
                    foreach (var adapter in networkAdapters)
                    {
                        NetworkAdapterComboBox.Items.Add(adapter);
                    }
                    
                    // 首先尝试恢复当前选择
                    if (currentSelection != null)
                    {
                        var matchingAdapter = networkAdapters.FirstOrDefault(a => a.Name == currentSelection.Name);
                        if (matchingAdapter != null)
                        {
                            NetworkAdapterComboBox.SelectedItem = matchingAdapter;
                            return;
                        }
                    }
                    
                    // 然后尝试加载保存的选择
                    var savedAdapterName = Properties.Settings.Default.SelectedNetworkAdapter;
                    if (!string.IsNullOrEmpty(savedAdapterName))
                    {
                        var savedAdapter = networkAdapters.FirstOrDefault(a => a.Name == savedAdapterName);
                        if (savedAdapter != null)
                        {
                            NetworkAdapterComboBox.SelectedItem = savedAdapter;
                            return;
                        }
                    }
                    
                    // 最后选择第一个连接的适配器作为默认选择
                    var defaultAdapter = networkAdapters.FirstOrDefault(n => n.IsConnected) ?? networkAdapters.FirstOrDefault();
                    if (defaultAdapter != null)
                    {
                        NetworkAdapterComboBox.SelectedItem = defaultAdapter;
                    }
                    
                    // 更新缓存的网卡列表
                    _lastAdapterNames = currentAdapterNames;
                }
                finally
                {
                    // 重置更新标志
                    _isUpdatingComboBox = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新网络适配器下拉框时发生错误");
                _isUpdatingComboBox = false;
            }
        }
        
        /// <summary>
        /// 获取选中的网络适配器
        /// </summary>
        private NetworkInfo? GetSelectedNetworkAdapter(List<NetworkInfo> networkAdapters)
        {
            try
            {
                // 如果下拉框有选择，返回选中的适配器
                if (NetworkAdapterComboBox.SelectedItem is NetworkInfo selectedAdapter)
                {
                    return networkAdapters.FirstOrDefault(a => a.Name == selectedAdapter.Name);
                }
                
                // 否则返回第一个连接的适配器
                return networkAdapters.FirstOrDefault(n => n.IsConnected) ?? networkAdapters.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取选中的网络适配器时发生错误");
                return networkAdapters.FirstOrDefault();
            }
        }
        
        /// <summary>
        /// 加载保存的网络适配器选择
        /// </summary>
        private void LoadSelectedNetworkAdapter()
        {
            try
            {
                var savedAdapterName = Properties.Settings.Default.SelectedNetworkAdapter;
                if (!string.IsNullOrEmpty(savedAdapterName))
                {
                    // 在下次数据更新时会自动选择保存的适配器
                    _logger.LogInformation($"已加载保存的网络适配器选择: {savedAdapterName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载保存的网络适配器选择时发生错误");
            }
        }
    }
}