using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace MyComputerMonitor.Infrastructure.Services;

/// <summary>
/// 性能计数器服务接口
/// </summary>
public interface IPerformanceCounterService
{
    /// <summary>
    /// 获取CPU使用率
    /// </summary>
    /// <returns>CPU使用率百分比</returns>
    Task<double> GetCpuUsageAsync();
    
    /// <summary>
    /// 获取内存使用率
    /// </summary>
    /// <returns>内存使用率百分比</returns>
    Task<double> GetMemoryUsageAsync();
    
    /// <summary>
    /// 获取磁盘使用率
    /// </summary>
    /// <param name="driveName">驱动器名称（如"C:"）</param>
    /// <returns>磁盘使用率百分比</returns>
    Task<double> GetDiskUsageAsync(string driveName = "C:");
    
    /// <summary>
    /// 获取网络使用率
    /// </summary>
    /// <param name="interfaceName">网络接口名称</param>
    /// <returns>网络使用率信息</returns>
    Task<NetworkUsageInfo> GetNetworkUsageAsync(string? interfaceName = null);
    
    /// <summary>
    /// 获取进程CPU使用率
    /// </summary>
    /// <param name="processName">进程名称</param>
    /// <returns>进程CPU使用率百分比</returns>
    Task<double> GetProcessCpuUsageAsync(string processName);
    
    /// <summary>
    /// 获取进程内存使用量
    /// </summary>
    /// <param name="processName">进程名称</param>
    /// <returns>进程内存使用量（字节）</returns>
    Task<long> GetProcessMemoryUsageAsync(string processName);
    
    /// <summary>
    /// 获取系统性能摘要
    /// </summary>
    /// <returns>系统性能摘要</returns>
    Task<SystemPerformanceSummary> GetSystemPerformanceSummaryAsync();
    
    /// <summary>
    /// 开始性能监控
    /// </summary>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// 停止性能监控
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// 释放资源
    /// </summary>
    void Dispose();
}

/// <summary>
/// 网络使用率信息
/// </summary>
public class NetworkUsageInfo
{
    /// <summary>
    /// 接收速率（字节/秒）
    /// </summary>
    public double BytesReceivedPerSecond { get; set; }
    
    /// <summary>
    /// 发送速率（字节/秒）
    /// </summary>
    public double BytesSentPerSecond { get; set; }
    
    /// <summary>
    /// 总接收字节数
    /// </summary>
    public long TotalBytesReceived { get; set; }
    
    /// <summary>
    /// 总发送字节数
    /// </summary>
    public long TotalBytesSent { get; set; }
    
    /// <summary>
    /// 网络接口名称
    /// </summary>
    public string InterfaceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 网络使用率百分比
    /// </summary>
    public double UsagePercentage { get; set; }
}

/// <summary>
/// 系统性能摘要
/// </summary>
public class SystemPerformanceSummary
{
    /// <summary>
    /// CPU使用率
    /// </summary>
    public double CpuUsage { get; set; }
    
    /// <summary>
    /// 内存使用率
    /// </summary>
    public double MemoryUsage { get; set; }
    
    /// <summary>
    /// 磁盘使用率
    /// </summary>
    public double DiskUsage { get; set; }
    
    /// <summary>
    /// 网络使用率
    /// </summary>
    public NetworkUsageInfo NetworkUsage { get; set; } = new();
    
    /// <summary>
    /// 系统负载等级
    /// </summary>
    public SystemLoadLevel LoadLevel { get; set; }
    
    /// <summary>
    /// 性能评分（0-100）
    /// </summary>
    public double PerformanceScore { get; set; }
    
    /// <summary>
    /// 采样时间
    /// </summary>
    public DateTime SampleTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 系统负载等级
/// </summary>
public enum SystemLoadLevel
{
    /// <summary>
    /// 低负载
    /// </summary>
    Low,
    
    /// <summary>
    /// 中等负载
    /// </summary>
    Medium,
    
    /// <summary>
    /// 高负载
    /// </summary>
    High,
    
    /// <summary>
    /// 极高负载
    /// </summary>
    Critical
}

/// <summary>
/// 性能计数器服务实现
/// </summary>
public class PerformanceCounterService : IPerformanceCounterService, IDisposable
{
    private readonly ILogger<PerformanceCounterService> _logger;
    private readonly Dictionary<string, PerformanceCounter> _counters;
    private readonly object _lockObject = new();
    private bool _disposed = false;
    private bool _isMonitoring = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public PerformanceCounterService(ILogger<PerformanceCounterService> logger)
    {
        _logger = logger;
        _counters = new Dictionary<string, PerformanceCounter>();
        InitializeCounters();
    }

