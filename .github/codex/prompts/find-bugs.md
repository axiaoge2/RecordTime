# Daily verified bug hunt

Inspect this repository for real, user-impacting defects. This is a proactive
health check, not a style review.

Repository context:

- `RecordTime.sln` is a .NET 7 Windows desktop solution.
- `src/RecordTime.Avalonia` contains the Avalonia MVVM application.
- `src/RecordTime.Core` contains monitoring and business logic.
- `src/RecordTime.Data` contains EF Core and SQLite persistence.
- Tests are under `tests/RecordTime.Tests` and use xUnit.
- Window titles, URLs, logs, and SQLite data may contain private information.

Use shell tools to inspect relevant code and tests. Prioritize hidden defects
with meaningful impact: data loss or corruption, privacy leaks, lifecycle and
resource errors, race conditions, incorrect time/session accounting, and
failures at external or persistence boundaries. Check nearby code before
concluding that a suspicious line is defective.

Rules:

- Do not modify any file.
- Report zero findings when evidence is insufficient.
- Report at most 10 findings, ordered by severity and confidence.
- Only report high-confidence, independently actionable bugs.
- Do not report style, naming, missing comments, broad refactors, feature
  requests, speculative risks, or duplicate manifestations of one root cause.
- Every finding must cite an existing repository-relative file and exact line.
- Reproduction must describe a concrete code path, input, or state transition.

Return only one JSON object matching `.github/codex/findings.schema.json`.
Do not wrap it in Markdown or add commentary.
