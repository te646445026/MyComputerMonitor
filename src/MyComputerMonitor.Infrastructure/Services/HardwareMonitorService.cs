using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Events;
using MyComputerMonitor.Core.Interfaces;
using MyComputerMonitor.Core.Models;
using System.Collections.Concurrent;
using System.Management;
using System.Runtime.InteropServices;
using Timer = System.Threading.Timer;

namespace MyComputerMonitor.Infrastructure.Services;

/// <summary>
/// Windows API 内存状态结构
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MEMORYSTATUSEX
{
    public uint dwLength;
    public uint dwMemoryLoad;
    public ulong ullTotalPhys;
    public ulong ullAvailPhys;
    public ulong ullTotalPageFile;
    public ulong ullAvailPageFile;
    public ulong ullTotalVirtual;
    public ulong ullAvailVirtual;
    public ulong ullAvailExtendedVirtual;
}

/// <summary>
/// 硬件监控服务实现
/// </summary>
public class HardwareMonitorService : IHardwareMonitorService, IDisposable
{
    private readonly ILogger<HardwareMonitorService> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IEnhancedNetworkMonitorService? _enhancedNetworkMonitorService;
    private readonly Computer _computer;
    private readonly Timer _monitoringTimer;
    private readonly ConcurrentDictionary<string, HardwareInfo> _hardwareCache;
    private readonly object _lockObject = new();
    
    private bool _isInitialized;
    private bool _isMonitoring;
    private bool _disposed;
    private TimeSpan _monitoringInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Windows API 获取内存状态
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    /// <summary>
    /// 硬件数据更新事件
    /// </summary>
    public event EventHandler<HardwareDataUpdatedEventArgs>? HardwareDataUpdated;
    
    /// <summary>
    /// 硬件状态变化事件
    /// </summary>
    public event EventHandler<HardwareStatusChangedEventArgs>? HardwareStatusChanged;

    /// <summary>
    /// 是否正在监控
    /// </summary>
    public bool IsMonitoring => _isMonitoring;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="configurationService">配置服务</param>
    /// <param name="enhancedNetworkMonitorService">增强网络监控服务（可选）</param>
    public HardwareMonitorService(
        ILogger<HardwareMonitorService> logger,
        IConfigurationService configurationService,
        IEnhancedNetworkMonitorService? enhancedNetworkMonitorService = null)
    {
        _logger = logger;
        _configurationService = configurationService;
        _enhancedNetworkMonitorService = enhancedNetworkMonitorService;
        _hardwareCache = new ConcurrentDictionary<string, HardwareInfo>();
        
        // 初始化LibreHardwareMonitor Computer对象
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true  // 启用存储监控（仅温度）
        };
        
        // 创建定时器但不启动
        _monitoringTimer = new Timer(OnMonitoringTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        
        _logger.LogInformation("硬件监控服务已创建");
    }

