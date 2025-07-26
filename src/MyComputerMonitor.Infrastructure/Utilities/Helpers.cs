using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MyComputerMonitor.Infrastructure.Utilities;

/// <summary>
/// 文件操作工具类
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// 确保目录存在
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
    
    /// <summary>
    /// 安全读取文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件内容</returns>
    public static async Task<string> SafeReadFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return string.Empty;
            
            return await File.ReadAllTextAsync(filePath);
        }
        catch
        {
            return string.Empty;
        }
    }
    
    /// <summary>
    /// 安全写入文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="content">文件内容</param>
    /// <returns>是否成功</returns>
    public static async Task<bool> SafeWriteFileAsync(string filePath, string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                EnsureDirectoryExists(directory);
            }
            
            await File.WriteAllTextAsync(filePath, content);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 获取应用程序数据目录
    /// </summary>
    /// <param name="appName">应用程序名称</param>
    /// <returns>数据目录路径</returns>
    public static string GetAppDataDirectory(string appName = "MyComputerMonitor")
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDirectory = Path.Combine(appDataPath, appName);
        EnsureDirectoryExists(appDirectory);
        return appDirectory;
    }
    
    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    /// <param name="fileName">配置文件名</param>
    /// <param name="appName">应用程序名称</param>
    /// <returns>配置文件路径</returns>
    public static string GetConfigFilePath(string fileName = "config.json", string appName = "MyComputerMonitor")
    {
        var appDataDirectory = GetAppDataDirectory(appName);
        return Path.Combine(appDataDirectory, fileName);
    }
}

/// <summary>
/// JSON序列化工具类
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    
    /// <summary>
    /// 序列化对象到JSON字符串
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">要序列化的对象</param>
    /// <param name="options">序列化选项</param>
    /// <returns>JSON字符串</returns>
    public static string Serialize<T>(T obj, JsonSerializerOptions? options = null)
    {
        try
        {
            return JsonSerializer.Serialize(obj, options ?? DefaultOptions);
        }
        catch
        {
            return string.Empty;
        }
    }
    
    /// <summary>
    /// 从JSON字符串反序列化对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="json">JSON字符串</param>
    /// <param name="options">反序列化选项</param>
    /// <returns>反序列化的对象</returns>
    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;
            
            return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
        }
        catch
        {
            return default;
        }
    }
    
    /// <summary>
    /// 从文件反序列化对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="filePath">文件路径</param>
    /// <param name="options">反序列化选项</param>
    /// <returns>反序列化的对象</returns>
    public static async Task<T?> DeserializeFromFileAsync<T>(string filePath, JsonSerializerOptions? options = null)
    {
        try
        {
            var json = await FileHelper.SafeReadFileAsync(filePath);
            return Deserialize<T>(json, options);
        }
        catch
        {
            return default;
        }
    }
    
    /// <summary>
    /// 序列化对象到文件
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">要序列化的对象</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="options">序列化选项</param>
    /// <returns>是否成功</returns>
    public static async Task<bool> SerializeToFileAsync<T>(T obj, string filePath, JsonSerializerOptions? options = null)
    {
        try
        {
            var json = Serialize(obj, options);
            return await FileHelper.SafeWriteFileAsync(filePath, json);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 系统工具类
/// </summary>
public static class SystemHelper
{
    /// <summary>
    /// 检查是否为管理员权限
    /// </summary>
    /// <returns>是否为管理员</returns>
    public static bool IsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 格式化字节大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化的大小字符串</returns>
    public static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        decimal number = bytes;
        
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        
        return $"{number:n1} {suffixes[counter]}";
    }
    
    /// <summary>
    /// 格式化百分比
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="decimals">小数位数</param>
    /// <returns>格式化的百分比字符串</returns>
    public static string FormatPercentage(double value, int decimals = 1)
    {
        return string.Format("{0:F" + decimals + "}%", Math.Round(value, decimals));
    }
    
    /// <summary>
    /// 格式化时间跨度
    /// </summary>
    /// <param name="timeSpan">时间跨度</param>
    /// <returns>格式化的时间字符串</returns>
    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalDays}天 {timeSpan.Hours}小时 {timeSpan.Minutes}分钟";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}小时 {timeSpan.Minutes}分钟";
        }
        else
        {
            return $"{timeSpan.Minutes}分钟 {timeSpan.Seconds}秒";
        }
    }
    
    /// <summary>
    /// 获取友好的温度字符串
    /// </summary>
    /// <param name="temperature">温度（摄氏度）</param>
    /// <returns>温度字符串</returns>
    public static string FormatTemperature(double temperature)
    {
        return $"{Math.Round(temperature, 1):F1}°C";
    }
    
    /// <summary>
    /// 获取友好的频率字符串
    /// </summary>
    /// <param name="frequency">频率（Hz）</param>
    /// <returns>频率字符串</returns>
    public static string FormatFrequency(double frequency)
    {
        if (frequency >= 1_000_000_000)
        {
            return $"{frequency / 1_000_000_000:F2} GHz";
        }
        else if (frequency >= 1_000_000)
        {
            return $"{frequency / 1_000_000:F0} MHz";
        }
        else if (frequency >= 1_000)
        {
            return $"{frequency / 1_000:F0} KHz";
        }
        else
        {
            return $"{frequency:F0} Hz";
        }
    }
}

/// <summary>
/// 日志工具类
/// </summary>
public static class LogHelper
{
    /// <summary>
    /// 安全记录异常日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="exception">异常</param>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    public static void SafeLogError(ILogger? logger, Exception exception, string message, params object[] args)
    {
        try
        {
            logger?.LogError(exception, message, args);
        }
        catch
        {
            // 忽略日志记录失败
        }
    }
    
    /// <summary>
    /// 安全记录警告日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    public static void SafeLogWarning(ILogger? logger, string message, params object[] args)
    {
        try
        {
            logger?.LogWarning(message, args);
        }
        catch
        {
            // 忽略日志记录失败
        }
    }
    
    /// <summary>
    /// 安全记录信息日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    public static void SafeLogInformation(ILogger? logger, string message, params object[] args)
    {
        try
        {
            logger?.LogInformation(message, args);
        }
        catch
        {
            // 忽略日志记录失败
        }
    }
}