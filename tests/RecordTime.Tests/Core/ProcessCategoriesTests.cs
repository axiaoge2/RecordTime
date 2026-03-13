using RecordTime.Core.Services;
using Xunit;

namespace RecordTime.Tests.Core;

public class ProcessCategoriesTests
{
    [Theory]
    [InlineData("vlc")]
    [InlineData("potplayer")]
    [InlineData("bilibili")]
    [InlineData("netflix")]
    public void VideoPlayers_ContainsKnownPlayers(string processName)
    {
        Assert.Contains(processName, ProcessCategories.VideoPlayers);
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("msedge")]
    [InlineData("firefox")]
    public void Browsers_ContainsKnownBrowsers(string processName)
    {
        Assert.Contains(processName, ProcessCategories.Browsers);
    }

    [Theory]
    [InlineData("zoom")]
    [InlineData("teams")]
    [InlineData("discord")]
    public void OnlineMeetingApps_ContainsKnownApps(string processName)
    {
        Assert.Contains(processName, ProcessCategories.OnlineMeetingApps);
    }

    [Fact]
    public void AllCollections_AreCaseInsensitive()
    {
        Assert.Contains("VLC", ProcessCategories.VideoPlayers);
        Assert.Contains("CHROME", ProcessCategories.Browsers);
        Assert.Contains("ZOOM", ProcessCategories.OnlineMeetingApps);
        Assert.Contains("SPOTIFY", ProcessCategories.MusicPlayers);
    }

    [Fact]
    public void VideoPlayers_DoesNotContainMeetingApps()
    {
        Assert.DoesNotContain("zoom", ProcessCategories.VideoPlayers);
        Assert.DoesNotContain("teams", ProcessCategories.VideoPlayers);
    }

    [Fact]
    public void VideoRelatedProcesses_ContainsMeetingAndRecordingApps()
    {
        Assert.Contains("zoom", ProcessCategories.VideoRelatedProcesses);
        Assert.Contains("obs", ProcessCategories.VideoRelatedProcesses);
    }
}
