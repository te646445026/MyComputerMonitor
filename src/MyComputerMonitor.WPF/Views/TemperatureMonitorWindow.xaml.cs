using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.WPF.ViewModels;

namespace MyComputerMonitor.WPF.Views
{
    /// <summary>
    /// 温度监控窗口
    /// </summary>
    public partial class TemperatureMonitorWindow : Window
    {
        private readonly TemperatureMonitorViewModel _viewModel;
        private readonly ILogger<TemperatureMonitorWindow> _logger;

        public TemperatureMonitorWindow(TemperatureMonitorViewModel viewModel, ILogger<TemperatureMonitorWindow> logger)
        {
            _viewModel = viewModel;
            _logger = logger;
            
            InitializeComponent();
            DataContext = _viewModel;
            
            // 窗口事件
            Loaded += TemperatureMonitorWindow_Loaded;
            Closed += TemperatureMonitorWindow_Closed;
        }
        
        /// <summary>
        /// 窗口加载事件
        /// </summary>
        private void TemperatureMonitorWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("温度监控窗口开始加载...");
                
                // 更新按钮文本
                UpdateMonitoringButtonText();
                
                _logger.LogInformation("温度监控窗口加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载温度监控窗口时发生错误");
                MessageBox.Show($"加载温度监控窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void TemperatureMonitorWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                // 释放视图模型资源
                _viewModel?.Dispose();
                _logger.LogInformation("温度监控窗口已关闭");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭温度监控窗口时发生错误");
            }
        }

        /// <summary>
        /// 监控切换按钮点击事件
        /// </summary>
        private void MonitoringToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.IsMonitoring)
                {
                    _viewModel.StopMonitoringCommand.Execute(null);
                }
                else
                {
                    _viewModel.StartMonitoringCommand.Execute(null);
                }
                UpdateMonitoringButtonText();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换监控状态时发生错误");
                MessageBox.Show($"切换监控状态时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新监控按钮文本
        /// </summary>
        private void UpdateMonitoringButtonText()
        {
            try
            {
                if (MonitoringToggleButton != null)
                {
                    MonitoringToggleButton.Content = _viewModel.IsMonitoring ? "停止监控" : "开始监控";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新监控按钮文本时发生错误");
            }
        }
    }
}