using RecordTime.Core.Services;
using Xunit;

namespace RecordTime.Tests.Core;

public class AppSettingsServiceTests
{
    [Fact]
    public async Task GetIdleTimeoutSeconds_UsesPersistedMinutes()
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), $"RecordTime.Tests.{Guid.NewGuid():N}.json");

        try
        {
            var service = new AppSettingsService(settingsPath);

            await service.UpdateSettingsAsync(s => s.Monitoring.IdleTimeoutMinutes = 30);

            Assert.Equal(1800, service.GetIdleTimeoutSeconds());
        }
        finally
        {
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }
        }
    }
}
