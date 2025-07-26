using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Events;
using System.Drawing;
using System.Windows.Forms;

namespace MyComputerMonitor.Infrastructure.Services;

/// <summary>
/// 系统托盘服务接口
/// </summary>
public interface ISystemTrayService : IDisposable
{
    /// <summary>
    /// 系统托盘事件
    /// </summary>
    event EventHandler<SystemTrayEventArgs>? TrayEvent;
    
    /// <summary>
    /// 托盘图标单击事件
    /// </summary>
    event EventHandler? TrayIconClicked;
    
    /// <summary>
    /// 托盘图标双击事件
    /// </summary>
    event EventHandler? TrayIconDoubleClicked;
    
    /// <summary>
    /// 显示主窗口请求事件
    /// </summary>
    event EventHandler? ShowMainWindowRequested;
    
    /// <summary>
    /// 退出应用程序请求事件
    /// </summary>
    event EventHandler? ExitApplicationRequested;
    
    /// <summary>
    /// 初始化系统托盘
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 显示系统托盘图标
    /// </summary>
    void ShowTrayIcon();
    
    /// <summary>
    /// 隐藏系统托盘图标
    /// </summary>
    void HideTrayIcon();
    
    /// <summary>
    /// 更新托盘图标
    /// </summary>
    /// <param name="icon">新图标</param>
    void UpdateTrayIcon(Icon icon);
    
    /// <summary>
    /// 更新托盘提示文本
    /// </summary>
    /// <param name="text">提示文本</param>
    void SetTooltipText(string text);
    
    /// <summary>
    /// 显示气球提示
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="text">内容</param>
    /// <param name="timeout">显示时间(毫秒)</param>
    void ShowBalloonTip(string title, string text, int timeout = 3000);
    
    /// <summary>
    /// 是否显示托盘图标
    /// </summary>
    bool IsVisible { get; }
}

/// <summary>
/// 系统托盘服务实现
/// </summary>
public class SystemTrayService : ISystemTrayService
{
    private readonly ILogger<SystemTrayService> _logger;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private bool _disposed;

    /// <summary>
    /// 系统托盘事件
    /// </summary>
    public event EventHandler<SystemTrayEventArgs>? TrayEvent;
    
    /// <summary>
    /// 托盘图标单击事件
    /// </summary>
    public event EventHandler? TrayIconClicked;
    
    /// <summary>
    /// 托盘图标双击事件
    /// </summary>
    public event EventHandler? TrayIconDoubleClicked;
    
    /// <summary>
    /// 显示主窗口请求事件
    /// </summary>
    public event EventHandler? ShowMainWindowRequested;
    
    /// <summary>
    /// 退出应用程序请求事件
    /// </summary>
    public event EventHandler? ExitApplicationRequested;

    /// <summary>
    /// 是否显示托盘图标
    /// </summary>
    public bool IsVisible => _notifyIcon?.Visible ?? false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public SystemTrayService(ILogger<SystemTrayService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 初始化系统托盘
    /// </summary>
    public void Initialize()
    {
        try
        {
            _notifyIcon = new NotifyIcon();
            
            // 设置默认图标
            SetDefaultIcon();
            
            // 设置默认提示文本
            _notifyIcon.Text = "我的电脑监控器";
            
            // 创建右键菜单
            CreateContextMenu();
            
            // 绑定事件
            _notifyIcon.MouseClick += OnNotifyIconMouseClick;
            _notifyIcon.MouseDoubleClick += OnNotifyIconMouseDoubleClick;
            _notifyIcon.BalloonTipClicked += OnBalloonTipClicked;
            
            _logger.LogInformation("系统托盘服务初始化成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化系统托盘服务时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 显示系统托盘图标
    /// </summary>
    public void ShowTrayIcon()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = true;
            _logger.LogDebug("系统托盘图标已显示");
        }
    }

    /// <summary>
    /// 隐藏系统托盘图标
    /// </summary>
    public void HideTrayIcon()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _logger.LogDebug("系统托盘图标已隐藏");
        }
    }

