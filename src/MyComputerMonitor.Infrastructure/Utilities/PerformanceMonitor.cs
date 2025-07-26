using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MyComputerMonitor.Infrastructure.Utilities;

/// <summary>
/// 性能监控工具
/// </summary>
public static class PerformanceMonitor
{
    /// <summary>
    /// 测量方法执行时间
    /// </summary>
    public static async Task<T> MeasureAsync<T>(
        Func<Task<T>> operation, 
        ILogger logger, 
        string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 1000) // 超过1秒记录警告
            {
                logger.LogWarning("操作 {OperationName} 执行时间较长: {ElapsedMs}ms", 
                    operationName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogDebug("操作 {OperationName} 执行完成: {ElapsedMs}ms", 
                    operationName, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "操作 {OperationName} 执行失败，耗时: {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// 测量同步方法执行时间
    /// </summary>
    public static T Measure<T>(
        Func<T> operation, 
        ILogger logger, 
        string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = operation();
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 500) // 超过500ms记录警告
            {
                logger.LogWarning("同步操作 {OperationName} 执行时间较长: {ElapsedMs}ms", 
                    operationName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogDebug("同步操作 {OperationName} 执行完成: {ElapsedMs}ms", 
                    operationName, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "同步操作 {OperationName} 执行失败，耗时: {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}