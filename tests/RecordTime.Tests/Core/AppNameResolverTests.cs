using RecordTime.Core.Services;
using Xunit;

namespace RecordTime.Tests.Core;

public class AppNameResolverTests
{
    [Theory]
    [InlineData("chrome", "Google Chrome")]
    [InlineData("msedge", "Microsoft Edge")]
    [InlineData("Code", "Visual Studio Code")]
    [InlineData("WINWORD", "Microsoft Word")]
    [InlineData("WeChat", "微信")]
    [InlineData("explorer", "文件资源管理器")]
    public void GetFriendlyName_KnownApp_ReturnsMappedName(string processName, string expectedName)
    {
        var result = AppNameResolver.GetFriendlyName(processName);

        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void GetFriendlyName_EmptyString_ReturnsUnknown()
    {
        var result = AppNameResolver.GetFriendlyName(string.Empty);

        Assert.Equal("未知应用", result);
    }

    [Fact]
    public void GetFriendlyName_Null_ReturnsUnknown()
    {
        var result = AppNameResolver.GetFriendlyName(null!);

        Assert.Equal("未知应用", result);
    }

    [Fact]
    public void GetFriendlyName_CaseInsensitive()
    {
        var lower = AppNameResolver.GetFriendlyName("chrome");
        var upper = AppNameResolver.GetFriendlyName("CHROME");

        Assert.Equal(lower, upper);
    }

    [Fact]
    public void AddCustomMapping_OverridesExisting()
    {
        AppNameResolver.AddCustomMapping("test_custom_app", "My Custom App");

        var result = AppNameResolver.GetFriendlyName("test_custom_app");

        Assert.Equal("My Custom App", result);
    }
}
