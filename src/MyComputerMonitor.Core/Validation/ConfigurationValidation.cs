using System.ComponentModel.DataAnnotations;

namespace MyComputerMonitor.Core.Validation;

/// <summary>
/// 配置验证特性
/// </summary>
public class ConfigurationValidationAttribute : ValidationAttribute
{
    /// <summary>
    /// 验证配置值
    /// </summary>
    /// <param name="value">配置值</param>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>验证结果</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult($"{validationContext.DisplayName} 不能为空");
        }
        
        return ValidationResult.Success;
    }
}

/// <summary>
/// 范围验证特性
/// </summary>
public class RangeValidationAttribute : ValidationAttribute
{
    private readonly double _minimum;
    private readonly double _maximum;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="minimum">最小值</param>
    /// <param name="maximum">最大值</param>
    public RangeValidationAttribute(double minimum, double maximum)
    {
        _minimum = minimum;
        _maximum = maximum;
    }
    
    /// <summary>
    /// 验证范围
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>验证结果</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is double doubleValue)
        {
            if (doubleValue < _minimum || doubleValue > _maximum)
            {
                return new ValidationResult($"{validationContext.DisplayName} 必须在 {_minimum} 到 {_maximum} 之间");
            }
        }
        else if (value is int intValue)
        {
            if (intValue < _minimum || intValue > _maximum)
            {
                return new ValidationResult($"{validationContext.DisplayName} 必须在 {_minimum} 到 {_maximum} 之间");
            }
        }
        
        return ValidationResult.Success;
    }
}

/// <summary>
/// 配置验证器
/// </summary>
public static class ConfigurationValidator
{
    /// <summary>
    /// 验证配置对象
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="configuration">配置对象</param>
    /// <returns>验证结果</returns>
    public static ValidationResult ValidateConfiguration<T>(T configuration) where T : class
    {
        var context = new ValidationContext(configuration);
        var results = new List<ValidationResult>();
        
        var isValid = Validator.TryValidateObject(configuration, context, results, true);
        
        if (isValid)
        {
            return ValidationResult.Success!;
        }
        
        var errorMessage = string.Join("; ", results.Select(r => r.ErrorMessage));
        return new ValidationResult(errorMessage);
    }
    
    /// <summary>
    /// 验证并修复配置
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="configuration">配置对象</param>
    /// <param name="defaultConfiguration">默认配置</param>
    /// <returns>修复后的配置</returns>
    public static T ValidateAndFixConfiguration<T>(T configuration, T defaultConfiguration) where T : class
    {
        var validationResult = ValidateConfiguration(configuration);
        
        if (validationResult == ValidationResult.Success)
        {
            return configuration;
        }
        
        // 如果验证失败，返回默认配置
        return defaultConfiguration;
    }
}