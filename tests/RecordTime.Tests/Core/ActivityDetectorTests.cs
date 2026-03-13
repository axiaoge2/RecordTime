using RecordTime.Core.Models;
using RecordTime.Core.Services;
using Xunit;

namespace RecordTime.Tests.Core;

public class ActivityDetectorTests
{
    private readonly ActivityDetector _detector = new();

    private static WindowInfo Window(string processName = "test", bool fullscreen = false) => new()
    {
        ProcessName = processName,
        WindowTitle = "Test",
        IsFullscreen = fullscreen,
        IsFocused = true
    };

    private static SystemState BaseState() => new()
    {
        WindowFocused = true
    };

    [Fact]
    public void Idle_HasHighestPriority()
    {
        var state = BaseState();
        state.SystemIdle = true;
        state.MediaSessionPlaying = true;

        var result = _detector.DetermineActivity(Window(), state);

        Assert.Equal(ActivityType.Idle, result);
    }

    [Fact]
    public void Video_MediaSessionPlaying()
    {
        var state = BaseState();
        state.MediaSessionPlaying = true;

        var result = _detector.DetermineActivity(Window("vlc"), state);

        Assert.Equal(ActivityType.Video, result);
    }

    [Fact]
    public void Video_VideoAppWithAudio()
    {
        var state = BaseState();
        state.IsVideoApp = true;
        state.AudioActive = true;

        var result = _detector.DetermineActivity(Window("potplayer"), state);

        Assert.Equal(ActivityType.Video, result);
    }

    [Fact]
    public void Video_BrowserVideoPlaying()
    {
        var state = BaseState();
        state.IsBrowser = true;
        state.BrowserVideoPlaying = true;

        var result = _detector.DetermineActivity(Window("chrome"), state);

        Assert.Equal(ActivityType.Video, result);
    }

    [Fact]
    public void Meeting_OnlineMeetingWithAudio()
    {
        var state = BaseState();
        state.AudioActive = true;

        var result = _detector.DetermineActivity(Window("zoom"), state);

        Assert.Equal(ActivityType.Meeting, result);
    }

    [Fact]
    public void Gaming_FullscreenWithFrequentInput()
    {
        var state = BaseState();
        state.FrequentInput = true;

        var result = _detector.DetermineActivity(Window("game", fullscreen: true), state);

        Assert.Equal(ActivityType.Gaming, result);
    }

    [Fact]
    public void Gaming_GameProcessWithFrequentInput()
    {
        var state = BaseState();
        state.FrequentInput = true;

        var result = _detector.DetermineActivity(Window("steam"), state);

        Assert.Equal(ActivityType.Gaming, result);
    }

    [Fact]
    public void ActiveTyping_HighKeyboardActivity()
    {
        var state = BaseState();
        state.KeyboardActivityLast30s = 25;

        var result = _detector.DetermineActivity(Window("Code"), state);

        Assert.Equal(ActivityType.ActiveTyping, result);
    }

    [Fact]
    public void Reading_HighScrollLowKeyboard()
    {
        var state = BaseState();
        state.ScrollCountLast30s = 15;
        state.KeyboardActivityLast30s = 2;

        var result = _detector.DetermineActivity(Window("chrome"), state);

        Assert.Equal(ActivityType.Reading, result);
    }

    [Fact]
    public void PassiveBrowsing_DefaultWhenFocused()
    {
        var state = BaseState();

        var result = _detector.DetermineActivity(Window("notepad"), state);

        Assert.Equal(ActivityType.PassiveBrowsing, result);
    }

    [Theory]
    [InlineData("chrome", "浏览器")]
    [InlineData("Code", "开发工具")]
    [InlineData("WINWORD", "办公软件")]
    [InlineData("WeChat", "社交通讯")]
    [InlineData("vlc", "视频娱乐")]
    [InlineData("spotify", "音乐")]
    [InlineData("explorer", "系统桌面")]
    [InlineData("unknown_app", "其他应用")]
    public void GetAppCategory_ReturnsCorrectCategory(string processName, string expectedCategory)
    {
        var result = _detector.GetAppCategory(processName);

        Assert.Equal(expectedCategory, result);
    }

    [Fact]
    public void IsVideoPlaying_KnownVideoPlayer_ReturnsTrue()
    {
        Assert.True(_detector.IsVideoPlaying("vlc"));
        Assert.True(_detector.IsVideoPlaying("potplayer"));
        Assert.False(_detector.IsVideoPlaying("notepad"));
    }

    [Fact]
    public void IsBrowser_KnownBrowser_ReturnsTrue()
    {
        Assert.True(_detector.IsBrowser("chrome"));
        Assert.True(_detector.IsBrowser("firefox"));
        Assert.False(_detector.IsBrowser("notepad"));
    }

    [Fact]
    public void IsOnlineMeeting_KnownMeetingApp_ReturnsTrue()
    {
        Assert.True(_detector.IsOnlineMeeting("zoom"));
        Assert.True(_detector.IsOnlineMeeting("teams"));
        Assert.False(_detector.IsOnlineMeeting("notepad"));
    }
}
