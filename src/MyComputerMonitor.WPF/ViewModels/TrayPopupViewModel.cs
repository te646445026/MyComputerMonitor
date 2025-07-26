using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Interfaces;

namespace MyComputerMonitor.WPF.ViewModels
{
    /// <summary>
    /// 托盘弹出窗口的ViewModel
    /// </summary>
    public class TrayPopupViewModel : INotifyPropertyChanged
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly ILogger<TrayPopupViewModel> _logger;
        private readonly DispatcherTimer _updateTimer;

        private string _cpuUsage = "0%";
        private string _cpuTemperature = "0°C";
        private string _gpuUsage = "0%";
        private string _gpuTemperature = "0°C";
        private string _memoryUsage = "0%";
        private string _memoryUsed = "0 GB";
        private string _memoryTotal = "0 GB";
        private string _networkDownloadSpeed = "0.00 MB/s";
        private string _networkUploadSpeed = "0.00 MB/s";
        private string _networkUsage = "0%";

        public TrayPopupViewModel(IHardwareMonitorService hardwareMonitorService, ILogger<TrayPopupViewModel> logger)
        {
            _hardwareMonitorService = hardwareMonitorService ?? throw new ArgumentNullException(nameof(hardwareMonitorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 创建更新定时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _updateTimer.Tick += async (s, e) => await UpdateHardwareInfoAsync();
        }

        #region 属性

        /// <summary>
        /// CPU使用率
        /// </summary>
        public string CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        /// <summary>
        /// CPU温度
        /// </summary>
        public string CpuTemperature
        {
            get => _cpuTemperature;
            set => SetProperty(ref _cpuTemperature, value);
        }

        /// <summary>
        /// GPU使用率
        /// </summary>
        public string GpuUsage
        {
            get => _gpuUsage;
            set => SetProperty(ref _gpuUsage, value);
        }

        /// <summary>
        /// GPU温度
        /// </summary>
        public string GpuTemperature
        {
            get => _gpuTemperature;
            set => SetProperty(ref _gpuTemperature, value);
        }

        /// <summary>
        /// 内存使用率
        /// </summary>
        public string MemoryUsage
        {
            get => _memoryUsage;
            set => SetProperty(ref _memoryUsage, value);
        }

        /// <summary>
        /// 已使用内存
        /// </summary>
        public string MemoryUsed
        {
            get => _memoryUsed;
            set => SetProperty(ref _memoryUsed, value);
        }

        /// <summary>
        /// 总内存
        /// </summary>
        public string MemoryTotal
        {
            get => _memoryTotal;
            set => SetProperty(ref _memoryTotal, value);
        }

        /// <summary>
        /// 网络下载速度
        /// </summary>
        public string NetworkDownloadSpeed
        {
            get => _networkDownloadSpeed;
            set => SetProperty(ref _networkDownloadSpeed, value);
        }

        /// <summary>
        /// 网络上传速度
        /// </summary>
        public string NetworkUploadSpeed
        {
            get => _networkUploadSpeed;
            set => SetProperty(ref _networkUploadSpeed, value);
        }

        /// <summary>
        /// 网络使用率
        /// </summary>
        public string NetworkUsage
        {
            get => _networkUsage;
            set => SetProperty(ref _networkUsage, value);
        }

        #endregion

        #region 方法

        /// <summary>
        /// 开始更新硬件信息
        /// </summary>
        public async Task StartUpdatingAsync()
        {
            try
            {
                await UpdateHardwareInfoAsync();
                _updateTimer.Start();
                _logger.LogDebug("托盘弹出窗口开始更新硬件信息");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动硬件信息更新时发生错误");
            }
        }

        /// <summary>
        /// 确保更新正在进行（如果没有在更新则启动）
        /// </summary>
        public async Task EnsureUpdatingAsync()
        {
            try
            {
                if (!_updateTimer.IsEnabled)
                {
                    await StartUpdatingAsync();
                }
                else
                {
                    // 如果定时器已经在运行，只需要更新一次数据以确保是最新的
                    await UpdateHardwareInfoAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确保硬件信息更新时发生错误");
            }
        }

        /// <summary>
        /// 停止更新硬件信息
        /// </summary>
        public void StopUpdating()
        {
            try
            {
                _updateTimer.Stop();
                _logger.LogDebug("托盘弹出窗口停止更新硬件信息");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止硬件信息更新时发生错误");
            }
        }

        /// <summary>
        /// 更新硬件信息
        /// </summary>
        private async Task UpdateHardwareInfoAsync()
        {
            try
            {
                // 获取硬件数据
                var hardwareData = await _hardwareMonitorService.GetHardwareDataAsync();
                
                // 获取CPU信息
                var cpuInfo = hardwareData.Cpus?.FirstOrDefault();
                if (cpuInfo != null)
                {
                    CpuUsage = $"{cpuInfo.UsagePercentage:F1}%";
                    CpuTemperature = $"{cpuInfo.Temperature:F1}°C";
                }

                // 获取GPU信息
                var gpuInfo = hardwareData.Gpus?.FirstOrDefault();
                if (gpuInfo != null)
                {
                    GpuUsage = $"{gpuInfo.UsagePercentage:F1}%";
                    GpuTemperature = $"{gpuInfo.Temperature:F1}°C";
                }

                // 获取内存信息
                var memoryInfo = hardwareData.Memory;
                if (memoryInfo != null)
                {
                    MemoryUsage = $"{memoryInfo.UsagePercentage:F1}%";
                    MemoryUsed = $"{memoryInfo.UsedMemory / 1024.0:F1} GB";
                    MemoryTotal = $"{memoryInfo.TotalMemory / 1024.0:F1} GB";
                }

                // 获取网络信息 - 选择最活跃的网络适配器
                var networkInfo = hardwareData.NetworkAdapters?
                    .Where(n => n.IsConnected && n.IsOnline)
                    .OrderByDescending(n => n.DownloadSpeed + n.UploadSpeed)
                    .ThenByDescending(n => n.UsagePercentage)
                    .FirstOrDefault();
                
                if (networkInfo != null)
                {
                    NetworkDownloadSpeed = $"{networkInfo.DownloadSpeed:F2} MB/s";
                    NetworkUploadSpeed = $"{networkInfo.UploadSpeed:F2} MB/s";
                    NetworkUsage = $"{networkInfo.UsagePercentage:F1}%";
                    _logger.LogDebug($"选择网络适配器: {networkInfo.Name}, 下载: {networkInfo.DownloadSpeed:F2} MB/s, 上传: {networkInfo.UploadSpeed:F2} MB/s");
                }
                else
                {
                    // 如果没有活跃的适配器，尝试获取任何可用的适配器
                    networkInfo = hardwareData.NetworkAdapters?.FirstOrDefault();
                    if (networkInfo != null)
                    {
                        NetworkDownloadSpeed = $"{networkInfo.DownloadSpeed:F2} MB/s";
                        NetworkUploadSpeed = $"{networkInfo.UploadSpeed:F2} MB/s";
                        NetworkUsage = $"{networkInfo.UsagePercentage:F1}%";
                        _logger.LogDebug($"使用备用网络适配器: {networkInfo.Name}");
                    }
                    else
                    {
                        NetworkDownloadSpeed = "0.00 MB/s";
                        NetworkUploadSpeed = "0.00 MB/s";
                        NetworkUsage = "0.0%";
                        _logger.LogWarning("未找到任何网络适配器");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新硬件信息时发生错误");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}