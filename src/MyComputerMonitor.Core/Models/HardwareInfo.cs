namespace MyComputerMonitor.Core.Models;

/// <summary>
/// 硬件信息基类
/// </summary>
public abstract class HardwareInfo
{
    /// <summary>
    /// 硬件名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 硬件类型
    /// </summary>
    public HardwareType Type { get; set; }
    
    /// <summary>
    /// 硬件标识符
    /// </summary>
    public string Identifier { get; set; } = string.Empty;
    
    /// <summary>
    /// 传感器数据列表
    /// </summary>
    public List<SensorData> Sensors { get; set; } = [];
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; set; } = true;
    
    /// <summary>
    /// 获取指定类型的传感器数据
    /// </summary>
    /// <param name="sensorType">传感器类型</param>
    /// <returns>传感器数据，如果不存在则返回null</returns>
    public SensorData? GetSensor(SensorType sensorType)
    {
        return Sensors.FirstOrDefault(s => s.Type == sensorType && s.IsValid);
    }
    
    /// <summary>
    /// 获取指定名称的传感器数据
    /// </summary>
    /// <param name="sensorName">传感器名称</param>
    /// <returns>传感器数据，如果不存在则返回null</returns>
    public SensorData? GetSensor(string sensorName)
    {
        return Sensors.FirstOrDefault(s => s.Name.Equals(sensorName, StringComparison.OrdinalIgnoreCase) && s.IsValid);
    }
    
    /// <summary>
    /// 获取所有指定类型的传感器数据
    /// </summary>
    /// <param name="sensorType">传感器类型</param>
    /// <returns>传感器数据列表</returns>
    public IEnumerable<SensorData> GetSensors(SensorType sensorType)
    {
        return Sensors.Where(s => s.Type == sensorType && s.IsValid);
    }
}