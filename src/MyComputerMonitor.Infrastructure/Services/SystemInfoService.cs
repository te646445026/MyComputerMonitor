using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace MyComputerMonitor.Infrastructure.Services;

/// <summary>
/// 系统信息服务接口
/// </summary>
public interface ISystemInfoService
{
    /// <summary>
    /// 获取操作系统信息
    /// </summary>
    /// <returns>操作系统信息</returns>
    Task<OperatingSystemInfo> GetOperatingSystemInfoAsync();
    
    /// <summary>
    /// 获取系统启动时间
    /// </summary>
    /// <returns>系统启动时间</returns>
    Task<DateTime> GetSystemBootTimeAsync();
    
    /// <summary>
    /// 获取系统运行时间
    /// </summary>
    /// <returns>系统运行时间</returns>
    Task<TimeSpan> GetSystemUptimeAsync();
    
    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns>用户信息</returns>
    Task<UserInfo> GetCurrentUserInfoAsync();
    
    /// <summary>
    /// 获取系统性能信息
    /// </summary>
    /// <returns>系统性能信息</returns>
    Task<SystemPerformanceInfo> GetSystemPerformanceInfoAsync();
    
    /// <summary>
    /// 获取系统进程信息
    /// </summary>
    /// <returns>进程信息列表</returns>
    Task<List<ProcessInfo>> GetSystemProcessesAsync();
    
    /// <summary>
    /// 获取系统服务信息
    /// </summary>
    /// <returns>服务信息列表</returns>
    Task<List<ServiceInfo>> GetSystemServicesAsync();
    
    /// <summary>
    /// 获取环境变量
    /// </summary>
    /// <returns>环境变量字典</returns>
    Task<Dictionary<string, string>> GetEnvironmentVariablesAsync();
}

/// <summary>
/// 操作系统信息
/// </summary>
public class OperatingSystemInfo
{
    /// <summary>
    /// 操作系统名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作系统版本
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作系统架构
    /// </summary>
    public string Architecture { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作系统构建号
    /// </summary>
    public string BuildNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作系统安装日期
    /// </summary>
    public DateTime? InstallDate { get; set; }
    
    /// <summary>
    /// 系统目录
    /// </summary>
    public string SystemDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Windows目录
    /// </summary>
    public string WindowsDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// 系统制造商
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;
    
    /// <summary>
    /// 系统型号
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// 系统序列号
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;
}

/// <summary>
/// 用户信息
/// </summary>
public class UserInfo
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// 域名
    /// </summary>
    public string Domain { get; set; } = string.Empty;
    
    /// <summary>
    /// 完整用户名
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否管理员
    /// </summary>
    public bool IsAdministrator { get; set; }
    
    /// <summary>
    /// 用户配置文件路径
    /// </summary>
    public string ProfilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTime? LoginTime { get; set; }
}

/// <summary>
/// 系统性能信息
/// </summary>
public class SystemPerformanceInfo
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
    public double NetworkUsage { get; set; }
    
    /// <summary>
    /// 进程数量
    /// </summary>
    public int ProcessCount { get; set; }
    
    /// <summary>
    /// 线程数量
    /// </summary>
    public int ThreadCount { get; set; }
    
    /// <summary>
    /// 句柄数量
    /// </summary>
    public int HandleCount { get; set; }
}

/// <summary>
/// 进程信息
/// </summary>
public class ProcessInfo
{
    /// <summary>
    /// 进程ID
    /// </summary>
    public int ProcessId { get; set; }
    
    /// <summary>
    /// 进程名称
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;
    
    /// <summary>
    /// 进程标题
    /// </summary>
    public string WindowTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// CPU使用率
    /// </summary>
    public double CpuUsage { get; set; }
    
    /// <summary>
    /// 内存使用量（字节）
    /// </summary>
    public long MemoryUsage { get; set; }
    
    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 可执行文件路径
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;
    
    /// <summary>
    /// 进程状态
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 服务信息
/// </summary>
public class ServiceInfo
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 服务状态
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// 启动类型
    /// </summary>
    public string StartType { get; set; } = string.Empty;
    
    /// <summary>
    /// 服务描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 可执行文件路径
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;
}

/// <summary>
/// 系统信息服务实现
/// </summary>
public class SystemInfoService : ISystemInfoService
{
    private readonly ILogger<SystemInfoService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public SystemInfoService(ILogger<SystemInfoService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取操作系统信息
    /// </summary>
    public async Task<OperatingSystemInfo> GetOperatingSystemInfoAsync()
    {
        try
        {
            var osInfo = new OperatingSystemInfo();
            
            // 获取基本系统信息
            var os = Environment.OSVersion;
            osInfo.Version = os.Version.ToString();
            osInfo.Architecture = RuntimeInformation.OSArchitecture.ToString();
            osInfo.SystemDirectory = Environment.SystemDirectory;
            osInfo.WindowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            
            // 使用WMI获取详细信息
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    osInfo.Name = obj["Caption"]?.ToString() ?? string.Empty;
                    osInfo.BuildNumber = obj["BuildNumber"]?.ToString() ?? string.Empty;
                    
                    if (obj["InstallDate"] != null)
                    {
                        var installDateStr = obj["InstallDate"].ToString();
                        if (DateTime.TryParseExact(installDateStr, "yyyyMMddHHmmss.ffffff-000", 
                            null, System.Globalization.DateTimeStyles.None, out var installDate))
                        {
                            osInfo.InstallDate = installDate;
                        }
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取操作系统详细信息时发生错误");
            }
            
            // 获取计算机系统信息
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    osInfo.Manufacturer = obj["Manufacturer"]?.ToString() ?? string.Empty;
                    osInfo.Model = obj["Model"]?.ToString() ?? string.Empty;
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取计算机系统信息时发生错误");
            }
            
            // 获取BIOS信息
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    osInfo.SerialNumber = obj["SerialNumber"]?.ToString() ?? string.Empty;
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取BIOS信息时发生错误");
            }
            
            return osInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作系统信息时发生错误");
            return new OperatingSystemInfo();
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取系统启动时间
    /// </summary>
    public async Task<DateTime> GetSystemBootTimeAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                var bootTimeStr = obj["LastBootUpTime"]?.ToString();
                if (!string.IsNullOrEmpty(bootTimeStr))
                {
                    if (DateTime.TryParseExact(bootTimeStr, "yyyyMMddHHmmss.ffffff-000", 
                        null, System.Globalization.DateTimeStyles.None, out var bootTime))
                    {
                        return bootTime;
                    }
                }
                break;
            }
            