    /// <summary>
    /// 初始化硬件监控服务
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("硬件监控服务已经初始化");
            return;
        }

        try
        {
            _logger.LogInformation("正在初始化硬件监控服务...");
            
            // 在后台线程中打开Computer以避免阻塞UI
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    _computer.Open();
                    _computer.Accept(new UpdateVisitor());
                }
            });

            // 加载配置
            var config = _configurationService.GetMonitoringConfiguration();
            _monitoringInterval = TimeSpan.FromMilliseconds(config.MonitoringInterval);
            
            // 预热网络监控服务
            if (_enhancedNetworkMonitorService != null)
            {
                try
                {
                    await _enhancedNetworkMonitorService.WarmupAsync();
                    _logger.LogDebug("网络监控服务预热完成");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "网络监控服务预热失败");
                }
            }
            
            // 初始化硬件缓存
            await RefreshHardwareDataAsync();
            
            _isInitialized = true;
            _logger.LogInformation("硬件监控服务初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化硬件监控服务时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 启动硬件监控
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        if (_isMonitoring)
        {
            _logger.LogWarning("硬件监控已经在运行");
            return;
        }

        try
        {
            _logger.LogInformation("启动硬件监控...");
            
            _isMonitoring = true;
            _monitoringTimer.Change(TimeSpan.Zero, _monitoringInterval);
            
            _logger.LogInformation($"硬件监控已启动，监控间隔: {_monitoringInterval.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动硬件监控时发生错误");
            _isMonitoring = false;
            throw;
        }
    }

    /// <summary>
    /// 停止硬件监控
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
        {
            _logger.LogWarning("硬件监控未在运行");
            return;
        }

        try
        {
            _logger.LogInformation("停止硬件监控...");
            
            _isMonitoring = false;
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            _logger.LogInformation("硬件监控已停止");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止硬件监控时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取当前硬件数据
    /// </summary>
    public async Task<SystemHardwareData> GetHardwareDataAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        await RefreshHardwareDataAsync();
        
        var systemData = new SystemHardwareData
        {
            LastUpdated = DateTime.Now,
            SystemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
        };

        // 从缓存中获取各类硬件信息
        foreach (var hardware in _hardwareCache.Values)
        {
            switch (hardware)
            {
                case CpuInfo cpu:
                    systemData.Cpus.Add(cpu);
                    break;
                case GpuInfo gpu:
                    systemData.Gpus.Add(gpu);
                    break;
                case MemoryInfo memory:
                    systemData.Memory = memory;
                    break;
                case MotherboardInfo motherboard:
                    systemData.Motherboard = motherboard;
                    break;
                case StorageInfo storage:
                    systemData.StorageDevices.Add(storage);
                    break;
                case NetworkInfo network:
                    // 如果有增强网络监控服务，则跳过LibreHardwareMonitor的网络信息
                    if (_enhancedNetworkMonitorService == null)
                    {
                        systemData.NetworkAdapters.Add(network);
                    }
                    break;
                case FanInfo fan:
                    systemData.Fans.Add(fan);
                    break;
            }
        }

        // 使用增强网络监控服务获取物理网络适配器信息
        if (_enhancedNetworkMonitorService != null)
        {
            try
            {
                var physicalAdapters = await _enhancedNetworkMonitorService.GetPhysicalNetworkAdaptersAsync();
                systemData.NetworkAdapters.AddRange(physicalAdapters);
                _logger.LogDebug($"通过增强网络监控服务获取到 {physicalAdapters.Count} 个物理网络适配器");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "使用增强网络监控服务获取网络适配器信息失败，回退到默认方式");
                // 回退到使用缓存中的网络信息
                foreach (var hardware in _hardwareCache.Values.OfType<NetworkInfo>())
                {
                    systemData.NetworkAdapters.Add(hardware);
                }
            }
        }

        return systemData;
    }

    /// <summary>
    /// 获取指定类型的硬件信息
    /// </summary>
    public async Task<IEnumerable<T>> GetHardwareInfoAsync<T>() where T : HardwareInfo
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        await RefreshHardwareDataAsync();
        
        return _hardwareCache.Values.OfType<T>();
    }

    /// <summary>
    /// 刷新硬件数据
    /// </summary>
    public async Task RefreshAsync()
    {
        await RefreshHardwareDataAsync();
    }

    /// <summary>
    /// 设置监控间隔
    /// </summary>
    public void SetMonitoringInterval(TimeSpan interval)
    {
        _monitoringInterval = interval;
        
        if (_isMonitoring)
        {
            _monitoringTimer.Change(TimeSpan.Zero, _monitoringInterval);
        }
        
        _logger.LogInformation($"监控间隔已设置为: {interval.TotalMilliseconds}ms");
    }

    /// <summary>
    /// 刷新硬件数据的内部实现
    /// </summary>
    private async Task RefreshHardwareDataAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    // 更新所有硬件传感器数据
                    _computer.Accept(new UpdateVisitor());
                    
                    // 处理每个硬件设备
                    foreach (var hardware in _computer.Hardware)
                    {
                        ProcessHardware(hardware);
                        
                        // 处理子硬件（如CPU核心）
                        foreach (var subHardware in hardware.SubHardware)
                        {
                            ProcessHardware(subHardware);
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新硬件数据时发生错误");
        }
    }

    /// <summary>
    /// 处理单个硬件设备
    /// </summary>
    private void ProcessHardware(IHardware hardware)
    {
        try
        {
            var hardwareInfo = ConvertToHardwareInfo(hardware);
            if (hardwareInfo != null)
            {
                var oldHardware = _hardwareCache.GetValueOrDefault(hardware.Identifier.ToString());
                _hardwareCache.AddOrUpdate(hardware.Identifier.ToString(), hardwareInfo, (key, old) => hardwareInfo);
                
                // 检查硬件状态变化
                CheckHardwareStatusChanges(oldHardware, hardwareInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"处理硬件 {hardware.Name} 时发生错误");
        }
    }

    /// <summary>
    /// 将LibreHardwareMonitor的IHardware转换为我们的HardwareInfo
    /// </summary>
    private HardwareInfo? ConvertToHardwareInfo(IHardware hardware)
    {
        HardwareInfo? result = hardware.HardwareType switch
        {
            LibreHardwareMonitor.Hardware.HardwareType.Cpu => CreateCpuInfo(hardware),
            LibreHardwareMonitor.Hardware.HardwareType.GpuNvidia or 
            LibreHardwareMonitor.Hardware.HardwareType.GpuAmd or 
            LibreHardwareMonitor.Hardware.HardwareType.GpuIntel => CreateGpuInfo(hardware),
            LibreHardwareMonitor.Hardware.HardwareType.Memory => CreateMemoryInfo(hardware),
            LibreHardwareMonitor.Hardware.HardwareType.Motherboard => CreateMotherboardInfo(hardware),
            LibreHardwareMonitor.Hardware.HardwareType.Storage => CreateStorageInfo(hardware),
            LibreHardwareMonitor.Hardware.HardwareType.Network => CreateNetworkInfo(hardware),
            _ => null
        };

        if (result != null)
        {
            result.Name = hardware.Name;
            result.Identifier = hardware.Identifier.ToString();
            result.LastUpdated = DateTime.Now;
            result.IsOnline = true;
            
            // 添加传感器数据
            foreach (var sensor in hardware.Sensors)
            {
                var sensorData = ConvertToSensorData(sensor);
                if (sensorData != null)
                {
                    result.Sensors.Add(sensorData);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 创建CPU信息
    /// </summary>
    private CpuInfo CreateCpuInfo(IHardware hardware)
    {
        var cpuInfo = new CpuInfo();
        
        // 设置CPU基本信息
        cpuInfo.Name = hardware.Name;
        cpuInfo.Model = hardware.Name;
        
        // 获取CPU架构信息
        try
        {
            cpuInfo.Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            
            // 获取CPU核心数和线程数（使用Environment.ProcessorCount作为基础）
            var processorCount = Environment.ProcessorCount;
            cpuInfo.CoreCount = processorCount;
            cpuInfo.ThreadCount = processorCount;
            
            // 从硬件名称中尝试提取制造商信息
            if (hardware.Name.ToLower().Contains("intel"))
            {
                cpuInfo.Manufacturer = "Intel";
            }
            else if (hardware.Name.ToLower().Contains("amd"))
            {
                cpuInfo.Manufacturer = "AMD";
            }
            else
            {
                cpuInfo.Manufacturer = "未知";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取CPU基本信息失败");
            cpuInfo.Architecture = "未知";
            cpuInfo.CoreCount = Environment.ProcessorCount;
            cpuInfo.ThreadCount = Environment.ProcessorCount;
            cpuInfo.Manufacturer = "未知";
        }
        
        // 初始化核心使用率和温度列表
        cpuInfo.CoreUsages = new List<double>();
        cpuInfo.CoreTemperatures = new List<double>();
        
        // 从传感器中提取CPU特定信息
        foreach (var sensor in hardware.Sensors)
        {
            if (!sensor.Value.HasValue) continue;
            
            switch (sensor.SensorType)
            {
                case LibreHardwareMonitor.Hardware.SensorType.Load:
                    if (sensor.Name.Contains("CPU Core #"))
                    {
                        // 提取核心使用率
                        cpuInfo.CoreUsages.Add(sensor.Value.Value);
                    }
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Temperature:
                    if (sensor.Name.Contains("CPU Core #"))
                    {
                        // 提取核心温度
                        cpuInfo.CoreTemperatures.Add(sensor.Value.Value);
                    }
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Clock:
                    if (sensor.Name.Contains("CPU Core #") && cpuInfo.BaseFrequency == 0)
                    {
                        // 设置基础频率（使用第一个核心的频率）
                        cpuInfo.BaseFrequency = sensor.Value.Value;
                    }
                    break;
            }
        }
        
        // 如果没有获取到核心使用率数据，创建默认数据
        if (cpuInfo.CoreUsages.Count == 0)
        {
            for (int i = 0; i < cpuInfo.CoreCount; i++)
            {
                cpuInfo.CoreUsages.Add(0.0);
            }
        }
        
        // 如果没有获取到核心温度数据，创建默认数据
        if (cpuInfo.CoreTemperatures.Count == 0)
        {
            for (int i = 0; i < cpuInfo.CoreCount; i++)
            {
                cpuInfo.CoreTemperatures.Add(0.0);
            }
        }
        
        return cpuInfo;
    }

    /// <summary>
    /// 创建GPU信息
    /// </summary>
    private GpuInfo CreateGpuInfo(IHardware hardware)
    {
        var gpuInfo = new GpuInfo
        {
            Name = hardware.Name,
            Model = hardware.Name
        };
        
        // 从传感器获取GPU数据并添加到Sensors集合
        foreach (var sensor in hardware.Sensors)
        {
            if (!sensor.Value.HasValue) continue;

            SensorData? sensorData = null;

            switch (sensor.SensorType)
            {
                case LibreHardwareMonitor.Hardware.SensorType.Load when 
                    sensor.Name.Contains("GPU Core") || sensor.Name.Contains("GPU") || sensor.Name.Contains("Core"):
                    sensorData = new SensorData
                    {
                        Name = "GPU Usage",
                        Type = Core.Models.SensorType.Usage,
                        Value = sensor.Value.Value,
                        Unit = "%",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = sensor.Identifier.ToString()
                    };
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Temperature when 
                    sensor.Name.Contains("GPU Core") || sensor.Name.Contains("GPU") || sensor.Name.Contains("Core"):
                    sensorData = new SensorData
                    {
                        Name = "GPU Temperature",
                        Type = Core.Models.SensorType.Temperature,
                        Value = sensor.Value.Value,
                        Unit = "°C",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = sensor.Identifier.ToString()
                    };
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Clock when 
                    sensor.Name.Contains("GPU Core") || sensor.Name.Contains("Core") || sensor.Name.Contains("Graphics"):
                    sensorData = new SensorData
                    {
                        Name = "GPU Core Clock",
                        Type = Core.Models.SensorType.Frequency,
                        Value = sensor.Value.Value,
                        Unit = "MHz",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = sensor.Identifier.ToString()
                    };
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Clock when 
                    sensor.Name.Contains("GPU Memory") || sensor.Name.Contains("Memory") || sensor.Name.Contains("VRAM"):
                    gpuInfo.MemoryClock = sensor.Value.Value;
                    sensorData = new SensorData
                    {
                        Name = "GPU Memory Clock",
                        Type = Core.Models.SensorType.Frequency,
                        Value = sensor.Value.Value,
                        Unit = "MHz",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = sensor.Identifier.ToString()
                    };
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Power when 
                    sensor.Name.Contains("GPU") || sensor.Name.Contains("Power") || sensor.Name.Contains("Total"):
                    sensorData = new SensorData
                    {
                        Name = "GPU Power",
                        Type = Core.Models.SensorType.Power,
                        Value = sensor.Value.Value,
                        Unit = "W",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = sensor.Identifier.ToString()
                    };
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Fan when 
                    sensor.Name.Contains("GPU") || sensor.Name.Contains("Fan"):
                    sensorData = new SensorData
                    {
                        Name = "GPU Fan",
                        Type = Core.Models.SensorType.Fan,
                        Value = sensor.Value.Value,
                        Unit = "RPM",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = sensor.Identifier.ToString()
                    };
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.SmallData when sensor.Name.Contains("GPU Memory Used"):
                    gpuInfo.MemoryUsed = (long)(sensor.Value.Value * 1024 * 1024); // 转换为字节
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.SmallData when sensor.Name.Contains("GPU Memory Total"):
                    gpuInfo.MemoryTotal = (long)(sensor.Value.Value * 1024 * 1024); // 转换为字节
                    break;
            }

            if (sensorData != null)
            {
                gpuInfo.Sensors.Add(sensorData);
            }
        }

        // 尝试获取GPU驱动版本和其他静态信息
        try
        {
            // 从硬件名称中提取制造商信息
            if (hardware.Name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
            {
                gpuInfo.Manufacturer = "NVIDIA";
            }
            else if (hardware.Name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || 
                     hardware.Name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
            {
                gpuInfo.Manufacturer = "AMD";
            }
            else if (hardware.Name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            {
                gpuInfo.Manufacturer = "Intel";
            }

            // 尝试通过WMI获取GPU驱动版本
            gpuInfo.DriverVersion = GetGpuDriverVersion(hardware.Name);

            // 如果没有获取到显存总量，尝试从其他传感器获取
            if (gpuInfo.MemoryTotal == 0)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.Name.Contains("Memory") && sensor.SensorType == LibreHardwareMonitor.Hardware.SensorType.Data)
                    {
                        if (sensor.Name.Contains("Total") || sensor.Name.Contains("Size"))
                        {
                            gpuInfo.MemoryTotal = (long)(sensor.Value ?? 0) * 1024 * 1024; // MB转字节
                            break;
                        }
                    }
                }
            }

            // 设置基础时钟（如果可用）
            var coreClockSensor = gpuInfo.GetSensor(Core.Models.SensorType.Frequency);
            if (coreClockSensor != null && coreClockSensor.Value > 0)
            {
                gpuInfo.BaseCoreClock = coreClockSensor.Value; // 当前时钟作为基础时钟的近似值
            }
            
            if (gpuInfo.MemoryClock > 0)
            {
                gpuInfo.BaseMemoryClock = gpuInfo.MemoryClock; // 当前时钟作为基础时钟的近似值
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"获取GPU {hardware.Name} 的额外信息时发生错误");
        }
        
        return gpuInfo;
    }

    /// <summary>
    /// 创建内存信息
    /// </summary>
    private MemoryInfo CreateMemoryInfo(IHardware hardware)
    {
        var memoryInfo = new MemoryInfo();
        
        // 从传感器获取内存数据
        foreach (var sensor in hardware.Sensors)
        {
            if (!sensor.Value.HasValue) continue;

            switch (sensor.SensorType)
            {
                case LibreHardwareMonitor.Hardware.SensorType.Load when sensor.Name.Contains("Memory"):
                    // 通过添加传感器数据来设置使用率
                    var usageSensor = new SensorData
                    {
                        Name = sensor.Name,
                        Type = Core.Models.SensorType.Usage,
                        Value = sensor.Value.Value,
                        Unit = "%",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = sensor.Identifier.ToString()
                    };
                    memoryInfo.Sensors.Add(usageSensor);
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Data when sensor.Name.Contains("Used"):
                    memoryInfo.UsedMemory = (long)(sensor.Value.Value * 1024); // 转换为MB
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Data when sensor.Name.Contains("Available"):
                    memoryInfo.AvailableMemory = (long)(sensor.Value.Value * 1024); // 转换为MB
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Data when sensor.Name.Contains("Total"):
                    memoryInfo.TotalMemory = (long)(sensor.Value.Value * 1024); // 转换为MB
                    break;
            }
        }

        // 如果没有从传感器获取到数据，使用系统API获取
        if (memoryInfo.TotalMemory == 0)
        {
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf(memStatus);
                
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    memoryInfo.TotalMemory = (long)(memStatus.ullTotalPhys / (1024 * 1024)); // 转换为MB
                    memoryInfo.AvailableMemory = (long)(memStatus.ullAvailPhys / (1024 * 1024)); // 转换为MB
                    memoryInfo.UsedMemory = memoryInfo.TotalMemory - memoryInfo.AvailableMemory;
                    
                    // 添加使用率传感器数据
                    var usagePercentage = memoryInfo.TotalMemory > 0 ? 
                        (double)memoryInfo.UsedMemory / memoryInfo.TotalMemory * 100 : 0;
                    
                    var usageSensor = new SensorData
                    {
                        Name = "Memory Usage",
                        Type = Core.Models.SensorType.Usage,
                        Value = usagePercentage,
                        Unit = "%",
                        IsValid = true,
                        LastUpdated = DateTime.Now,
                        Identifier = "memory/usage"
                    };
                    memoryInfo.Sensors.Add(usageSensor);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取系统内存信息失败");
            }
        }

        // 获取详细的内存信息（频率、类型等）
        try
        {
            GetDetailedMemoryInfo(memoryInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取详细内存信息失败");
        }
        
        return memoryInfo;
    }

    /// <summary>
    /// 获取详细的内存信息
    /// </summary>
    private void GetDetailedMemoryInfo(MemoryInfo memoryInfo)
    {
        try
        {
            // 获取物理内存信息
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            using var collection = searcher.Get();
            
            var modules = new List<MemoryModuleInfo>();
            double totalFrequency = 0;
            int moduleCount = 0;
            string memoryType = "";

            foreach (ManagementObject obj in collection)
            {
                var module = new MemoryModuleInfo();
                
                // 容量 (字节转换为MB)
                if (obj["Capacity"] != null)
                {
                    module.Capacity = Convert.ToInt64(obj["Capacity"]) / (1024 * 1024);
                }
                
                // 频率
                if (obj["Speed"] != null)
                {
                    module.Frequency = Convert.ToDouble(obj["Speed"]);
                    totalFrequency += module.Frequency;
                    moduleCount++;
                }
                
                // 制造商
                module.Manufacturer = obj["Manufacturer"]?.ToString() ?? "";
                
                // 型号
                module.Model = obj["PartNumber"]?.ToString() ?? "";
                
                // 插槽位置
                module.SlotLocation = obj["DeviceLocator"]?.ToString() ?? "";
                
                // 内存类型
                if (obj["MemoryType"] != null)
                {
                    var typeCode = Convert.ToInt32(obj["MemoryType"]);
                    module.MemoryType = GetMemoryTypeString(typeCode);
                    if (string.IsNullOrEmpty(memoryType))
                    {
                        memoryType = module.MemoryType;
                    }
                }
                
                // 如果有SMBIOSMemoryType，优先使用它
                if (obj["SMBIOSMemoryType"] != null)
                {
                    var smbiosType = Convert.ToInt32(obj["SMBIOSMemoryType"]);
                    var smbiosTypeString = GetSMBIOSMemoryTypeString(smbiosType);
                    if (!string.IsNullOrEmpty(smbiosTypeString))
                    {
                        module.MemoryType = smbiosTypeString;
                        memoryType = smbiosTypeString;
                    }
                }
                
                module.Name = $"{module.Manufacturer} {module.Model}".Trim();
                modules.Add(module);
            }

            // 设置内存信息
            memoryInfo.Modules = modules;
            memoryInfo.SlotCount = GetMemorySlotCount();
            memoryInfo.UsedSlots = modules.Count;
            memoryInfo.MemoryType = memoryType;
            
            // 计算平均频率
            if (moduleCount > 0)
            {
                memoryInfo.Frequency = totalFrequency / moduleCount;
            }

            // 获取系统内存使用情况
            GetSystemMemoryUsage(memoryInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取WMI内存信息失败");
        }
    }

    /// <summary>
    /// 获取内存插槽总数
    /// </summary>
    private int GetMemorySlotCount()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemoryArray");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                if (obj["MemoryDevices"] != null)
                {
                    return Convert.ToInt32(obj["MemoryDevices"]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取内存插槽数量失败");
        }
        
        return 0;
    }

    /// <summary>
    /// 获取系统内存使用情况
    /// </summary>
    private void GetSystemMemoryUsage(MemoryInfo memoryInfo)
    {
        try
        {
            // 获取性能计数器信息
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfRawData_PerfOS_Memory");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                // 缓存内存 (字节转换为MB)
                if (obj["CacheBytes"] != null)
                {
                    memoryInfo.CachedMemory = Convert.ToInt64(obj["CacheBytes"]) / (1024 * 1024);
                }
                
                // 系统缓存驻留字节
                if (obj["SystemCacheResidentBytes"] != null)
                {
                    var systemCache = Convert.ToInt64(obj["SystemCacheResidentBytes"]) / (1024 * 1024);
                    memoryInfo.CachedMemory = Math.Max(memoryInfo.CachedMemory, systemCache);
                }
                
                // 已提交字节
                if (obj["CommittedBytes"] != null)
                {
                    // 这个值通常很大，我们使用已用内存作为已提交内存的近似值
                }
                
                break; // 只需要第一个对象
            }

            // 获取页面文件信息
            using var pageFileSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PageFileUsage");
            using var pageFileCollection = pageFileSearcher.Get();
            
            foreach (ManagementObject obj in pageFileCollection)
            {
                if (obj["CurrentUsage"] != null)
                {
                    memoryInfo.SwapUsed += Convert.ToInt64(obj["CurrentUsage"]);
                }
                
                if (obj["AllocatedBaseSize"] != null)
                {
                    memoryInfo.SwapTotal += Convert.ToInt64(obj["AllocatedBaseSize"]);
                }
            }

            // 如果缓存内存为0，使用一个估算值
            if (memoryInfo.CachedMemory == 0)
            {
                // 通常缓存内存约为可用内存的一部分
                memoryInfo.CachedMemory = memoryInfo.AvailableMemory / 4;
            }

            // 缓冲内存通常是系统保留的一小部分
            memoryInfo.BufferedMemory = memoryInfo.TotalMemory / 100; // 约1%作为估算
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取系统内存使用情况失败");
        }
    }

    /// <summary>
    /// 获取内存类型字符串
    /// </summary>
    private string GetMemoryTypeString(int typeCode)
    {
        return typeCode switch
        {
            20 => "DDR",
            21 => "DDR2",
            22 => "DDR2 FB-DIMM",
            24 => "DDR3",
            26 => "DDR4",
            34 => "DDR5",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// 获取SMBIOS内存类型字符串
    /// </summary>
    private string GetSMBIOSMemoryTypeString(int typeCode)
    {
        return typeCode switch
        {
            26 => "DDR4",
            34 => "DDR5",
            24 => "DDR3",
            21 => "DDR2",
            20 => "DDR",
            _ => ""
        };
    }



    /// <summary>
    /// 创建主板信息
    /// </summary>
    private MotherboardInfo CreateMotherboardInfo(IHardware hardware)
    {
        var motherboardInfo = new MotherboardInfo();
        
        // 主板特定信息将通过传感器数据获取
        return motherboardInfo;
    }

    /// <summary>
    /// 创建存储设备信息（仅温度监控）
    /// </summary>
    private StorageInfo CreateStorageInfo(IHardware hardware)
    {
        var storageInfo = new StorageInfo
        {
            Name = hardware.Name,
            Identifier = hardware.Identifier.ToString(),
            LastUpdated = DateTime.Now,
            IsOnline = true,
            Type = Core.Models.HardwareType.Storage
        };
        
        // 检查是否有温度传感器
        var temperatureSensors = hardware.Sensors
            .Where(s => s.SensorType == LibreHardwareMonitor.Hardware.SensorType.Temperature)
            .ToList();
        
        storageInfo.HasTemperatureSensor = temperatureSensors.Any();
        
        // 获取温度值
        if (storageInfo.HasTemperatureSensor)
        {
            var tempSensor = temperatureSensors.FirstOrDefault(s => s.Value.HasValue);
            if (tempSensor != null)
            {
                storageInfo.Temperature = tempSensor.Value.Value;
            }
        }
        
        return storageInfo;
    }

    /// <summary>
    /// 创建网络信息
    /// </summary>
    private NetworkInfo CreateNetworkInfo(IHardware hardware)
    {
        var networkInfo = new NetworkInfo
        {
            Name = hardware.Name,
            Identifier = hardware.Identifier.ToString(),
            LastUpdated = DateTime.Now,
            IsOnline = true
        };
        
        // 从传感器获取网络数据
        foreach (var sensor in hardware.Sensors)
        {
            if (!sensor.Value.HasValue) continue;

            switch (sensor.SensorType)
            {
                case LibreHardwareMonitor.Hardware.SensorType.Throughput:
                    if (sensor.Name.Contains("Download") || sensor.Name.Contains("Received"))
                    {
                        networkInfo.DownloadSpeed = sensor.Value.Value / (1024 * 1024); // 转换为MB/s
                    }
                    else if (sensor.Name.Contains("Upload") || sensor.Name.Contains("Sent"))
                    {
                        networkInfo.UploadSpeed = sensor.Value.Value / (1024 * 1024); // 转换为MB/s
                    }
                    break;
                case LibreHardwareMonitor.Hardware.SensorType.Load:
                    if (sensor.Name.Contains("Usage") || sensor.Name.Contains("Utilization"))
                    {
                        // 网络使用率传感器会通过基类的Sensors集合自动添加
                    }
                    break;
            }
        }
        
        return networkInfo;
    }

    /// <summary>
    /// 将LibreHardwareMonitor的ISensor转换为我们的SensorData
    /// </summary>
    private SensorData? ConvertToSensorData(ISensor sensor)
    {
        if (!sensor.Value.HasValue)
            return null;

        var sensorType = sensor.SensorType switch
        {
            LibreHardwareMonitor.Hardware.SensorType.Temperature => Core.Models.SensorType.Temperature,
            LibreHardwareMonitor.Hardware.SensorType.Load => Core.Models.SensorType.Usage,
            LibreHardwareMonitor.Hardware.SensorType.Clock => Core.Models.SensorType.Frequency,
            LibreHardwareMonitor.Hardware.SensorType.Voltage => Core.Models.SensorType.Voltage,
            LibreHardwareMonitor.Hardware.SensorType.Current => Core.Models.SensorType.Current,
            LibreHardwareMonitor.Hardware.SensorType.Power => Core.Models.SensorType.Power,
            LibreHardwareMonitor.Hardware.SensorType.Fan => Core.Models.SensorType.Fan,
            LibreHardwareMonitor.Hardware.SensorType.Flow => Core.Models.SensorType.Flow,
            LibreHardwareMonitor.Hardware.SensorType.Control => Core.Models.SensorType.Control,
            LibreHardwareMonitor.Hardware.SensorType.Level => Core.Models.SensorType.Level,
            LibreHardwareMonitor.Hardware.SensorType.Factor => Core.Models.SensorType.Factor,
            LibreHardwareMonitor.Hardware.SensorType.Data => Core.Models.SensorType.Data,
            LibreHardwareMonitor.Hardware.SensorType.SmallData => Core.Models.SensorType.SmallData,
            LibreHardwareMonitor.Hardware.SensorType.Throughput => Core.Models.SensorType.Throughput,
            _ => Core.Models.SensorType.Data
        };

        return new SensorData
        {
            Name = sensor.Name,
            Type = sensorType,
            Value = sensor.Value.Value,
            MinValue = sensor.Min,
            MaxValue = sensor.Max,
            Unit = GetSensorUnit(sensor.SensorType),
            IsValid = sensor.Value.HasValue,
            LastUpdated = DateTime.Now,
            Identifier = sensor.Identifier.ToString()
        };
    }

    /// <summary>
    /// 获取传感器单位
    /// </summary>
    private string GetSensorUnit(LibreHardwareMonitor.Hardware.SensorType sensorType)
    {
        return sensorType switch
        {
            LibreHardwareMonitor.Hardware.SensorType.Temperature => "°C",
            LibreHardwareMonitor.Hardware.SensorType.Load => "%",
            LibreHardwareMonitor.Hardware.SensorType.Clock => "MHz",
            LibreHardwareMonitor.Hardware.SensorType.Voltage => "V",
            LibreHardwareMonitor.Hardware.SensorType.Current => "A",
            LibreHardwareMonitor.Hardware.SensorType.Power => "W",
            LibreHardwareMonitor.Hardware.SensorType.Fan => "RPM",
            LibreHardwareMonitor.Hardware.SensorType.Flow => "L/h",
            LibreHardwareMonitor.Hardware.SensorType.Control => "%",
            LibreHardwareMonitor.Hardware.SensorType.Level => "%",
            LibreHardwareMonitor.Hardware.SensorType.Factor => "",
            LibreHardwareMonitor.Hardware.SensorType.Data => "GB",
            LibreHardwareMonitor.Hardware.SensorType.SmallData => "MB",
            LibreHardwareMonitor.Hardware.SensorType.Throughput => "B/s",
            _ => ""
        };
    }

    /// <summary>
    /// 检查硬件状态变化
    /// </summary>
    private void CheckHardwareStatusChanges(HardwareInfo? oldHardware, HardwareInfo newHardware)
    {
        if (oldHardware == null) return;

        var config = _configurationService.GetMonitoringConfiguration();
        
        // 检查温度变化
        CheckTemperatureChanges(oldHardware, newHardware, config);
        
        // 检查使用率变化
        CheckUsageChanges(oldHardware, newHardware, config);
    }

    /// <summary>
    /// 检查温度变化
    /// </summary>
    private void CheckTemperatureChanges(HardwareInfo oldHardware, HardwareInfo newHardware, MonitoringConfiguration config)
    {
        var oldTemp = oldHardware.GetSensor(Core.Models.SensorType.Temperature)?.Value ?? 0;
        var newTemp = newHardware.GetSensor(Core.Models.SensorType.Temperature)?.Value ?? 0;

        if (Math.Abs(newTemp - oldTemp) < 1) return; // 温度变化小于1度则忽略

        var (warningThreshold, criticalThreshold) = newHardware.Type switch
        {
            Core.Models.HardwareType.CPU => (config.CpuTemperatureWarningThreshold, config.CpuTemperatureCriticalThreshold),
            Core.Models.HardwareType.GPU => (config.GpuTemperatureWarningThreshold, config.GpuTemperatureCriticalThreshold),
            _ => (75.0, 85.0)
        };

        HardwareStatusChangeType? changeType = null;
        string description = "";

        if (newTemp >= criticalThreshold && oldTemp < criticalThreshold)
        {
            changeType = HardwareStatusChangeType.TemperatureCritical;
            description = $"{newHardware.Name} 温度达到危险水平: {newTemp:F1}°C";
        }
        else if (newTemp >= warningThreshold && oldTemp < warningThreshold)
        {
            changeType = HardwareStatusChangeType.TemperatureWarning;
            description = $"{newHardware.Name} 温度过高: {newTemp:F1}°C";
        }
        else if (newTemp < warningThreshold && oldTemp >= warningThreshold)
        {
            changeType = HardwareStatusChangeType.Normal;
            description = $"{newHardware.Name} 温度恢复正常: {newTemp:F1}°C";
        }

        if (changeType.HasValue)
        {
            HardwareStatusChanged?.Invoke(this, new HardwareStatusChangedEventArgs(newHardware, changeType.Value, description));
        }
    }

    /// <summary>
    /// 检查使用率变化
    /// </summary>
    private void CheckUsageChanges(HardwareInfo oldHardware, HardwareInfo newHardware, MonitoringConfiguration config)
    {
        var oldUsage = oldHardware.GetSensor(Core.Models.SensorType.Usage)?.Value ?? 0;
        var newUsage = newHardware.GetSensor(Core.Models.SensorType.Usage)?.Value ?? 0;

        if (Math.Abs(newUsage - oldUsage) < 5) return; // 使用率变化小于5%则忽略

        var (warningThreshold, criticalThreshold) = newHardware.Type switch
        {
            Core.Models.HardwareType.Memory => (config.MemoryUsageWarningThreshold, config.MemoryUsageCriticalThreshold),
            _ => (80.0, 95.0)
        };

        HardwareStatusChangeType? changeType = null;
        string description = "";

        if (newUsage >= criticalThreshold && oldUsage < criticalThreshold)
        {
            changeType = HardwareStatusChangeType.UsageCritical;
            description = $"{newHardware.Name} 使用率达到危险水平: {newUsage:F1}%";
        }
        else if (newUsage >= warningThreshold && oldUsage < warningThreshold)
        {
            changeType = HardwareStatusChangeType.UsageWarning;
            description = $"{newHardware.Name} 使用率过高: {newUsage:F1}%";
        }
        else if (newUsage < warningThreshold && oldUsage >= warningThreshold)
        {
            changeType = HardwareStatusChangeType.Normal;
            description = $"{newHardware.Name} 使用率恢复正常: {newUsage:F1}%";
        }

        if (changeType.HasValue)
        {
            HardwareStatusChanged?.Invoke(this, new HardwareStatusChangedEventArgs(newHardware, changeType.Value, description));
        }
    }

    /// <summary>
    /// 监控定时器回调
    /// </summary>
    private async void OnMonitoringTimerElapsed(object? state)
    {
        if (!_isMonitoring || _disposed) return;

        try
        {
            await RefreshHardwareDataAsync();
            
            var hardwareData = await GetHardwareDataAsync();
            HardwareDataUpdated?.Invoke(this, new HardwareDataUpdatedEventArgs(hardwareData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "监控定时器执行时发生错误");
        }
    }

    /// <summary>
    /// 通过WMI获取GPU驱动版本
    /// </summary>
    private string GetGpuDriverVersion(string gpuName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                var name = obj["Name"]?.ToString();
                var driverVersion = obj["DriverVersion"]?.ToString();
                var driverDate = obj["DriverDate"]?.ToString();
                
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(driverVersion))
                {
                    // 检查是否匹配当前GPU
                    if (name.Contains(gpuName, StringComparison.OrdinalIgnoreCase) ||
                        gpuName.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        // 如果有驱动日期，格式化显示
                        if (!string.IsNullOrEmpty(driverDate) && DateTime.TryParse(driverDate, out var date))
                        {
                            return $"{driverVersion} ({date:yyyy-MM-dd})";
                        }
                        return driverVersion;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "通过WMI获取GPU驱动版本失败");
        }
        
        return "未知";
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _isMonitoring = false;
            _monitoringTimer?.Dispose();
            
            lock (_lockObject)
            {
                _computer?.Close();
            }
            
            _hardwareCache.Clear();
            _disposed = true;
            
            _logger.LogInformation("硬件监控服务已释放资源");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放硬件监控服务资源时发生错误");
        }
    }
}

/// <summary>
/// LibreHardwareMonitor更新访问者
/// </summary>
internal class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var subHardware in hardware.SubHardware)
        {
            subHardware.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}