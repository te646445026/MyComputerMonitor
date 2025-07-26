using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.WPF.ViewModels;

namespace MyComputerMonitor.WPF.Views
{
    /// <summary>
    /// 托盘弹出窗口
    /// </summary>
    public partial class TrayPopupWindow : Window
    {
        private readonly ILogger<TrayPopupWindow> _logger;
        private readonly TrayPopupViewModel _viewModel;

        public TrayPopupWindow(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<TrayPopupWindow>>();
            _viewModel = serviceProvider.GetRequiredService<TrayPopupViewModel>();
            
            InitializeComponent();
            DataContext = _viewModel;
            
            // 设置窗口属性
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ShowInTaskbar = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            
            // 窗口事件
            Loaded += TrayPopupWindow_Loaded;
            Closed += TrayPopupWindow_Closed;
            Deactivated += TrayPopupWindow_Deactivated;
            MouseLeave += TrayPopupWindow_MouseLeave;
        }
        
        /// <summary>
        /// 窗口加载事件
        /// </summary>
        private async void TrayPopupWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("托盘弹出窗口开始加载...");
                
                // 设置窗口位置（右下角）
                SetWindowPosition();
                
                // 开始更新硬件信息
                await _viewModel.StartUpdatingAsync();
                
                _logger.LogInformation("托盘弹出窗口加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载托盘弹出窗口时发生错误");
                MessageBox.Show($"加载托盘弹出窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void TrayPopupWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                _viewModel?.StopUpdating();
                _logger.LogInformation("托盘弹出窗口已关闭");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭托盘弹出窗口时发生错误");
            }
        }

        /// <summary>
        /// 窗口失去焦点事件
        /// </summary>
        private void TrayPopupWindow_Deactivated(object? sender, EventArgs e)
        {
            CloseWindow();
        }

        /// <summary>
        /// 鼠标离开事件
        /// </summary>
        private void TrayPopupWindow_MouseLeave(object? sender, MouseEventArgs e)
        {
            // 延迟关闭，避免鼠标快速移动时意外关闭
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsMouseOver)
                {
                    CloseWindow();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// 显示主窗口按钮点击事件
        /// </summary>
        private void ShowMainWindow_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 触发显示主窗口事件
                ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
                CloseWindow();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示主窗口时发生错误");
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        private void SetWindowPosition()
        {
            try
            {
                var workingArea = SystemParameters.WorkArea;
                Left = workingArea.Right - Width - 10;
                Top = workingArea.Bottom - Height - 10;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置窗口位置时发生错误");
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow()
        {
            try
            {
                // 只隐藏窗口，不停止数据更新，这样下次显示时数据是最新的
                Hide();
                _logger.LogDebug("托盘弹出窗口已隐藏");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "隐藏托盘弹出窗口时发生错误");
            }
        }

        /// <summary>
        /// 在指定位置显示窗口
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        public void ShowAt(double x, double y)
        {
            // 确保窗口不会超出屏幕边界
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // 调整位置以确保窗口完全可见
            if (x + Width > screenWidth)
                x = screenWidth - Width - 10;
            if (y + Height > screenHeight)
                y = screenHeight - Height - 10;
            
            if (x < 0) x = 10;
            if (y < 0) y = 10;
            
            Left = x;
            Top = y;
            
            Show();
            Activate();
        }

        /// <summary>
        /// 显示主窗口请求事件
        /// </summary>
        public event EventHandler? ShowMainWindowRequested;
    }
}