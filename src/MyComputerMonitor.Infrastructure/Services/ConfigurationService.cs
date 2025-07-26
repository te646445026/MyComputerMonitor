using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyComputerMonitor.Core.Events;
using MyComputerMonitor.Core.Interfaces;
using System.Text.Json;

namespace MyComputerMonitor.Infrastructure.Services;

/// <summary>
/// 配置管理服务实现
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configFilePath;
    private readonly Dictionary<string, object> _configurationData;
    private readonly object _lockObject = new();
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 配置变更事件
    /// </summary>
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        _configurationData = new Dictionary<string, object>();
        
        // 配置文件路径
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appConfigPath = Path.Combine(appDataPath, "MyComputerMonitor");
        Directory.CreateDirectory(appConfigPath);
        _configFilePath = Path.Combine(appConfigPath, "config.json");
        
        // JSON序列化选项
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        // 立即加载默认配置，确保服务可用
        LoadDefaultConfiguration();
        
        _logger.LogInformation($"配置服务已创建，配置文件路径: {_configFilePath}");
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public async Task LoadConfigurationAsync()
    {
        try
        {
            lock (_lockObject)
            {
                _configurationData.Clear();
                
                if (File.Exists(_configFilePath))
                {
                    var jsonContent = File.ReadAllText(_configFilePath);
                    if (!string.IsNullOrWhiteSpace(jsonContent))
                    {
                        var configData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent, _jsonOptions);
                        if (configData != null)
                        {
                            foreach (var kvp in configData)
                            {
                                _configurationData[kvp.Key] = ConvertJsonElement(kvp.Value);
                            }
                        }
                    }
                }
                
                // 如果配置文件不存在或为空，则使用默认配置
                if (_configurationData.Count == 0)
                {
                    LoadDefaultConfiguration();
                }
            }
            
            _logger.LogInformation($"配置已加载，共 {_configurationData.Count} 项配置");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载配置时发生错误");
            LoadDefaultConfiguration();
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public async Task SaveConfigurationAsync()
    {
        try
        {
            lock (_lockObject)
            {
                var jsonContent = JsonSerializer.Serialize(_configurationData, _jsonOptions);
                File.WriteAllText(_configFilePath, jsonContent);
            }
            
            _logger.LogInformation("配置已保存");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存配置时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取配置值
    /// </summary>
    public T GetValue<T>(string key, T defaultValue = default!)
    {
        try
        {
            lock (_lockObject)
            {
                if (_configurationData.TryGetValue(key, out var value))
                {
                    if (value is T directValue)
                    {
                        return directValue;
                    }
                    
                    // 尝试转换类型
                    if (value is JsonElement jsonElement)
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), _jsonOptions) ?? defaultValue;
                    }
                    
                    // 尝试直接转换
                    return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"获取配置值 {key} 时发生错误，使用默认值");
        }
        
        return defaultValue;
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    public void SetValue<T>(string key, T value)
    {
        object? oldValue = null;
        
        try
        {
            lock (_lockObject)
            {
                _configurationData.TryGetValue(key, out oldValue);
                _configurationData[key] = value!;
            }
            
            // 触发配置变更事件
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(key, oldValue, value));
            
            _logger.LogDebug($"配置值已更新: {key} = {value}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"设置配置值 {key} 时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取监控配置
    /// </summary>
    public MonitoringConfiguration GetMonitoringConfiguration()
    {
        return GetValue("MonitoringConfiguration", new MonitoringConfiguration());
    }

    /// <summary>
    /// 设置监控配置
    /// </summary>
    public void SetMonitoringConfiguration(MonitoringConfiguration configuration)
    {
        SetValue("MonitoringConfiguration", configuration);
    }

    /// <summary>
    /// 获取UI配置
    /// </summary>
    public UiConfiguration GetUiConfiguration()
    {
        return GetValue("UiConfiguration", new UiConfiguration());
    }

    /// <summary>
    /// 设置UI配置
    /// </summary>
    public void SetUiConfiguration(UiConfiguration configuration)
    {
        SetValue("UiConfiguration", configuration);
    }

    /// <summary>
    /// 获取通知配置
    /// </summary>
    public NotificationConfiguration GetNotificationConfiguration()
    {
        return GetValue("NotificationConfiguration", new NotificationConfiguration());
    }

    /// <summary>
    /// 设置通知配置
    /// </summary>
    public void SetNotificationConfiguration(NotificationConfiguration configuration)
    {
        SetValue("NotificationConfiguration", configuration);
    }

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    public async Task ResetToDefaultAsync()
    {
        try
        {
            lock (_lockObject)
            {
                _configurationData.Clear();
                LoadDefaultConfiguration();
            }
            
            await SaveConfigurationAsync();
            _logger.LogInformation("配置已重置为默认值");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置配置时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 加载默认配置
    /// </summary>
    private void LoadDefaultConfiguration()
    {
        _configurationData["MonitoringConfiguration"] = new MonitoringConfiguration();
        _configurationData["UiConfiguration"] = new UiConfiguration();
        _configurationData["NotificationConfiguration"] = new NotificationConfiguration();
        
        _logger.LogInformation("已加载默认配置");
    }

    /// <summary>
    /// 转换JsonElement为对应的.NET类型
    /// </summary>
    private object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            JsonValueKind.Null => null!,
            _ => element
        };
    }
}