    /// <summary>
    /// 更新托盘图标
    /// </summary>
    public void UpdateTrayIcon(Icon icon)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Icon = icon;
            _logger.LogDebug("系统托盘图标已更新");
        }
    }

    /// <summary>
    /// 更新托盘提示文本
    /// </summary>
    public void SetTooltipText(string text)
    {
        if (_notifyIcon != null)
        {
            // NotifyIcon.Text 最大长度为63个字符
            _notifyIcon.Text = text.Length > 63 ? text[..60] + "..." : text;
            _logger.LogDebug($"系统托盘提示文本已更新: {text}");
        }
    }

    /// <summary>
    /// 显示气球提示
    /// </summary>
    public void ShowBalloonTip(string title, string text, int timeout = 3000)
    {
        if (_notifyIcon != null && _notifyIcon.Visible)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, text, ToolTipIcon.Info);
            _logger.LogDebug($"显示气球提示: {title} - {text}");
        }
    }

    /// <summary>
    /// 设置默认图标
    /// </summary>
    private void SetDefaultIcon()
    {
        try
        {
            // 创建一个简单的默认图标
            using var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            
            // 绘制一个简单的监控图标
            graphics.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.DodgerBlue);
            graphics.FillRectangle(brush, 2, 2, 12, 8);
            graphics.FillRectangle(brush, 4, 11, 8, 3);
            graphics.FillRectangle(brush, 6, 14, 4, 1);
            
            var icon = Icon.FromHandle(bitmap.GetHicon());
            _notifyIcon!.Icon = icon;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "设置默认图标时发生错误，使用系统默认图标");
            _notifyIcon!.Icon = SystemIcons.Application;
        }
    }

    /// <summary>
    /// 创建右键菜单
    /// </summary>
    private void CreateContextMenu()
    {
        _contextMenu = new ContextMenuStrip();

        // 显示主窗口
        var showMenuItem = new ToolStripMenuItem("显示主窗口(&S)")
        {
            Font = new Font(_contextMenu.Font, FontStyle.Bold)
        };
        showMenuItem.Click += (s, e) => {
            ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
            TrayEvent?.Invoke(this, new SystemTrayEventArgs(SystemTrayEventType.ShowMainWindow));
        };

        // 分隔符
        var separator1 = new ToolStripSeparator();

        // 设置
        var settingsMenuItem = new ToolStripMenuItem("设置(&T)");
        settingsMenuItem.Click += (s, e) => TrayEvent?.Invoke(this, new SystemTrayEventArgs(SystemTrayEventType.ShowSettings));

        // 关于
        var aboutMenuItem = new ToolStripMenuItem("关于(&A)");
        aboutMenuItem.Click += (s, e) => TrayEvent?.Invoke(this, new SystemTrayEventArgs(SystemTrayEventType.ShowAbout));

        // 分隔符
        var separator2 = new ToolStripSeparator();

        // 退出
        var exitMenuItem = new ToolStripMenuItem("退出(&X)");
        exitMenuItem.Click += (s, e) => {
            ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
            TrayEvent?.Invoke(this, new SystemTrayEventArgs(SystemTrayEventType.ExitApplication));
        };

        // 添加菜单项
        _contextMenu.Items.AddRange(new ToolStripItem[]
        {
            showMenuItem,
            separator1,
            settingsMenuItem,
            aboutMenuItem,
            separator2,
            exitMenuItem
        });

        _notifyIcon!.ContextMenuStrip = _contextMenu;
    }

    /// <summary>
    /// 托盘图标单击事件
    /// </summary>
    private void OnNotifyIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            TrayIconClicked?.Invoke(this, EventArgs.Empty);
            TrayEvent?.Invoke(this, new SystemTrayEventArgs(SystemTrayEventType.Click));
        }
        else if (e.Button == MouseButtons.Right)
        {
            TrayEvent?.Invoke(this, new SystemTrayEventArgs(SystemTrayEventType.RightClick));
        }
    }

    /// <summary>
    /// 托盘图标双击事件
    /// </summary>
    private void OnNotifyIconMouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            TrayIconDoubleClicked?.Invoke(this, EventArgs.Empty);
            TrayEvent?.Invoke(this, new SystemTrayEventArgs(SystemTrayEventType.DoubleClick));
        }
    }

    /// <summary>
    /// 气球提示点击事件
    /// </summary>
    private void OnBalloonTipClicked(object? sender, EventArgs e)
    {
        ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
        TrayEvent?.Invoke(this, new SystemTrayEventArgs(SystemTrayEventType.ShowMainWindow));
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            _contextMenu?.Dispose();
            _contextMenu = null;

            _disposed = true;
            _logger.LogInformation("系统托盘服务已释放资源");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放系统托盘服务资源时发生错误");
        }
    }
}