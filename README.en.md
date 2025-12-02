<div align="center">

# RecordTime

<!-- ![Banner](docs/screenshots/banner.png) -->

**Smart Desktop Time Tracker** - Know where your time goes

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-7.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![GitHub Stars](https://img.shields.io/github/stars/axiaoge2/recordtime?style=social)](https://github.com/axiaoge2/recordtime)
[![GitHub Release](https://img.shields.io/github/v/release/axiaoge2/recordtime?include_prereleases)](https://github.com/axiaoge2/recordtime/releases)

**English** | [中文](README.md)

</div>

---

## Features

| Feature | Description |
|---------|-------------|
| **Auto Tracking** | Monitors active windows every 500ms, no manual input needed |
| **Activity Detection** | Smart detection of video, gaming, typing, browsing, idle states |
| **Time Budgets** | Set daily goals for apps/categories with smart reminders |
| **Visual Analytics** | Top apps ranking, usage statistics, HTML report generation |
| **AI Analysis** | OpenAI API integration for personalized time management insights |
| **Privacy First** | Local SQLite storage, SHA256 encrypted titles, no cloud uploads |
| **Multi-language** | Real-time switching between Chinese and English UI |
| **System Tray** | Background operation, auto-start, quick tray actions |

## Screenshots

<div align="center">

### Main Dashboard
![Dashboard](docs/screenshots/dashboard.png)
*Real-time app tracking, TOP 10 apps ranking, category distribution*

### Analytics
![Analytics](docs/screenshots/analytics.png)
![Report Generation](docs/screenshots/analytics1.png)
![AI Configuration](docs/screenshots/analytics2.png)
*HTML report generation, AI analysis, custom date range*

</div>

## Quick Start

### Option 1: Download Release

Download the latest version from [Releases](https://github.com/axiaoge2/recordtime/releases)

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/axiaoge2/recordtime.git
cd recordtime

# Restore dependencies
dotnet restore

# Build and run
dotnet build
dotnet run --project src/RecordTime.Avalonia
```

### Option 3: Publish Standalone

```bash
# Publish self-contained single-file executable (no .NET Runtime required)
dotnet publish src/RecordTime.Avalonia -c Release -r win-x64 --self-contained true
```

## System Requirements

| Item | Requirement |
|------|-------------|
| **OS** | Windows 10 Build 17763+ / Windows 11 |
| **Runtime** | .NET 7.0 Runtime (not needed for self-contained) |
| **Memory** | Minimum 512MB RAM |
| **Disk** | 100MB available space |

## Tech Stack

| Category | Technology |
|----------|------------|
| **Framework** | .NET 7.0 + Avalonia UI 11.x |
| **Database** | SQLite + Entity Framework Core 7.0 |
| **Charts** | LiveChartsCore.SkiaSharp 2.0 |
| **Architecture** | MVVM (CommunityToolkit.Mvvm) |
| **Logging** | Serilog (Console + File) |

## Core Features

### Activity Type Detection

Priority-based smart detection rules:

1. **Idle** - System idle > 5 minutes
2. **Video** - Media session playing / video app + audio
3. **Gaming** - Fullscreen + frequent input / gaming platform process
4. **ActiveTyping** - Keyboard > 20 keys/30s or keyboard+mouse combo
5. **PassiveBrowsing** - Window focused but low activity

### Time Budget System

- **App Budgets** - Set daily limits/goals for specific apps
- **Category Budgets** - Set goals by category (dev tools, entertainment, etc.)
- **Smart Reminders** - Notifications when approaching thresholds
- **Daily Summary** - Auto-generated end-of-day reports
- **AI Suggestions** - Smart goal recommendations based on usage history

### Data Integrity

- **Heartbeat Mechanism** - 30-second heartbeat prevents crash data loss
- **Auto Recovery** - Detects and fixes incomplete sessions on startup
- **Database Indexes** - Optimized query performance

## Privacy Protection

RecordTime is designed with privacy as a core principle:

- **Local Storage** - All data stored in `%LOCALAPPDATA%\RecordTime\`
- **Title Encryption** - Window titles stored as SHA256 hashes
- **No Cloud Sync** - No data uploaded to cloud by default
- **Optional AI** - AI analysis disabled by default, fully user-controlled
- **Data Sanitization** - URLs, emails auto-removed before AI analysis
- **Transparent Logs** - Detailed logs for auditing (`%LOCALAPPDATA%\RecordTime\Logs\`)

## Roadmap

### Phase 1 - Data Integrity ✅
- [x] Heartbeat mechanism for crash protection
- [x] Auto-recovery of incomplete sessions
- [x] Database index optimization

### Phase 2 - System Tray ✅
- [x] System tray integration
- [x] Minimize to tray / Auto-start
- [x] App icon extraction

### Phase 3 - UI/Analytics Enhancement ✅
- [x] Real-time monitoring dashboard
- [x] HTML report generation
- [x] AI analysis features
- [x] Multi-language support (CN/EN)

### Phase 4 - Time Budget System ✅
- [x] Usage time goal setting
- [x] Limit approaching notifications
- [x] Daily summary notifications
- [x] AI smart goal suggestions
- [x] Custom date range filtering

### Phase 5 - Advanced Features (Planned)
- [ ] Chart visualization (trends, pie charts)
- [ ] Focus mode
- [ ] Week/month comparison analysis
- [ ] Data export (CSV/Excel)
- [ ] PDF report generation

## Project Structure

```
RecordTime/
├── src/
│   ├── RecordTime.Avalonia/    # Avalonia UI Frontend
│   │   ├── Views/              # Page views
│   │   ├── ViewModels/         # MVVM view models
│   │   ├── Services/           # UI services
│   │   └── Resources/          # i18n resources
│   ├── RecordTime.Core/        # Core business logic
│   │   ├── Models/             # Data models
│   │   └── Services/           # Monitoring services
│   └── RecordTime.Data/        # Data access layer
│       ├── Repositories/       # Data repositories
│       └── Migrations/         # EF Core migrations
├── tools/                      # Verification and debug tools
└── docs/                       # Documentation
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed architecture design.

## Contributing

Issues and Pull Requests are welcome!

### Development Setup

```bash
# Clone repository
git clone https://github.com/axiaoge2/recordtime.git
cd recordtime

# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test

# Run application
dotnet run --project src/RecordTime.Avalonia
```

### Code Guidelines

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Commit message format: `feat:`, `fix:`, `docs:`, `refactor:`
- Run `dotnet format` before committing

See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## Documentation

| Document | Description |
|----------|-------------|
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Detailed technical architecture |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Contribution guidelines |
| [CHANGELOG.md](CHANGELOG.md) | Version changelog |

## License

This project is licensed under the [MIT License](LICENSE).

## Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform .NET UI framework
- [LiveCharts2](https://lvcharts.com/) - Data visualization library
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM toolkit
- Inspired by: [ActivityWatch](https://activitywatch.net/), RescueTime

---

<div align="center">

**If you find this project helpful, please consider giving it a Star!**

[![Star History Chart](https://api.star-history.com/svg?repos=axiaoge2/recordtime&type=Date)](https://star-history.com/#axiaoge2/recordtime&Date)

</div>