            // 备用方法：使用Environment.TickCount64
            var tickCount = Environment.TickCount64;
            return DateTime.Now.AddMilliseconds(-tickCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统启动时间时发生错误");
            return DateTime.Now;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取系统运行时间
    /// </summary>
    public async Task<TimeSpan> GetSystemUptimeAsync()
    {
        try
        {
            var bootTime = await GetSystemBootTimeAsync();
            return DateTime.Now - bootTime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统运行时间时发生错误");
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    public async Task<UserInfo> GetCurrentUserInfoAsync()
    {
        try
        {
            var userInfo = new UserInfo
            {
                UserName = Environment.UserName,
                Domain = Environment.UserDomainName,
                ProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            
            // 检查是否为管理员
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                userInfo.IsAdministrator = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查管理员权限时发生错误");
            }
            
            // 获取用户详细信息
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_UserAccount WHERE Name='{userInfo.UserName}'");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    userInfo.FullName = obj["FullName"]?.ToString() ?? string.Empty;
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取用户详细信息时发生错误");
            }
            
            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户信息时发生错误");
            return new UserInfo();
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取系统性能信息
    /// </summary>
    public async Task<SystemPerformanceInfo> GetSystemPerformanceInfoAsync()
    {
        try
        {
            var perfInfo = new SystemPerformanceInfo();
            
            // 获取进程和线程数量
            var processes = Process.GetProcesses();
            perfInfo.ProcessCount = processes.Length;
            perfInfo.ThreadCount = processes.Sum(p => 
            {
                try { return p.Threads.Count; }
                catch { return 0; }
            });
            perfInfo.HandleCount = processes.Sum(p => 
            {
                try { return p.HandleCount; }
                catch { return 0; }
            });
            
            // 获取内存使用率
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    var totalMemory = Convert.ToDouble(obj["TotalVisibleMemorySize"]) * 1024;
                    var freeMemory = Convert.ToDouble(obj["FreePhysicalMemory"]) * 1024;
                    perfInfo.MemoryUsage = ((totalMemory - freeMemory) / totalMemory) * 100;
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取内存使用率时发生错误");
            }
            
            return perfInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统性能信息时发生错误");
            return new SystemPerformanceInfo();
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取系统进程信息
    /// </summary>
    public async Task<List<ProcessInfo>> GetSystemProcessesAsync()
    {
        try
        {
            var processList = new List<ProcessInfo>();
            var processes = Process.GetProcesses();
            
            foreach (var process in processes)
            {
                try
                {
                    var processInfo = new ProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        WindowTitle = process.MainWindowTitle,
                        MemoryUsage = process.WorkingSet64,
                        Status = process.Responding ? "运行中" : "无响应"
                    };
                    
                    try
                    {
                        processInfo.StartTime = process.StartTime;
                        processInfo.ExecutablePath = process.MainModule?.FileName ?? string.Empty;
                    }
                    catch
                    {
                        // 某些系统进程可能无法访问这些信息
                    }
                    
                    processList.Add(processInfo);
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
            
            return processList.OrderByDescending(p => p.MemoryUsage).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统进程信息时发生错误");
            return new List<ProcessInfo>();
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取系统服务信息
    /// </summary>
    public async Task<List<ServiceInfo>> GetSystemServicesAsync()
    {
        try
        {
            var serviceList = new List<ServiceInfo>();
            
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                var serviceInfo = new ServiceInfo
                {
                    ServiceName = obj["Name"]?.ToString() ?? string.Empty,
                    DisplayName = obj["DisplayName"]?.ToString() ?? string.Empty,
                    Status = obj["State"]?.ToString() ?? string.Empty,
                    StartType = obj["StartMode"]?.ToString() ?? string.Empty,
                    Description = obj["Description"]?.ToString() ?? string.Empty,
                    ExecutablePath = obj["PathName"]?.ToString() ?? string.Empty
                };
                
                serviceList.Add(serviceInfo);
            }
            
            return serviceList.OrderBy(s => s.DisplayName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统服务信息时发生错误");
            return new List<ServiceInfo>();
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取环境变量
    /// </summary>
    public async Task<Dictionary<string, string>> GetEnvironmentVariablesAsync()
    {
        try
        {
            var envVars = new Dictionary<string, string>();
            
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                var value = entry.Value?.ToString() ?? string.Empty;
                
                if (!string.IsNullOrEmpty(key))
                {
                    envVars[key] = value;
                }
            }
            
            return envVars;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取环境变量时发生错误");
            return new Dictionary<string, string>();
        }
        finally
        {
            await Task.CompletedTask;
        }
    }
}