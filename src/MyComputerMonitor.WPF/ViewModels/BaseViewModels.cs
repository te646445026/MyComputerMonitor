using CommunityToolkit.Mvvm.ComponentModel;

namespace MyComputerMonitor.WPF.ViewModels
{
    /// <summary>
    /// 仪表板视图模型
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "系统概览";

    /// <summary>
    /// 构造函数
    /// </summary>
    public DashboardViewModel()
    {
        // 初始化仪表板数据
    }
}

/// <summary>
/// CPU视图模型
/// </summary>
public partial class CpuViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "CPU监控";

    /// <summary>
    /// 构造函数
    /// </summary>
    public CpuViewModel()
    {
        // 初始化CPU监控数据
    }
}

/// <summary>
/// GPU视图模型
/// </summary>
public partial class GpuViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "GPU监控";

    /// <summary>
    /// 构造函数
    /// </summary>
    public GpuViewModel()
    {
        // 初始化GPU监控数据
    }
}

/// <summary>
/// 内存视图模型
/// </summary>
public partial class MemoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "内存监控";

    /// <summary>
    /// 构造函数
    /// </summary>
    public MemoryViewModel()
    {
        // 初始化内存监控数据
    }
}


/// <summary>
/// 网络视图模型
/// </summary>
public partial class NetworkViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "网络监控";

    /// <summary>
    /// 构造函数
    /// </summary>
    public NetworkViewModel()
    {
        // 初始化网络监控数据
    }
}

/// <summary>
/// 设置视图模型
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "应用设置";

    /// <summary>
    /// 构造函数
    /// </summary>
    public SettingsViewModel()
    {
        // 初始化设置数据
    }
}

/// <summary>
/// 关于视图模型
/// </summary>
public partial class AboutViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "关于应用";

    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _description = "实时监控电脑硬件状态的应用程序";

    /// <summary>
    /// 构造函数
    /// </summary>
    public AboutViewModel()
    {
        // 初始化关于信息
    }
}
}