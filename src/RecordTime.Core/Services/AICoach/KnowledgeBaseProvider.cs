namespace RecordTime.Core.Services.AICoach;

/// <summary>
/// 知识库提供者 - 提供认知科学原则用于 Prompt 注入
/// </summary>
public class KnowledgeBaseProvider
{
    /// <summary>
    /// 获取系统角色设定
    /// </summary>
    public string GetSystemRole()
    {
        return """
            你是一个专业的时间管理助手，名为 "AI Time Coach"。
            你的核心理念是：不是控制时间，而是结合人脑的认知规律，提供可操作的建议。

            你的特点：
            1. 基于科学：所有建议都基于认知心理学研究
            2. 个性化：根据用户的实际数据和模式提供建议
            3. 积极正向：肯定用户的进步，以鼓励为主
            4. 可操作：建议要具体、可执行

            注意事项：
            - 不要说教或批评用户
            - 用简洁友好的语气
            - 建议要考虑用户当前的状态
            - 如果数据不足，诚实说明
            """;
    }

    /// <summary>
    /// 获取认知科学原则（用于 Prompt 注入）
    /// </summary>
    public string GetCognitivePrinciples()
    {
        return """
            ## 认知科学原则（你需要基于这些原则提供建议）

            ### 1. 超日节律 (Ultradian Rhythms)
            - 人类存在约 90-120 分钟的认知周期
            - 周期内认知能力呈波动状态，有高峰和低谷
            - 应在周期低谷时安排休息，而非强行坚持

            ### 2. 任务切换成本 (Task Switching Costs)
            - 每次任务切换存在认知"残余成本"
            - 被打断后平均需要 15-25 分钟才能完全恢复专注
            - 涉及任务重配置和干扰控制两个过程

            ### 3. 注意力残留 (Attention Residue)
            - 切换任务时，部分注意力仍"残留"在前一个任务上
            - 未完成的任务比已完成的任务产生更多残留
            - 记录当前状态可减少残留

            ### 4. 认知负荷理论 (Cognitive Load Theory)
            - 工作记忆容量有限：约 4-7 个信息块
            - 超载会导致效率急剧下降
            - 复杂任务应分解为小步骤

            ### 5. 自我损耗 (Ego Depletion)
            - 自控力像肌肉一样会疲劳
            - 决策、抑制冲动、保持专注消耗同一池资源
            - 重要决策应安排在精力充沛时

            ### 6. 心流理论 (Flow Theory)
            - 心流需要：清晰目标、即时反馈、技能与挑战平衡
            - 检测到深度专注时应保护而非打断
            - 太简单会无聊，太难会焦虑

            ### 7. 昼夜节律 (Circadian Rhythms)
            - 注意力水平在一天中有规律波动
            - 多数人在上午 10-12 点和下午 4-6 点有认知高峰
            - 下午 2-3 点通常是低谷（午后低迷）
            - 个体差异显著

            ### 8. 微休息效果 (Micro-breaks)
            - 短暂休息（30秒-5分钟）对提升表现有效
            - 休息应该是主动设计的
            - 休息内容与工作内容应该不同
            """;
    }

    /// <summary>
    /// 获取快捷按钮对应的 Prompt
    /// </summary>
    public string GetQuickActionPrompt(QuickActionType actionType)
    {
        return actionType switch
        {
            QuickActionType.DailySummary => """
                请根据我今天的使用数据，提供一个简洁的总结：
                1. 今天的时间分配概况
                2. 做得好的地方（值得肯定的）
                3. 可以改进的地方（用建设性的语气）
                4. 一句鼓励的话

                请用友好的语气，不要太长。
                """,

            QuickActionType.QuickAdvice => """
                根据我当前的状态和历史模式，给我一条最相关的可操作建议。

                要求：
                - 只给一条建议，不要太多
                - 要具体可执行
                - 考虑我当前的时间和状态
                - 如果我正在深度专注，不要建议打断
                """,

            QuickActionType.TomorrowPlan => """
                根据我的高效时段和今天的完成情况，建议明天的时间安排策略。

                要求：
                - 基于我的个人模式（如果有数据）
                - 考虑认知科学原则（如超日节律）
                - 给出具体的时段建议
                - 保持简洁实用
                """,

            _ => "请根据用户数据提供个性化的时间管理建议。"
        };
    }
}

/// <summary>
/// 快捷操作类型
/// </summary>
public enum QuickActionType
{
    /// <summary>
    /// 今日总结
    /// </summary>
    DailySummary,

    /// <summary>
    /// 即时建议
    /// </summary>
    QuickAdvice,

    /// <summary>
    /// 明日规划
    /// </summary>
    TomorrowPlan
}
