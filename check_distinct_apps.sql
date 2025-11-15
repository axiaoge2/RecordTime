-- 查询今天有多少个不同的应用
SELECT COUNT(DISTINCT COALESCE(DisplayName, ProcessName)) as UniqueAppCount
FROM Sessions
WHERE date(StartTime) = date('now', 'localtime');

-- 查看 TOP 15 应用
SELECT
    COALESCE(DisplayName, ProcessName) as AppName,
    COUNT(*) as SessionCount,
    SUM(DurationSeconds) as TotalSeconds
FROM Sessions
WHERE date(StartTime) = date('now', 'localtime')
GROUP BY COALESCE(DisplayName, ProcessName)
ORDER BY TotalSeconds DESC
LIMIT 15;
