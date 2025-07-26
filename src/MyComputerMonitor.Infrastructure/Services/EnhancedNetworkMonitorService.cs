using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Interfaces;
using MyComputerMonitor.Core.Models;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MyComputerMonitor.Infrastructure.Services;

/// <summary>
/// 增强的网络监控服务实现
/// </summary>
public class EnhancedNetworkMonitorService : IEnhancedNetworkMonitorService
{
    private readonly ILogger<EnhancedNetworkMonitorService> _logger;
    private readonly IPerformanceCounterService _performanceCounterService;
    private readonly Dictionary<string, NetworkInterfaceStats> _previousStats;
    private readonly object _statsLock = new();

    /// <summary>
    /// 网络接口统计信息
    /// </summary>
    private class NetworkInterfaceStats
    {
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
        public DateTime Timestamp { get; set; }
    }
    private readonly ConcurrentDictionary<string, NetworkUsageInfo> _previousUsageData = new();
    private readonly object _lockObject = new();

    public EnhancedNetworkMonitorService(
        ILogger<EnhancedNetworkMonitorService> logger,
        IPerformanceCounterService performanceCounterService)
    {
        _logger = logger;
        _performanceCounterService = performanceCounterService;
        _previousStats = new Dictionary<string, NetworkInterfaceStats>();
    }

