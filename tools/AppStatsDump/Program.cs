using RecordTime.Avalonia.Services;

var date = DateTime.Today;
if (args.Length > 0 && DateTime.TryParse(args[0], out var parsed))
{
    date = parsed.Date;
}

Console.WriteLine($"Dumping apps for {date:yyyy-MM-dd}...");

var snapshot = await AppDataService.Instance.GetSnapshotAsync(date, forceRefresh: true);

Console.WriteLine($"Total apps: {snapshot.AllApps.Count}");

int index = 1;
foreach (var app in snapshot.AllApps)
{
    Console.WriteLine($"{index,3}. {app.AppName,-25} | {app.Category,-10} | {app.TotalDuration:hh\\:mm\\:ss} | {app.TotalPercentage:F1}%");
    index++;
}

// Also instantiate the AppStatsViewModel to verify filtering behavior
var viewModel = new RecordTime.Avalonia.ViewModels.AppStatsViewModel();

// Wait for initial load to complete
for (var i = 0; i < 50 && viewModel.Apps.Count == 0; i++)
{
    await Task.Delay(100);
}

Console.WriteLine($"ViewModel Apps Count: {viewModel.Apps.Count}");
index = 1;
foreach (var item in viewModel.Apps)
{
    Console.WriteLine($"VM {index,3}. {item.AppName,-25} | {item.Category,-10} | {item.DurationText} | {item.PercentageText}");
    index++;
}
