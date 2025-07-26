using Microsoft.Extensions.Logging;
// using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;
using System.Reflection;

namespace MyComputerMonitor.Infrastructure.Services;

/// <summary>
/// 自启动管理服务接口
/// </summary>
public interface IAutoStartService
{
    /// <summary>
    /// 检查是否已设置自启动
    /// </summary>
    /// <returns>是否已设置自启动</returns>
    Task<bool> IsAutoStartEnabledAsync();
    
    /// <summary>
    /// 启用自启动
    /// </summary>
    /// <param name="method">自启动方法</param>
    /// <returns>是否成功</returns>
    Task<bool> EnableAutoStartAsync(AutoStartMethod method = AutoStartMethod.Registry);
    
    /// <summary>
    /// 禁用自启动
    /// </summary>
    /// <returns>是否成功</returns>
    Task<bool> DisableAutoStartAsync();
    
    /// <summary>
    /// 获取当前自启动方法
    /// </summary>
    /// <returns>自启动方法</returns>
    Task<AutoStartMethod> GetAutoStartMethodAsync();
}

/// <summary>
/// 自启动方法枚举
/// </summary>
public enum AutoStartMethod
{
    /// <summary>
    /// 注册表方式
    /// </summary>
    Registry,
    
    /// <summary>
    /// 任务计划程序方式
    /// </summary>
    TaskScheduler,
    
    /// <summary>
    /// 启动文件夹方式
    /// </summary>
    StartupFolder
}