    /// <summary>
    /// 初始化性能计数器
    /// </summary>
    private void InitializeCounters()
    {
        try
        {
            // CPU使用率计数器
            AddCounter("cpu_total", "Processor", "% Processor Time", "_Total");
            
            // 内存计数器
            AddCounter("memory_available", "Memory", "Available MBytes");
            AddCounter("memory_committed", "Memory", "Committed Bytes");
            
            // 磁盘计数器
            AddCounter("disk_time", "PhysicalDisk", "% Disk Time", "_Total");
            AddCounter("disk_read", "PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            AddCounter("disk_write", "PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            
            // 网络计数器
            var networkInterfaces = GetNetworkInterfaces();
            foreach (var interfaceName in networkInterfaces)
            {
                AddCounter($"network_received_{interfaceName}", "Network Interface", "Bytes Received/sec", interfaceName);
                AddCounter($"network_sent_{interfaceName}", "Network Interface", "Bytes Sent/sec", interfaceName);
            }
            
            _logger.LogInformation($"已初始化 {_counters.Count} 个性能计数器");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化性能计数器时发生错误");
        }
    }

    /// <summary>
    /// 添加性能计数器
    /// </summary>
    private void AddCounter(string key, string categoryName, string counterName, string? instanceName = null)
    {
        try
        {
            PerformanceCounter counter;
            if (string.IsNullOrEmpty(instanceName))
            {
                counter = new PerformanceCounter(categoryName, counterName);
            }
            else
            {
                counter = new PerformanceCounter(categoryName, counterName, instanceName);
            }
            
            // 预热计数器
            _ = counter.NextValue();
            
            lock (_lockObject)
            {
                _counters[key] = counter;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"添加性能计数器失败: {categoryName}\\{counterName}\\{instanceName}");
        }
    }

    /// <summary>
    /// 获取网络接口列表
    /// </summary>
    private List<string> GetNetworkInterfaces()
    {
        var interfaces = new List<string>();
        
        try
        {
            var category = new PerformanceCounterCategory("Network Interface");
            var instanceNames = category.GetInstanceNames();
            
            foreach (var instanceName in instanceNames)
            {
                if (!instanceName.Contains("Loopback") && 
                    !instanceName.Contains("Teredo") && 
                    !instanceName.Contains("isatap"))
                {
                    interfaces.Add(instanceName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取网络接口列表时发生错误");
        }
        
        return interfaces;
    }

    /// <summary>
    /// 获取CPU使用率
    /// </summary>
    public async Task<double> GetCpuUsageAsync()
    {
        try
        {
            lock (_lockObject)
            {
                if (_counters.TryGetValue("cpu_total", out var counter))
                {
                    return Math.Round(counter.NextValue(), 2);
                }
            }
            
            return 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取CPU使用率时发生错误");
            return 0.0;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取内存使用率
    /// </summary>
    public async Task<double> GetMemoryUsageAsync()
    {
        try
        {
            lock (_lockObject)
            {
                if (_counters.TryGetValue("memory_available", out var availableCounter) &&
                    _counters.TryGetValue("memory_committed", out var committedCounter))
                {
                    var availableMemory = availableCounter.NextValue();
                    var totalMemory = GC.GetTotalMemory(false) / (1024.0 * 1024.0); // 转换为MB
                    
                    // 使用系统总内存计算
                    var totalPhysicalMemory = GetTotalPhysicalMemory();
                    if (totalPhysicalMemory > 0)
                    {
                        var usedMemory = totalPhysicalMemory - availableMemory;
                        return Math.Round((usedMemory / totalPhysicalMemory) * 100, 2);
                    }
                }
            }
            
            return 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取内存使用率时发生错误");
            return 0.0;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取总物理内存
    /// </summary>
    private double GetTotalPhysicalMemory()
    {
        try
        {
            using var counter = new PerformanceCounter("Memory", "Available MBytes");
            var available = counter.NextValue();
            
            // 估算总内存（这是一个简化的方法）
            var totalMemoryInfo = GC.GetGCMemoryInfo();
            return available + (totalMemoryInfo.HeapSizeBytes / (1024.0 * 1024.0));
        }
        catch
        {
            return 8192; // 默认8GB
        }
    }

    /// <summary>
    /// 获取磁盘使用率
    /// </summary>
    public async Task<double> GetDiskUsageAsync(string driveName = "C:")
    {
        try
        {
            lock (_lockObject)
            {
                if (_counters.TryGetValue("disk_time", out var counter))
                {
                    return Math.Round(counter.NextValue(), 2);
                }
            }
            
            return 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取磁盘使用率时发生错误");
            return 0.0;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取网络使用率
    /// </summary>
    public async Task<NetworkUsageInfo> GetNetworkUsageAsync(string? interfaceName = null)
    {
        try
        {
            var networkInfo = new NetworkUsageInfo();
            
            if (string.IsNullOrEmpty(interfaceName))
            {
                // 获取第一个可用的网络接口
                var interfaces = GetNetworkInterfaces();
                _logger.LogDebug($"可用网络接口: {string.Join(", ", interfaces)}");
                if (interfaces.Count > 0)
                {
                    interfaceName = interfaces[0];
                    _logger.LogDebug($"使用默认网络接口: {interfaceName}");
                }
            }
            else
            {
                _logger.LogDebug($"尝试使用指定网络接口: {interfaceName}");
            }
            
            if (!string.IsNullOrEmpty(interfaceName))
            {
                networkInfo.InterfaceName = interfaceName;
                
                lock (_lockObject)
                {
                    var receivedKey = $"network_received_{interfaceName}";
                    var sentKey = $"network_sent_{interfaceName}";
                    
                    _logger.LogDebug($"查找计数器: {receivedKey}, {sentKey}");
                    
                    if (_counters.TryGetValue(receivedKey, out var receivedCounter))
                    {
                        networkInfo.BytesReceivedPerSecond = receivedCounter.NextValue();
                        _logger.LogDebug($"接收速度: {networkInfo.BytesReceivedPerSecond} bytes/sec");
                    }
                    else
                    {
                        _logger.LogWarning($"未找到接收计数器: {receivedKey}");
                    }
                    
                    if (_counters.TryGetValue(sentKey, out var sentCounter))
                    {
                        networkInfo.BytesSentPerSecond = sentCounter.NextValue();
                        _logger.LogDebug($"发送速度: {networkInfo.BytesSentPerSecond} bytes/sec");
                    }
                    else
                    {
                        _logger.LogWarning($"未找到发送计数器: {sentKey}");
                    }
                }
                
                // 计算网络使用率百分比（假设1Gbps网络）
                var totalBandwidth = 1000000000.0; // 1Gbps in bytes
                var totalUsage = networkInfo.BytesReceivedPerSecond + networkInfo.BytesSentPerSecond;
                networkInfo.UsagePercentage = Math.Round((totalUsage / totalBandwidth) * 100, 2);
                
                _logger.LogDebug($"网络使用率: {networkInfo.UsagePercentage}%");
            }
            else
            {
                _logger.LogWarning("没有可用的网络接口");
            }
            
            return networkInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取网络使用率时发生错误");
            return new NetworkUsageInfo();
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取进程CPU使用率
    /// </summary>
    public async Task<double> GetProcessCpuUsageAsync(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
                return 0.0;
            
            double totalCpuUsage = 0.0;
            foreach (var process in processes)
            {
                try
                {
                    using var counter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
                    totalCpuUsage += counter.NextValue();
                }
                catch
                {
                    // 忽略无法访问的进程
                }
                finally
                {
                    process.Dispose();
                }
            }
            
            return Math.Round(totalCpuUsage, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取进程 {processName} CPU使用率时发生错误");
            return 0.0;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取进程内存使用量
    /// </summary>
    public async Task<long> GetProcessMemoryUsageAsync(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
                return 0;
            
            long totalMemory = 0;
            foreach (var process in processes)
            {
                try
                {
                    totalMemory += process.WorkingSet64;
                }
                catch
                {
                    // 忽略无法访问的进程
                }
                finally
                {
                    process.Dispose();
                }
            }
            
            return totalMemory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取进程 {processName} 内存使用量时发生错误");
            return 0;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取系统性能摘要
    /// </summary>
    public async Task<SystemPerformanceSummary> GetSystemPerformanceSummaryAsync()
    {
        try
        {
            var summary = new SystemPerformanceSummary
            {
                CpuUsage = await GetCpuUsageAsync(),
                MemoryUsage = await GetMemoryUsageAsync(),
                DiskUsage = await GetDiskUsageAsync(),
                NetworkUsage = await GetNetworkUsageAsync(),
                SampleTime = DateTime.Now
            };
            
            // 计算系统负载等级
            var maxUsage = Math.Max(Math.Max(summary.CpuUsage, summary.MemoryUsage), summary.DiskUsage);
            summary.LoadLevel = maxUsage switch
            {
                < 30 => SystemLoadLevel.Low,
                < 60 => SystemLoadLevel.Medium,
                < 85 => SystemLoadLevel.High,
                _ => SystemLoadLevel.Critical
            };
            
            // 计算性能评分
            summary.PerformanceScore = Math.Round(100 - maxUsage, 2);
            
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统性能摘要时发生错误");
            return new SystemPerformanceSummary();
        }
    }

    /// <summary>
    /// 开始性能监控
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        try
        {
            if (_isMonitoring)
                return;
            
            _isMonitoring = true;
            _logger.LogInformation("性能监控已启动");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动性能监控时发生错误");
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 停止性能监控
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        try
        {
            if (!_isMonitoring)
                return;
            
            _isMonitoring = false;
            _logger.LogInformation("性能监控已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止性能监控时发生错误");
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        
        try
        {
            lock (_lockObject)
            {
                foreach (var counter in _counters.Values)
                {
                    counter?.Dispose();
                }
                _counters.Clear();
            }
            
            _disposed = true;
            _logger.LogInformation("性能计数器服务已释放资源");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放性能计数器服务资源时发生错误");
        }
    }
}