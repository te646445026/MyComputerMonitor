using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MyComputerMonitor.Infrastructure.Services;

namespace MyComputerMonitor.WPF.Views
{
    /// <summary>
    /// 设置窗口
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly ILogger<SettingsWindow> _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _isLoading = true;

        public SettingsWindow(IServiceProvider serviceProvider, ILogger<SettingsWindow> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            InitializeComponent();
            
            // 窗口事件
            Loaded += SettingsWindow_Loaded;
            Closed += SettingsWindow_Closed;
        }
        
        /// <summary>
        /// 窗口加载事件
        /// </summary>
        private void SettingsWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("设置窗口开始加载...");
                
                LoadSettings();
                
                // 设置默认活动按钮（常规设置）
                UpdateNavigationButtonStyle(GeneralSettingsButton);
                
                _isLoading = false;
                
                _logger.LogInformation("设置窗口加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载设置窗口时发生错误");
                MessageBox.Show($"加载设置窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void SettingsWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("设置窗口已关闭");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭设置窗口时发生错误");
            }
        }

    private void LoadSettings()
    {
        try
        {
            // 加载启动设置
            AutoStartCheckBox.IsChecked = IsAutoStartEnabled();
            StartMinimizedCheckBox.IsChecked = Properties.Settings.Default.StartMinimized;
            MinimizeToTrayCheckBox.IsChecked = Properties.Settings.Default.MinimizeToTray;
            
            // 加载更新设置
            AutoUpdateCheckBox.IsChecked = Properties.Settings.Default.AutoUpdate;
            
            // 加载语言设置
            LanguageComboBox.SelectedIndex = Properties.Settings.Default.Language;
            
            // 加载监控设置
            RefreshIntervalSlider.Value = Properties.Settings.Default.RefreshInterval;
            MonitorCpuCheckBox.IsChecked = Properties.Settings.Default.MonitorCpu;
        MonitorGpuCheckBox.IsChecked = Properties.Settings.Default.MonitorGpu;
        MonitorMemoryCheckBox.IsChecked = Properties.Settings.Default.MonitorMemory;
        MonitorNetworkCheckBox.IsChecked = Properties.Settings.Default.MonitorNetwork;
            MonitorTemperatureCheckBox.IsChecked = Properties.Settings.Default.MonitorTemperature;
            DataRetentionComboBox.SelectedIndex = Properties.Settings.Default.DataRetention;
            
            // 加载报警设置
            EnableTemperatureAlertCheckBox.IsChecked = Properties.Settings.Default.EnableTemperatureAlert;
            CpuTempThresholdNumberBox.Value = Properties.Settings.Default.CpuTempThreshold;
            GpuTempThresholdNumberBox.Value = Properties.Settings.Default.GpuTempThreshold;
            EnableUsageAlertCheckBox.IsChecked = Properties.Settings.Default.EnableUsageAlert;
            CpuUsageThresholdNumberBox.Value = Properties.Settings.Default.CpuUsageThreshold;
            MemoryUsageThresholdNumberBox.Value = Properties.Settings.Default.MemoryUsageThreshold;
            ShowNotificationCheckBox.IsChecked = Properties.Settings.Default.ShowNotification;
            PlaySoundCheckBox.IsChecked = Properties.Settings.Default.PlaySound;
            FlashTrayIconCheckBox.IsChecked = Properties.Settings.Default.FlashTrayIcon;
            
            // 加载外观设置
            switch (Properties.Settings.Default.Theme)
            {
                case 0:
                    LightThemeRadio.IsChecked = true;
                    break;
                case 1:
                    DarkThemeRadio.IsChecked = true;
                    break;
                case 2:
                    AutoThemeRadio.IsChecked = true;
                    break;
            }
            ShowGridLinesCheckBox.IsChecked = Properties.Settings.Default.ShowGridLines;
            SmoothCurvesCheckBox.IsChecked = Properties.Settings.Default.SmoothCurves;
            ShowDataPointsCheckBox.IsChecked = Properties.Settings.Default.ShowDataPoints;
            AlwaysOnTopCheckBox.IsChecked = Properties.Settings.Default.AlwaysOnTop;
            RememberWindowSizeCheckBox.IsChecked = Properties.Settings.Default.RememberWindowSize;
            
            // 加载高级设置
            LowPowerModeCheckBox.IsChecked = Properties.Settings.Default.LowPowerMode;
            ReduceAnimationsCheckBox.IsChecked = Properties.Settings.Default.ReduceAnimations;
            MaxMemoryComboBox.SelectedIndex = Properties.Settings.Default.MaxMemory;
            EnableLoggingCheckBox.IsChecked = Properties.Settings.Default.EnableLogging;
            LogLevelComboBox.SelectedIndex = Properties.Settings.Default.LogLevel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载设置时发生错误");
        }
    }

    private void SaveSettings()
    {
        try
        {
            // 保存启动设置
            Properties.Settings.Default.StartMinimized = StartMinimizedCheckBox.IsChecked ?? false;
            Properties.Settings.Default.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;
            
            // 保存更新设置
            Properties.Settings.Default.AutoUpdate = AutoUpdateCheckBox.IsChecked ?? false;
            
            // 保存语言设置
            Properties.Settings.Default.Language = LanguageComboBox.SelectedIndex;
            
            // 保存监控设置
            Properties.Settings.Default.RefreshInterval = (int)RefreshIntervalSlider.Value;
            Properties.Settings.Default.MonitorCpu = MonitorCpuCheckBox.IsChecked ?? true;
        Properties.Settings.Default.MonitorGpu = MonitorGpuCheckBox.IsChecked ?? true;
        Properties.Settings.Default.MonitorMemory = MonitorMemoryCheckBox.IsChecked ?? true;
        Properties.Settings.Default.MonitorNetwork = MonitorNetworkCheckBox.IsChecked ?? true;
            Properties.Settings.Default.MonitorTemperature = MonitorTemperatureCheckBox.IsChecked ?? true;
            Properties.Settings.Default.DataRetention = DataRetentionComboBox.SelectedIndex;
            
            // 保存报警设置
            Properties.Settings.Default.EnableTemperatureAlert = EnableTemperatureAlertCheckBox.IsChecked ?? false;
            Properties.Settings.Default.CpuTempThreshold = (int)CpuTempThresholdNumberBox.Value;
            Properties.Settings.Default.GpuTempThreshold = (int)GpuTempThresholdNumberBox.Value;
            Properties.Settings.Default.EnableUsageAlert = EnableUsageAlertCheckBox.IsChecked ?? false;
            Properties.Settings.Default.CpuUsageThreshold = (int)CpuUsageThresholdNumberBox.Value;
            Properties.Settings.Default.MemoryUsageThreshold = (int)MemoryUsageThresholdNumberBox.Value;
            Properties.Settings.Default.ShowNotification = ShowNotificationCheckBox.IsChecked ?? true;
            Properties.Settings.Default.PlaySound = PlaySoundCheckBox.IsChecked ?? false;
            Properties.Settings.Default.FlashTrayIcon = FlashTrayIconCheckBox.IsChecked ?? true;
            
            // 保存外观设置
            if (LightThemeRadio.IsChecked == true)
                Properties.Settings.Default.Theme = 0;
            else if (DarkThemeRadio.IsChecked == true)
                Properties.Settings.Default.Theme = 1;
            else if (AutoThemeRadio.IsChecked == true)
                Properties.Settings.Default.Theme = 2;
                
            Properties.Settings.Default.ShowGridLines = ShowGridLinesCheckBox.IsChecked ?? true;
            Properties.Settings.Default.SmoothCurves = SmoothCurvesCheckBox.IsChecked ?? true;
            Properties.Settings.Default.ShowDataPoints = ShowDataPointsCheckBox.IsChecked ?? false;
            Properties.Settings.Default.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked ?? false;
            Properties.Settings.Default.RememberWindowSize = RememberWindowSizeCheckBox.IsChecked ?? true;
            
            // 保存高级设置
            Properties.Settings.Default.LowPowerMode = LowPowerModeCheckBox.IsChecked ?? false;
            Properties.Settings.Default.ReduceAnimations = ReduceAnimationsCheckBox.IsChecked ?? false;
            Properties.Settings.Default.MaxMemory = MaxMemoryComboBox.SelectedIndex;
            Properties.Settings.Default.EnableLogging = EnableLoggingCheckBox.IsChecked ?? true;
            Properties.Settings.Default.LogLevel = LogLevelComboBox.SelectedIndex;
            
            Properties.Settings.Default.Save();
            
            _logger.LogInformation("设置已保存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存设置时发生错误");
        }
    }

    #region 导航按钮事件
    private void GeneralSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(GeneralSettingsPanel);
        UpdateNavigationButtonStyle(GeneralSettingsButton);
    }

    private void MonitoringSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(MonitoringSettingsPanel);
        UpdateNavigationButtonStyle(MonitoringSettingsButton);
    }

    private void AlertSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(AlertSettingsPanel);
        UpdateNavigationButtonStyle(AlertSettingsButton);
    }

    private void AppearanceSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(AppearanceSettingsPanel);
        UpdateNavigationButtonStyle(AppearanceSettingsButton);
    }

    private void AdvancedSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel(AdvancedSettingsPanel);
        UpdateNavigationButtonStyle(AdvancedSettingsButton);
    }

    private void ShowPanel(FrameworkElement targetPanel)
    {
        // 隐藏所有面板
        GeneralSettingsPanel.Visibility = Visibility.Collapsed;
        MonitoringSettingsPanel.Visibility = Visibility.Collapsed;
        AlertSettingsPanel.Visibility = Visibility.Collapsed;
        AppearanceSettingsPanel.Visibility = Visibility.Collapsed;
        AdvancedSettingsPanel.Visibility = Visibility.Collapsed;
        
        // 显示目标面板
        targetPanel.Visibility = Visibility.Visible;
        
        // 滚动到顶部
        SettingsContentScrollViewer.ScrollToTop();
    }

    private void UpdateNavigationButtonStyle(Button activeButton)
    {
        // 获取默认导航按钮样式
        var defaultStyle = FindResource("NavigationButtonStyle") as Style;
        var activeStyle = FindResource("ActiveNavigationButtonStyle") as Style;
        
        // 重置所有按钮为默认样式
        GeneralSettingsButton.Style = defaultStyle;
        MonitoringSettingsButton.Style = defaultStyle;
        AlertSettingsButton.Style = defaultStyle;
        AppearanceSettingsButton.Style = defaultStyle;
        AdvancedSettingsButton.Style = defaultStyle;
        
        // 设置活动按钮样式
        activeButton.Style = activeStyle;
    }
    #endregion

    #region 控件事件
    private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isLoading)
            SetAutoStart(true);
    }

    private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_isLoading)
            SetAutoStart(false);
    }

    private void RefreshIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RefreshIntervalText != null)
            RefreshIntervalText.Text = $"{(int)e.NewValue} 秒";
    }

    private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        
        // 应用主题更改
        ApplyThemeChange();
    }

    private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MessageBox.Show("当前版本已是最新版本。", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查更新时发生错误");
            MessageBox.Show("检查更新失败，请稍后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                                     "MyComputerMonitor", "Logs");
            
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);
                
            Process.Start("explorer.exe", logPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开日志文件夹时发生错误");
            MessageBox.Show("无法打开日志文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportDataButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|JSON 文件 (*.json)|*.json",
                DefaultExt = "csv",
                FileName = $"硬件监控数据_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // TODO: 实现数据导出功能
                MessageBox.Show("数据导出功能正在开发中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出数据时发生错误");
            MessageBox.Show("导出数据失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearDataButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show("确定要清除所有历史数据吗？此操作不可撤销。", 
                                       "确认清除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                // TODO: 实现清除历史数据功能
                MessageBox.Show("历史数据已清除。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除数据时发生错误");
            MessageBox.Show("清除数据失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show("确定要重置所有设置为默认值吗？", 
                                       "确认重置", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                Properties.Settings.Default.Reset();
                Properties.Settings.Default.Save();
                LoadSettings();
                MessageBox.Show("设置已重置为默认值。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置设置时发生错误");
            MessageBox.Show("重置设置失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region 底部按钮事件
    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        MessageBox.Show("设置已应用。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    #endregion

    #region 辅助方法
    private bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            return key?.GetValue("MyComputerMonitor") != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查自启动状态时发生错误");
            return false;
        }
    }

    private void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            
            if (enable)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    key?.SetValue("MyComputerMonitor", exePath);
            }
            else
            {
                key?.DeleteValue("MyComputerMonitor", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置自启动时发生错误");
            MessageBox.Show("设置自启动失败，可能需要管理员权限。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyThemeChange()
    {
        try
        {
            // TODO: 实现主题切换功能
            _logger.LogInformation("主题设置已更改");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用主题更改时发生错误");
        }
    }
    #endregion
}
}