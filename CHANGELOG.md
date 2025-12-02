# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Focus mode (planned)
- Week/month comparison analysis (planned)
- Data export to CSV/Excel (planned)
- PDF report generation (planned)

## [1.0.0] - 2024-12-02

### Added

#### Phase 4 - Time Budget System
- Time budget management for apps and categories
- Smart reminder notifications when approaching limits
- Daily summary notifications
- AI-powered goal suggestions based on usage patterns
- Custom date range filtering for reports

#### Phase 3 - UI/Analytics Enhancement
- Real-time monitoring dashboard with live updates
- 7-day usage trend charts (LiveCharts2)
- Activity type distribution pie charts
- AI analysis with OpenAI API integration
- Multi-language support (Chinese/English)
- Settings page with language selection

#### Phase 2 - System Tray Integration
- System tray icon with context menu
- Minimize to tray behavior
- Auto-start on Windows boot
- App icon extraction for display

#### Phase 1 - Data Integrity
- 30-second heartbeat mechanism to prevent data loss
- Auto-recovery of incomplete sessions on startup
- Database index optimization for query performance
- Verification tools for debugging

### Core Features
- Automatic window tracking (500ms polling)
- 5 activity types: Video, Gaming, ActiveTyping, PassiveBrowsing, Idle
- Local SQLite database storage
- SHA256 window title encryption for privacy
- Application categorization (Dev Tools, Office, Entertainment, etc.)

### Technical
- .NET 7.0 + Avalonia UI 11.x
- Entity Framework Core 7.0
- MVVM architecture with CommunityToolkit.Mvvm
- Serilog logging (Console + File)

---

## Version History Summary

| Version | Date | Highlights |
|---------|------|------------|
| 1.0.0 | 2024-12-02 | First public release with full feature set |

[Unreleased]: https://github.com/axiaoge2/recordtime/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/axiaoge2/recordtime/releases/tag/v1.0.0
