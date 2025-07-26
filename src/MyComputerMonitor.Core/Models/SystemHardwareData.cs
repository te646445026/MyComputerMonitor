namespace MyComputerMonitor.Core.Models;

/// <summary>
/// 系统硬件监控数据汇总
/// </summary>
public class SystemHardwareData
{
    /// <summary>
    /// CPU信息列表
    /// </summary>
    public List<CpuInfo> Cpus { get; set; } = [];
    
    /// <summary>
    /// GPU信息列表
    /// </summary>
    public List<GpuInfo> Gpus { get; set; } = [];
    
    /// <summary>
    /// 内存信息
    /// </summary>
    public MemoryInfo? Memory { get; set; }
    
    /// <summary>
    /// 主板信息
    /// </summary>
    public MotherboardInfo? Motherboard { get; set; }
    
    /// <summary>
    /// 存储设备信息列表（仅温度监控）
    /// </summary>
    public List<StorageInfo> StorageDevices { get; set; } = [];
    
    /// <summary>
    /// 网络适配器信息列表
    /// </summary>
    public List<NetworkInfo> NetworkAdapters { get; set; } = [];
    
    /// <summary>
    /// 风扇信息列表
    /// </summary>
    public List<FanInfo> Fans { get; set; } = [];
    
    /// <summary>
    /// 数据更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 系统运行时间
    /// </summary>
    public TimeSpan SystemUptime { get; set; }
    
    /// <summary>
    /// 获取主CPU信息
    /// </summary>
    /// <returns>主CPU信息，如果不存在则返回null</returns>
    public CpuInfo? GetPrimaryCpu()
    {
        return Cpus.FirstOrDefault();
    }
    
    /// <summary>
    /// 获取主GPU信息
    /// </summary>
    /// <returns>主GPU信息，如果不存在则返回null</returns>
    public GpuInfo? GetPrimaryGpu()
    {
        return Gpus.FirstOrDefault();
    }
    
    /// <summary>
    /// 获取系统总体健康状态
    /// </summary>
    /// <returns>健康状态描述</returns>
    public string GetSystemHealthStatus()
    {
        var issues = new List<string>();
        
        // 检查CPU温度
        var cpu = GetPrimaryCpu();
        if (cpu?.Temperature > 80)
            issues.Add("CPU温度过高");
        
        // 检查GPU温度
        var gpu = GetPrimaryGpu();
        if (gpu?.Temperature > 85)
            issues.Add("GPU温度过高");
        
        // 检查内存使用率
        if (Memory?.UsagePercentage > 90)
            issues.Add("内存使用率过高");
        
        return issues.Count == 0 ? "系统运行正常" : string.Join(", ", issues);
    }
}

/// <summary>
/// 主板信息模型
/// </summary>
public class MotherboardInfo : HardwareInfo
{
    public MotherboardInfo()
    {
        Type = HardwareType.Motherboard;
    }
    
    /// <summary>
    /// 主板温度 (°C)
    /// </summary>
    public double Temperature => GetSensor(SensorType.Temperature)?.Value ?? 0.0;
    
    /// <summary>
    /// 制造商
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;
    
    /// <summary>
    /// 型号
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// BIOS版本
    /// </summary>
    public string BiosVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// BIOS日期
    /// </summary>
    public DateTime BiosDate { get; set; }
    
    /// <summary>
    /// 芯片组
    /// </summary>
    public string Chipset { get; set; } = string.Empty;
}

/// <summary>
/// 网络适配器信息模型
/// </summary>
public class NetworkInfo : HardwareInfo
{
    public NetworkInfo()
    {
        Type = HardwareType.Network;
    }
    
    /// <summary>
    /// 网络使用率 (%)
    /// </summary>
    public double UsagePercentage => GetSensor(SensorType.Usage)?.Value ?? 0.0;
    
    /// <summary>
    /// 下载速度 (MB/s)
    /// </summary>
    public double DownloadSpeed { get; set; }
    
    /// <summary>
    /// 上传速度 (MB/s)
    /// </summary>
    public double UploadSpeed { get; set; }
    
    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsConnected { get; set; }
    
    /// <summary>
    /// 网络类型 (Ethernet, WiFi等)
    /// </summary>
    public string NetworkType { get; set; } = string.Empty;
    
    /// <summary>
    /// MAC地址
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
}

/// <summary>
/// 风扇信息模型
/// </summary>
public class FanInfo : HardwareInfo
{
    public FanInfo()
    {
        Type = HardwareType.Fan;
    }
    
    /// <summary>
    /// 风扇转速 (RPM)
    /// </summary>
    public double Speed => GetSensor(SensorType.Fan)?.Value ?? 0.0;
    
    /// <summary>
    /// 风扇控制百分比 (%)
    /// </summary>
    public double ControlPercentage => GetSensor(SensorType.Control)?.Value ?? 0.0;
    
    /// <summary>
    /// 最小转速 (RPM)
    /// </summary>
    public double MinSpeed { get; set; }
    
    /// <summary>
    /// 最大转速 (RPM)
    /// </summary>
    public double MaxSpeed { get; set; }
    
    /// <summary>
    /// 风扇位置描述
    /// </summary>
    public string Location { get; set; } = string.Empty;
}