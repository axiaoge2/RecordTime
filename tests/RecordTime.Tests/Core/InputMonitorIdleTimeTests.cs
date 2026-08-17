using RecordTime.Core.Services;
using Xunit;

namespace RecordTime.Tests.Core;

/// <summary>
/// InputMonitor 空闲时间计算测试（覆盖 32 位 tick 回绕与钳制逻辑）
/// </summary>
public class InputMonitorIdleTimeTests
{
    public static TheoryData<uint, uint, int> IdleTimeCases => new()
    {
        // 同一时刻无空闲
        { 0u, 0u, 0 },
        // 正常空闲 5 分钟
        { 1_000u, 301_000u, 300 },
        // dwTime 已越过 2^32 回绕点：原 GetTickCount64 - dwTime 会误报约 49.7 天
        { 0xFFFB6C20u, 0x100u, 300 },
        // 空闲略低于 24 小时上限，不钳制
        { 1_000u, 86_399_000u, 86_398 },
        // 空闲刚好达到 24 小时上限
        { 1_000u, 86_401_000u, 86_400 },
        // 超长空闲（约 23 天）被钳制到 24 小时
        { 1_000u, 2_000_001_000u, 86_400 },
    };

    [Theory]
    [MemberData(nameof(IdleTimeCases))]
    public void ComputeIdleTimeSeconds_HandlesTickWrapAndClamps(uint lastInputTick, uint nowTick, int expectedSeconds)
    {
        Assert.Equal(expectedSeconds, InputMonitor.ComputeIdleTimeSeconds(lastInputTick, nowTick));
    }
}
