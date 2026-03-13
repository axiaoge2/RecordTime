namespace RecordTime.Core.Models.AICoach;

/// <summary>
/// 用户个人参数 - 从用户数据中学习得到
/// </summary>
public class UserParameters
{
    /// <summary>
    /// 用户标识（本地用户固定为 "local_user"）
    /// </summary>
    public string UserId { get; set; } = "local_user";

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// 数据采集开始日期
    /// </summary>
    public DateTime DataCollectionStartDate { get; set; }

    /// <summary>
    /// 当前学习阶段
    /// </summary>
    public LearningPhase CurrentPhase { get; set; } = LearningPhase.ColdStart;

    /// <summary>
    /// 专注周期长度（分钟）
    /// </summary>
    public ParameterValue<int> FocusCycleLength { get; set; } = new()
    {
        DefaultValue = 90,
        Confidence = 0
    };

    /// <summary>
    /// 高效时段（小时，0-23）
    /// </summary>
    public ParameterValue<List<int>> PeakHours { get; set; } = new()
    {
        DefaultValue = new List<int> { 10, 11, 16, 17 },
        Confidence = 0
    };

    /// <summary>
    /// 应用切换容忍度（5分钟内切换次数）
    /// </summary>
    public ParameterValue<int> SwitchTolerance { get; set; } = new()
    {
        DefaultValue = 10,
        Confidence = 0
    };

    /// <summary>
    /// 工作日模式（小时分布）
    /// </summary>
    public ParameterValue<List<int>>? WeekdayPattern { get; set; }

    /// <summary>
    /// 周末模式（小时分布）
    /// </summary>
    public ParameterValue<List<int>>? WeekendPattern { get; set; }

    /// <summary>
    /// 获取数据采集天数
    /// </summary>
    public int DataCollectionDays => (DateTime.Now - DataCollectionStartDate).Days;

    /// <summary>
    /// 根据数据采集天数更新学习阶段
    /// </summary>
    public void UpdateLearningPhase()
    {
        var days = DataCollectionDays;
        CurrentPhase = days switch
        {
            < 7 => LearningPhase.ColdStart,
            < 30 => LearningPhase.InitialLearning,
            _ => LearningPhase.StablePersonalized
        };
    }
}

/// <summary>
/// 学习阶段
/// </summary>
public enum LearningPhase
{
    /// <summary>
    /// 冷启动（0-7天）：100% 使用默认值
    /// </summary>
    ColdStart,

    /// <summary>
    /// 初步学习（7-30天）：根据置信度混合使用
    /// </summary>
    InitialLearning,

    /// <summary>
    /// 稳定个性化（30天+）：大部分使用个人参数
    /// </summary>
    StablePersonalized
}

/// <summary>
/// 带置信度的参数值
/// </summary>
public class ParameterValue<T>
{
    private T? _personalValue;
    private bool _hasPersonalValue;

    /// <summary>
    /// 默认值（来自知识库）
    /// </summary>
    public T DefaultValue { get; set; } = default!;

    /// <summary>
    /// 个人值（从数据学习）
    /// </summary>
    public T? PersonalValue
    {
        get => _personalValue;
        set
        {
            _personalValue = value;
            _hasPersonalValue = value != null;
        }
    }

    /// <summary>
    /// 是否有个人值
    /// </summary>
    public bool HasPersonalValue => _hasPersonalValue;

    /// <summary>
    /// 置信度（0-1）
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 样本数量
    /// </summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// 方差（用于评估稳定性）
    /// </summary>
    public double? Variance { get; set; }

    /// <summary>
    /// 最后计算时间
    /// </summary>
    public DateTime? LastCalculated { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// 设置个人值（用于值类型）
    /// </summary>
    public void SetPersonalValue(T value)
    {
        _personalValue = value;
        _hasPersonalValue = true;
    }

    /// <summary>
    /// 清除个人值
    /// </summary>
    public void ClearPersonalValue()
    {
        _personalValue = default;
        _hasPersonalValue = false;
    }

    /// <summary>
    /// 获取有效值（根据置信度混合）
    /// </summary>
    public T GetEffectiveValue()
    {
        // 如果没有个人值，返回默认值
        if (!_hasPersonalValue || _personalValue == null)
            return DefaultValue;

        // 对于数值类型，根据置信度混合
        if (typeof(T) == typeof(int))
        {
            var defaultVal = Convert.ToDouble(DefaultValue);
            var personalVal = Convert.ToDouble(_personalValue);
            var effective = defaultVal * (1 - Confidence) + personalVal * Confidence;
            return (T)(object)(int)Math.Round(effective);
        }

        // 对于其他类型，置信度 > 0.5 时使用个人值
        return Confidence > 0.5 ? _personalValue : DefaultValue;
    }
}
