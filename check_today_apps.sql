-- 查询今天的应用数量
SELECT
    COALESCE(DisplayName, ProcessName) as AppName,
    COUNT(*) as SessionCount,
    SUM(DurationSeconds) as TotalSeconds
FROM Sessions
WHERE date(StartTime) = date('now', 'localtime')
GROUP BY COALESCE(DisplayName, ProcessName)
ORDER BY TotalSeconds DESC;