    /// <summary>
    /// 预热网络监控，初始化基准数据
    /// </summary>
    public async Task WarmupAsync()
    {
        try
        {
            _logger.LogInformation("开始预热网络监控服务...");
            
            // 第一次初始化基准统计数据
            InitializeBaselineStats();
            
            // 等待1秒后进行第二次采样
            await Task.Delay(1000);
            
            // 第二次采样以获取初始速度数据
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in networkInterfaces)
            {
                if (IsPhysicalNetworkAdapter(networkInterface))
                {
                    // 计算速度（这会更新_previousStats）
                    var (downloadSpeed, uploadSpeed) = CalculateNetworkSpeed(networkInterface);
                    _logger.LogDebug($"预热采样 - 网络接口 {networkInterface.Name}: 下载 {downloadSpeed:F2} MB/s, 上传 {uploadSpeed:F2} MB/s");
                }
            }
            
            // 再等待1秒进行第三次采样，确保有足够的数据
            await Task.Delay(1000);
            
            // 第三次采样以获取更准确的速度数据
            foreach (var networkInterface in networkInterfaces)
            {
                if (IsPhysicalNetworkAdapter(networkInterface))
                {
                    var (downloadSpeed, uploadSpeed) = CalculateNetworkSpeed(networkInterface);
                    _logger.LogDebug($"预热最终采样 - 网络接口 {networkInterface.Name}: 下载 {downloadSpeed:F2} MB/s, 上传 {uploadSpeed:F2} MB/s");
                }
            }
            
            _logger.LogInformation("网络监控服务预热完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预热网络监控服务时发生错误");
        }
    }

    /// <summary>
    /// 初始化所有物理网络接口的基准统计数据
    /// </summary>
    private void InitializeBaselineStats()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in networkInterfaces)
            {
                if (IsPhysicalNetworkAdapter(networkInterface))
                {
                    var stats = networkInterface.GetIPStatistics();
                    var interfaceId = networkInterface.Id;
                    
                    lock (_statsLock)
                    {
                        _previousStats[interfaceId] = new NetworkInterfaceStats
                        {
                            BytesReceived = stats.BytesReceived,
                            BytesSent = stats.BytesSent,
                            Timestamp = DateTime.Now
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "初始化网络接口基准数据时发生错误");
        }
    }

    /// <summary>
    /// 获取所有物理网络适配器信息
    /// </summary>
    public async Task<List<NetworkInfo>> GetPhysicalNetworkAdaptersAsync()
    {
        try
        {
            _logger.LogDebug("开始获取所有物理网络适配器信息");
            
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var physicalAdapters = new List<NetworkInfo>();
            var seenMacAddresses = new Dictionary<string, NetworkInfo>();

            foreach (var networkInterface in networkInterfaces)
            {
                // 过滤物理网络适配器
                if (!IsPhysicalNetworkAdapter(networkInterface))
                    continue;

                var adapterInfo = await ConvertToNetworkInfoAsync(networkInterface);
                if (adapterInfo == null)
                    continue;

                // MAC地址去重逻辑
                var macAddress = adapterInfo.MacAddress;
                if (string.IsNullOrEmpty(macAddress))
                    continue;

                if (seenMacAddresses.ContainsKey(macAddress))
                {
                    // 比较优先级，选择更好的适配器
                    var existingAdapter = seenMacAddresses[macAddress];
                    var existingPriority = CalculateAdapterPriority(networkInterface);
                    var currentPriority = CalculateAdapterPriority(networkInterface);

                    if (currentPriority > existingPriority)
                    {
                        // 替换为优先级更高的适配器
                        var index = physicalAdapters.FindIndex(a => a.MacAddress == macAddress);
                        if (index >= 0)
                        {
                            physicalAdapters[index] = adapterInfo;
                            seenMacAddresses[macAddress] = adapterInfo;
                        }
                    }
                }
                else
                {
                    seenMacAddresses[macAddress] = adapterInfo;
                    physicalAdapters.Add(adapterInfo);
                }
            }

            _logger.LogInformation($"成功获取到 {physicalAdapters.Count} 个物理网络适配器");
            return physicalAdapters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取物理网络适配器信息时发生错误");
            return new List<NetworkInfo>();
        }
    }

    /// <summary>
    /// 获取指定网络适配器的详细信息
    /// </summary>
    public async Task<NetworkInfo?> GetNetworkAdapterInfoAsync(string adapterId)
    {
        try
        {
            _logger.LogDebug($"获取网络适配器详细信息: {adapterId}");
            
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var targetInterface = networkInterfaces.FirstOrDefault(ni => ni.Name == adapterId || ni.Id == adapterId);
            
            if (targetInterface == null)
            {
                _logger.LogWarning($"未找到指定的网络适配器: {adapterId}");
                return null;
            }

            if (!IsPhysicalNetworkAdapter(targetInterface))
            {
                _logger.LogWarning($"指定的网络适配器不是物理适配器: {adapterId}");
                return null;
            }

            return await ConvertToNetworkInfoAsync(targetInterface);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取网络适配器详细信息时发生错误: {adapterId}");
            return null;
        }
    }

    /// <summary>
    /// 判断是否为物理网络适配器
    /// </summary>
    private bool IsPhysicalNetworkAdapter(NetworkInterface networkInterface)
    {
        // 排除回环接口
        if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            return false;

        // 排除隧道接口
        if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
            return false;

        var name = networkInterface.Name.ToLower();
        var description = networkInterface.Description.ToLower();

        // 排除虚拟适配器关键词
        var virtualKeywords = new[]
        {
            "virtual", "vmware", "virtualbox", "hyper-v", "vbox", "tap", "tun",
            "loopback", "teredo", "isatap", "6to4", "bluetooth", "vpn",
            "microsoft", "software", "miniport", "wan", "ras", "ppp",
            "dial-up", "modem", "bridge", "filter", "ndis", "packet"
        };

        if (virtualKeywords.Any(keyword => name.Contains(keyword) || description.Contains(keyword)))
            return false;

        // 只保留以太网和无线网络接口
        return networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
               networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;
    }

    /// <summary>
    /// 计算网络适配器的优先级
    /// </summary>
    private int CalculateAdapterPriority(NetworkInterface networkInterface)
    {
        int priority = 0;
        
        // 状态为Up的适配器优先级更高
        if (networkInterface.OperationalStatus == OperationalStatus.Up)
            priority += 100;
        
        try
        {
            var ipProperties = networkInterface.GetIPProperties();
            
            // 有IPv4地址的适配器优先级更高
            var hasIPv4 = ipProperties.UnicastAddresses.Any(addr => 
                addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(addr.Address));
            if (hasIPv4)
                priority += 50;
            
            // 有默认网关的适配器优先级更高
            if (ipProperties.GatewayAddresses.Any())
                priority += 30;
            
            // 尝试检查DHCP状态（某些.NET版本可能不支持）
            try
            {
                // 通过检查是否有DHCP服务器地址来判断DHCP状态
                var dhcpAddresses = ipProperties.DhcpServerAddresses;
                if (dhcpAddresses != null && dhcpAddresses.Count > 0)
                    priority += 20;
            }
            catch
            {
                // 忽略DHCP检查错误
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, $"计算适配器优先级时获取IP属性失败: {networkInterface.Name}");
        }
        
        // 以太网接口优先级高于无线接口
        if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            priority += 10;
        else if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            priority += 5;
        
        return priority;
    }

    /// <summary>
    /// 将NetworkInterface转换为NetworkInfo
    /// </summary>
    private async Task<NetworkInfo?> ConvertToNetworkInfoAsync(NetworkInterface networkInterface)
    {
        try
        {
            var networkInfo = new NetworkInfo
            {
                Name = networkInterface.Name,
                NetworkType = networkInterface.NetworkInterfaceType.ToString(),
                IsConnected = networkInterface.OperationalStatus == OperationalStatus.Up,
                MacAddress = FormatMacAddress(networkInterface.GetPhysicalAddress().ToString()),
                Identifier = $"network/{networkInterface.Id}",
                LastUpdated = DateTime.Now,
                IsOnline = networkInterface.OperationalStatus == OperationalStatus.Up
            };

            // 获取IP属性
            try
            {
                var ipProperties = networkInterface.GetIPProperties();
                
                // 获取IPv4地址
                var ipv4Address = ipProperties.UnicastAddresses
                    .FirstOrDefault(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                          !IPAddress.IsLoopback(addr.Address));
                
                if (ipv4Address != null)
                {
                    networkInfo.IpAddress = ipv4Address.Address.ToString();
                }
                
                // 直接计算网络速度
                var (downloadSpeed, uploadSpeed) = CalculateNetworkSpeed(networkInterface);
                networkInfo.DownloadSpeed = downloadSpeed;
                networkInfo.UploadSpeed = uploadSpeed;

                // 获取网络使用率信息（用于使用率百分比）
                try
                {
                    // 尝试多种方式获取网络使用率
                    NetworkUsageInfo? networkUsage = null;
                    
                    // 首先尝试使用网络接口名称
                    networkUsage = await _performanceCounterService.GetNetworkUsageAsync(networkInfo.Name);
                    
                    // 如果没有数据，尝试使用描述
                    if (networkUsage == null || string.IsNullOrEmpty(networkUsage.InterfaceName))
                    {
                        networkUsage = await _performanceCounterService.GetNetworkUsageAsync(networkInterface.Description);
                    }
                    
                    // 如果还是没有数据，尝试不指定接口名称（使用默认接口）
                    if (networkUsage == null || string.IsNullOrEmpty(networkUsage.InterfaceName))
                    {
                        networkUsage = await _performanceCounterService.GetNetworkUsageAsync(null);
                    }
                    
                    double usagePercentage = 0.0;
                    if (networkUsage != null && !string.IsNullOrEmpty(networkUsage.InterfaceName))
                    {
                        usagePercentage = networkUsage.UsagePercentage;
                        _logger.LogDebug($"网络接口 {networkInfo.Name} 使用率: {usagePercentage}%");
                    }
                    else
                    {
                        // 如果无法从性能计数器获取使用率，基于当前速度估算
                        // 假设1Gbps网络带宽
                        var totalSpeedMbps = (downloadSpeed + uploadSpeed) * 8; // 转换为Mbps
                        usagePercentage = Math.Min(100.0, (totalSpeedMbps / 1000.0) * 100); // 1000Mbps = 1Gbps
                        _logger.LogDebug($"网络接口 {networkInfo.Name} 估算使用率: {usagePercentage}%");
                    }
                    
                    // 添加传感器数据
                    networkInfo.Sensors.Add(new SensorData
                    {
                        Name = "Network Usage",
                        Type = MyComputerMonitor.Core.Models.SensorType.Usage,
                        Value = usagePercentage,
                        Unit = "%",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = $"{networkInfo.Identifier}/usage"
                    });

                    // 添加下载速度传感器
                    networkInfo.Sensors.Add(new SensorData
                    {
                        Name = "Download Speed",
                        Type = MyComputerMonitor.Core.Models.SensorType.Throughput,
                        Value = networkInfo.DownloadSpeed,
                        Unit = "MB/s",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = $"{networkInfo.Identifier}/download"
                    });

                    // 添加上传速度传感器
                    networkInfo.Sensors.Add(new SensorData
                    {
                        Name = "Upload Speed",
                        Type = MyComputerMonitor.Core.Models.SensorType.Throughput,
                        Value = networkInfo.UploadSpeed,
                        Unit = "MB/s",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = $"{networkInfo.Identifier}/upload"
                    });
                }
                catch (Exception usageEx)
                {
                    _logger.LogWarning(usageEx, $"获取网络适配器使用率信息失败: {networkInfo.Name}");
                    
                    // 添加默认的传感器数据
                    networkInfo.Sensors.Add(new SensorData
                    {
                        Name = "Network Usage",
                        Type = MyComputerMonitor.Core.Models.SensorType.Usage,
                        Value = 0.0,
                        Unit = "%",
                        IsValid = false,
                        LastUpdated = DateTime.Now,
                        Identifier = $"{networkInfo.Identifier}/usage"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"获取网络适配器IP属性失败: {networkInterface.Name}");
            }

            return networkInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"转换网络接口信息时发生错误: {networkInterface.Name}");
            return null;
        }
    }

    /// <summary>
    /// 计算网络接口的实时速度
    /// </summary>
    /// <param name="networkInterface">网络接口</param>
    /// <returns>下载和上传速度（MB/s）</returns>
    private (double downloadSpeed, double uploadSpeed) CalculateNetworkSpeed(NetworkInterface networkInterface)
    {
        try
        {
            var stats = networkInterface.GetIPStatistics();
            var currentTime = DateTime.Now;
            var interfaceId = networkInterface.Id;

            lock (_statsLock)
            {
                if (_previousStats.TryGetValue(interfaceId, out var previousStats))
                {
                    var timeDiff = (currentTime - previousStats.Timestamp).TotalSeconds;
                    
                    if (timeDiff > 0)
                    {
                        var bytesReceivedDiff = stats.BytesReceived - previousStats.BytesReceived;
                        var bytesSentDiff = stats.BytesSent - previousStats.BytesSent;
                        
                        var downloadSpeed = Math.Max(0, bytesReceivedDiff / timeDiff / (1024.0 * 1024.0));
                        var uploadSpeed = Math.Max(0, bytesSentDiff / timeDiff / (1024.0 * 1024.0));
                        
                        _logger.LogDebug($"网络接口 {networkInterface.Name}: 下载 {downloadSpeed:F2} MB/s, 上传 {uploadSpeed:F2} MB/s");
                        
                        // 更新统计信息
                        _previousStats[interfaceId] = new NetworkInterfaceStats
                        {
                            BytesReceived = stats.BytesReceived,
                            BytesSent = stats.BytesSent,
                            Timestamp = currentTime
                        };
                        
                        return (Math.Round(downloadSpeed, 2), Math.Round(uploadSpeed, 2));
                    }
                }
                
                // 首次记录或时间差异太小，记录当前统计信息
                _previousStats[interfaceId] = new NetworkInterfaceStats
                {
                    BytesReceived = stats.BytesReceived,
                    BytesSent = stats.BytesSent,
                    Timestamp = currentTime
                };
                
                return (0.0, 0.0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"计算网络接口 {networkInterface.Name} 速度时发生错误");
            return (0.0, 0.0);
        }
    }

    /// <summary>
    /// 格式化MAC地址
    /// </summary>
    private static string FormatMacAddress(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress))
            return "未知";
            
        // 如果MAC地址长度不是12位，直接返回原值
        if (macAddress.Length != 12)
            return macAddress;

        // 将12位MAC地址格式化为 XX:XX:XX:XX:XX:XX 格式
        return string.Join(":", Enumerable.Range(0, 6)
            .Select(i => macAddress.Substring(i * 2, 2)));
    }
}