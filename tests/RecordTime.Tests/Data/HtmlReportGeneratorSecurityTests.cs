using System.Dynamic;
using System.Reflection;
using RecordTime.Data.Reports;
using Xunit;

namespace RecordTime.Tests.Data;

public class HtmlReportGeneratorSecurityTests
{
    [Fact]
    public void GenerateHtml_EscapesAppAndCategoryNamesInHtmlAndChartLabels()
    {
        var appName = "x'<img src=x onerror=alert(document.cookie)>";
        var categoryName = "dev'</script><script>alert(document.cookie)</script>";

        dynamic app = new ExpandoObject();
        app.AppName = appName;
        app.Category = categoryName;
        app.TotalSeconds = 3600;
        app.SessionCount = 1;
        app.Percentage = 100.0;

        dynamic category = new ExpandoObject();
        category.Category = categoryName;
        category.TotalSeconds = 3600;
        category.Percentage = 100.0;

        var generator = new HtmlReportGenerator(null!);
        var method = typeof(HtmlReportGenerator).GetMethod(
            "GenerateHtml",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var html = Assert.IsType<string>(method!.Invoke(generator, new object?[]
        {
            new DateTime(2026, 8, 18),
            new DateTime(2026, 8, 18),
            1.0,
            1,
            new[] { app },
            new[] { category },
            null
        }));

        Assert.DoesNotContain(appName, html);
        Assert.DoesNotContain(categoryName, html);
        Assert.Contains("&lt;img src=x onerror=alert(document.cookie)&gt;", html);
        Assert.Contains("\\u003Cimg src=x onerror=alert(document.cookie)\\u003E", html);
    }
}
