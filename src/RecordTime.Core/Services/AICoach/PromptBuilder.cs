using System.Text;
using System.Text.Json;
using RecordTime.Core.Models.AICoach;

namespace RecordTime.Core.Services.AICoach;

/// <summary>
/// Prompt 构建器 - 组合知识库、个人参数和当前状态生成完整 Prompt
/// </summary>
public class PromptBuilder
{
    private readonly KnowledgeBaseProvider _knowledgeBase;

    public PromptBuilder(KnowledgeBaseProvider knowledgeBase)
    {
        _knowledgeBase = knowledgeBase;
    }

    /// <summary>
    /// 构建系统消息（包含角色设定和知识库）
    /// </summary>
    public string BuildSystemMessage()
    {
        var sb = new StringBuilder();

        // 系统角色设定
        sb.AppendLine(_knowledgeBase.GetSystemRole());
        sb.AppendLine();

        // 认知科学原则
        sb.AppendLine(_knowledgeBase.GetCognitivePrinciples());

        return sb.ToString();
    }

    /// <summary>
    /// 构建用户消息（包含上下文和用户问题）
    /// </summary>
    public string BuildUserMessage(CognitiveContext context, string userQuestion)
    {
        var sb = new StringBuilder();

        // 用户数据上下文
        sb.AppendLine("## 我的数据");
        sb.AppendLine();
        sb.AppendLine(FormatContext(context));
        sb.AppendLine();

        // 用户问题
        sb.AppendLine("## 我的问题");
        sb.AppendLine(userQuestion);

        return sb.ToString();
    }

    /// <summary>
    /// 构建快捷操作的用户消息
    /// </summary>
    public string BuildQuickActionMessage(CognitiveContext context, QuickActionType actionType)
    {
        var prompt = _knowledgeBase.GetQuickActionPrompt(actionType);
        return BuildUserMessage(context, prompt);
    }

    /// <summary>
    /// 格式化上下文数据
    /// </summary>
    private string FormatContext(CognitiveContext context)
    {
        var sb = new StringBuilder();

        // 当前时间
        sb.AppendLine($"**当前时间**: {context.CurrentTime:yyyy年M月d日 HH:mm}");
        sb.AppendLine();

        // 今日摘要
        sb.AppendLine("**今日数据**:");
        sb.AppendLine($"- 总专注时间: {FormatMinutes(context.TodaySummary.TotalFocusMinutes)}");
        sb.AppendLine($"- 总空闲时间: {FormatMinutes(context.TodaySummary.TotalIdleMinutes)}");
        sb.AppendLine($"- 应用切换次数: {context.TodaySummary.AppSwitchCount}次");

        // 按分类统计
        if (context.TodaySummary.ByCategory.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**按分类统计**:");
            foreach (var (category, minutes) in context.TodaySummary.ByCategory.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"- {category}: {FormatMinutes(minutes)}");
            }
        }

        // 按应用统计（取前5个）
        if (context.TodaySummary.ByApp.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**主要应用**:");
            foreach (var (app, minutes) in context.TodaySummary.ByApp.OrderByDescending(x => x.Value).Take(5))
            {
                sb.AppendLine($"- {app}: {FormatMinutes(minutes)}");
            }
        }

        // 当前会话
        if (context.CurrentSession != null)
        {
            sb.AppendLine();
            sb.AppendLine("**当前会话**:");
            sb.AppendLine($"- 应用: {context.CurrentSession.App}");
            sb.AppendLine($"- 分类: {context.CurrentSession.Category}");
            sb.AppendLine($"- 已持续: {context.CurrentSession.DurationMinutes}分钟");
            sb.AppendLine($"- 活动类型: {GetActivityTypeDisplay(context.CurrentSession.ActivityType)}");
        }

        // 最近模式
        sb.AppendLine();
        sb.AppendLine("**最近状态**:");
        sb.AppendLine($"- 最近5分钟切换次数: {context.RecentPattern.SwitchesLast5Min}次");
        sb.AppendLine($"- 专注状态: {(context.RecentPattern.IsDeepFocus ? "深度专注 ✓" : context.RecentPattern.IsFragmented ? "注意力分散 ⚠" : "正常")}");

        // 个人参数（如果有）
        if (context.PersonalParams != null)
        {
            sb.AppendLine();
            sb.AppendLine("**我的模式**:");
            sb.AppendLine($"- 专注周期: 约{context.PersonalParams.FocusCycleLength.GetEffectiveValue()}分钟");
            sb.AppendLine($"- 高效时段: {string.Join(", ", context.PersonalParams.PeakHours.GetEffectiveValue().Select(h => $"{h}点"))}");
            sb.AppendLine($"- 学习阶段: {GetLearningPhaseDisplay(context.PersonalParams.CurrentPhase)}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 格式化分钟数为可读字符串
    /// </summary>
    private string FormatMinutes(int minutes)
    {
        if (minutes < 60)
            return $"{minutes}分钟";

        var hours = minutes / 60;
        var mins = minutes % 60;
        return mins > 0 ? $"{hours}小时{mins}分钟" : $"{hours}小时";
    }

    /// <summary>
    /// 获取活动类型显示名称
    /// </summary>
    private string GetActivityTypeDisplay(Models.ActivityType activityType)
    {
        return activityType switch
        {
            Models.ActivityType.Idle => "空闲",
            Models.ActivityType.Video => "看视频",
            Models.ActivityType.Meeting => "会议",
            Models.ActivityType.Gaming => "游戏",
            Models.ActivityType.ActiveTyping => "积极输入",
            Models.ActivityType.Reading => "阅读",
            Models.ActivityType.PassiveBrowsing => "浏览",
            _ => "其他"
        };
    }

    /// <summary>
    /// 获取学习阶段显示名称
    /// </summary>
    private string GetLearningPhaseDisplay(LearningPhase phase)
    {
        return phase switch
        {
            LearningPhase.ColdStart => "初始化中（数据积累中）",
            LearningPhase.InitialLearning => "学习中（开始个性化）",
            LearningPhase.StablePersonalized => "已个性化",
            _ => "未知"
        };
    }
}