/// <summary>
/// 自启动管理服务实现
/// </summary>
public class AutoStartService : IAutoStartService
{
    private readonly ILogger<AutoStartService> _logger;
    private const string AppName = "MyComputerMonitor";
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string TaskName = "MyComputerMonitor_AutoStart";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public AutoStartService(ILogger<AutoStartService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 检查是否已设置自启动
    /// </summary>
    public async Task<bool> IsAutoStartEnabledAsync()
    {
        try
        {
            // 检查注册表方式
            if (await IsRegistryAutoStartEnabledAsync())
                return true;
            
            // 检查任务计划程序方式
            if (await IsTaskSchedulerAutoStartEnabledAsync())
                return true;
            
            // 检查启动文件夹方式
            if (await IsStartupFolderAutoStartEnabledAsync())
                return true;
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查自启动状态时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 启用自启动
    /// </summary>
    public async Task<bool> EnableAutoStartAsync(AutoStartMethod method = AutoStartMethod.Registry)
    {
        try
        {
            // 先禁用所有现有的自启动设置
            await DisableAutoStartAsync();
            
            var success = method switch
            {
                AutoStartMethod.Registry => await EnableRegistryAutoStartAsync(),
                AutoStartMethod.TaskScheduler => await EnableTaskSchedulerAutoStartAsync(),
                AutoStartMethod.StartupFolder => await EnableStartupFolderAutoStartAsync(),
                _ => false
            };
            
            if (success)
            {
                _logger.LogInformation($"自启动已启用，方法: {method}");
            }
            else
            {
                _logger.LogWarning($"启用自启动失败，方法: {method}");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"启用自启动时发生错误，方法: {method}");
            return false;
        }
    }

    /// <summary>
    /// 禁用自启动
    /// </summary>
    public async Task<bool> DisableAutoStartAsync()
    {
        try
        {
            var success = true;
            
            // 禁用注册表方式
            if (!await DisableRegistryAutoStartAsync())
                success = false;
            
            // 禁用任务计划程序方式
            if (!await DisableTaskSchedulerAutoStartAsync())
                success = false;
            
            // 禁用启动文件夹方式
            if (!await DisableStartupFolderAutoStartAsync())
                success = false;
            
            if (success)
            {
                _logger.LogInformation("自启动已禁用");
            }
            else
            {
                _logger.LogWarning("禁用自启动时部分操作失败");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "禁用自启动时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 获取当前自启动方法
    /// </summary>
    public async Task<AutoStartMethod> GetAutoStartMethodAsync()
    {
        try
        {
            if (await IsRegistryAutoStartEnabledAsync())
                return AutoStartMethod.Registry;
            
            if (await IsTaskSchedulerAutoStartEnabledAsync())
                return AutoStartMethod.TaskScheduler;
            
            if (await IsStartupFolderAutoStartEnabledAsync())
                return AutoStartMethod.StartupFolder;
            
            return AutoStartMethod.Registry; // 默认方法
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取自启动方法时发生错误");
            return AutoStartMethod.Registry;
        }
    }

    #region 注册表方式

    /// <summary>
    /// 检查注册表自启动是否启用
    /// </summary>
    private async Task<bool> IsRegistryAutoStartEnabledAsync()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryKey);
            var value = key?.GetValue(AppName) as string;
            var currentPath = GetCurrentExecutablePath();
            
            return !string.IsNullOrEmpty(value) && value.Equals(currentPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "检查注册表自启动时发生错误");
            return false;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 启用注册表自启动
    /// </summary>
    private async Task<bool> EnableRegistryAutoStartAsync()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryKey);
            key.SetValue(AppName, GetCurrentExecutablePath());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启用注册表自启动时发生错误");
            return false;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 禁用注册表自启动
    /// </summary>
    private async Task<bool> DisableRegistryAutoStartAsync()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key?.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "禁用注册表自启动时发生错误");
            return false;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    #endregion

    #region 任务计划程序方式

    /// <summary>
    /// 检查任务计划程序自启动是否启用
    /// </summary>
    private async Task<bool> IsTaskSchedulerAutoStartEnabledAsync()
    {
        // TODO: 暂时禁用TaskScheduler功能，需要解决包引用问题
        await Task.CompletedTask;
        return false;
        /*
        try
        {
            using var taskService = new TaskService();
            var task = taskService.GetTask(TaskName);
            return task != null && task.Enabled;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "检查任务计划程序自启动时发生错误");
            return false;
        }
        finally
        {
            await Task.CompletedTask;
        }
        */
    }

    /// <summary>
    /// 启用任务计划程序自启动
    /// </summary>
    private async Task<bool> EnableTaskSchedulerAutoStartAsync()
    {
        // TODO: 暂时禁用TaskScheduler功能，需要解决包引用问题
        await Task.CompletedTask;
        return false;
        /*
        try
        {
            using var taskService = new TaskService();
            
            // 删除现有任务（如果存在）
            try
            {
                taskService.RootFolder.DeleteTask(TaskName);
            }
            catch
            {
                // 忽略删除失败
            }
            
            // 创建新任务
            var taskDefinition = taskService.NewTask();
            taskDefinition.RegistrationInfo.Description = "电脑硬件监控自启动任务";
            taskDefinition.RegistrationInfo.Author = "MyComputerMonitor";
            
            // 设置触发器（用户登录时）
            var trigger = new LogonTrigger();
            taskDefinition.Triggers.Add(trigger);
            
            // 设置操作（启动程序）
            var action = new ExecAction(GetCurrentExecutablePath());
            taskDefinition.Actions.Add(action);
            
            // 设置其他属性
            taskDefinition.Settings.AllowDemandStart = true;
            taskDefinition.Settings.DisallowStartIfOnBatteries = false;
            taskDefinition.Settings.StopIfGoingOnBatteries = false;
            taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
            
            // 注册任务
            taskService.RootFolder.RegisterTaskDefinition(TaskName, taskDefinition);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启用任务计划程序自启动时发生错误");
            return false;
        }
        finally
        {
            await Task.CompletedTask;
        }
        */
    }

    /// <summary>
    /// 禁用任务计划程序自启动
    /// </summary>
    private async Task<bool> DisableTaskSchedulerAutoStartAsync()
    {
        // TODO: 暂时禁用TaskScheduler功能，需要解决包引用问题
        await Task.CompletedTask;
        return false;
        /*
        try
        {
            using var taskService = new TaskService();
            taskService.RootFolder.DeleteTask(TaskName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "禁用任务计划程序自启动时发生错误");
            return false;
        }
        finally
        {
            await Task.CompletedTask;
        }
        */
    }

    #endregion

    #region 启动文件夹方式

    /// <summary>
    /// 检查启动文件夹自启动是否启用
    /// </summary>
    private async Task<bool> IsStartupFolderAutoStartEnabledAsync()
    {
        try
        {
            var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupPath, $"{AppName}.lnk");
            return File.Exists(shortcutPath);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "检查启动文件夹自启动时发生错误");
            return false;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 启用启动文件夹自启动
    /// </summary>
    private async Task<bool> EnableStartupFolderAutoStartAsync()
    {
        try
        {
            var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupPath, $"{AppName}.lnk");
            
            // 创建快捷方式
            CreateShortcut(shortcutPath, GetCurrentExecutablePath(), "电脑硬件监控");
            
            return File.Exists(shortcutPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启用启动文件夹自启动时发生错误");
            return false;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 禁用启动文件夹自启动
    /// </summary>
    private async Task<bool> DisableStartupFolderAutoStartAsync()
    {
        try
        {
            var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupPath, $"{AppName}.lnk");
            
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "禁用启动文件夹自启动时发生错误");
            return false;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取当前可执行文件路径
    /// </summary>
    private string GetCurrentExecutablePath()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return assembly.Location;
    }

    /// <summary>
    /// 创建快捷方式
    /// </summary>
    private void CreateShortcut(string shortcutPath, string targetPath, string description)
    {
        try
        {
            // 使用COM对象创建快捷方式
            var shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
            var shortcut = shell!.GetType().InvokeMember("CreateShortcut", 
                BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
            
            shortcut!.GetType().InvokeMember("TargetPath", 
                BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcut.GetType().InvokeMember("Description", 
                BindingFlags.SetProperty, null, shortcut, new object[] { description });
            shortcut.GetType().InvokeMember("Save", 
                BindingFlags.InvokeMethod, null, shortcut, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建快捷方式时发生错误");
            throw;
        }
    }

    #endregion
}