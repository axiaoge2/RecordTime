-- 更新数据库中已有记录的 DisplayName
-- 将进程名转换为友好的显示名称

-- 浏览器
UPDATE Sessions SET DisplayName = 'Google Chrome' WHERE ProcessName = 'chrome' AND (DisplayName IS NULL OR DisplayName = 'chrome');
UPDATE Sessions SET DisplayName = 'Microsoft Edge' WHERE ProcessName = 'msedge' AND (DisplayName IS NULL OR DisplayName = 'msedge');
UPDATE Sessions SET DisplayName = 'Microsoft Edge' WHERE ProcessName = 'MicrosoftEdge' AND (DisplayName IS NULL OR DisplayName = 'MicrosoftEdge');
UPDATE Sessions SET DisplayName = 'Mozilla Firefox' WHERE ProcessName = 'firefox' AND (DisplayName IS NULL OR DisplayName = 'firefox');
UPDATE Sessions SET DisplayName = 'Brave Browser' WHERE ProcessName = 'brave' AND (DisplayName IS NULL OR DisplayName = 'brave');
UPDATE Sessions SET DisplayName = 'Opera' WHERE ProcessName = 'opera' AND (DisplayName IS NULL OR DisplayName = 'opera');

-- 开发工具
UPDATE Sessions SET DisplayName = 'Visual Studio' WHERE ProcessName = 'devenv' AND (DisplayName IS NULL OR DisplayName = 'devenv');
UPDATE Sessions SET DisplayName = 'Visual Studio Code' WHERE ProcessName = 'Code' AND (DisplayName IS NULL OR DisplayName = 'Code');
UPDATE Sessions SET DisplayName = 'JetBrains Rider' WHERE ProcessName = 'rider64' AND (DisplayName IS NULL OR DisplayName = 'rider64');
UPDATE Sessions SET DisplayName = 'IntelliJ IDEA' WHERE ProcessName = 'idea64' AND (DisplayName IS NULL OR DisplayName = 'idea64');
UPDATE Sessions SET DisplayName = 'PyCharm' WHERE ProcessName = 'pycharm64' AND (DisplayName IS NULL OR DisplayName = 'pycharm64');
UPDATE Sessions SET DisplayName = 'WebStorm' WHERE ProcessName = 'webstorm64' AND (DisplayName IS NULL OR DisplayName = 'webstorm64');
UPDATE Sessions SET DisplayName = 'Sublime Text' WHERE ProcessName = 'sublime_text' AND (DisplayName IS NULL OR DisplayName = 'sublime_text');
UPDATE Sessions SET DisplayName = 'Notepad++' WHERE ProcessName = 'notepad++' AND (DisplayName IS NULL OR DisplayName = 'notepad++');

-- 办公软件
UPDATE Sessions SET DisplayName = 'Microsoft Word' WHERE ProcessName = 'WINWORD' AND (DisplayName IS NULL OR DisplayName = 'WINWORD');
UPDATE Sessions SET DisplayName = 'Microsoft Excel' WHERE ProcessName = 'EXCEL' AND (DisplayName IS NULL OR DisplayName = 'EXCEL');
UPDATE Sessions SET DisplayName = 'Microsoft PowerPoint' WHERE ProcessName = 'POWERPNT' AND (DisplayName IS NULL OR DisplayName = 'POWERPNT');
UPDATE Sessions SET DisplayName = 'Microsoft Outlook' WHERE ProcessName = 'OUTLOOK' AND (DisplayName IS NULL OR DisplayName = 'OUTLOOK');
UPDATE Sessions SET DisplayName = 'Microsoft OneNote' WHERE ProcessName = 'ONENOTE' AND (DisplayName IS NULL OR DisplayName = 'ONENOTE');

-- 通讯工具
UPDATE Sessions SET DisplayName = '微信' WHERE ProcessName = 'WeChat' AND (DisplayName IS NULL OR DisplayName = 'WeChat');
UPDATE Sessions SET DisplayName = 'QQ' WHERE ProcessName = 'QQ' AND (DisplayName IS NULL OR DisplayName = 'QQ');
UPDATE Sessions SET DisplayName = '钉钉' WHERE ProcessName = 'DingTalk' AND (DisplayName IS NULL OR DisplayName = 'DingTalk');
UPDATE Sessions SET DisplayName = '飞书' WHERE ProcessName = 'Feishu' AND (DisplayName IS NULL OR DisplayName = 'Feishu');
UPDATE Sessions SET DisplayName = 'Slack' WHERE ProcessName = 'slack' AND (DisplayName IS NULL OR DisplayName = 'slack');
UPDATE Sessions SET DisplayName = 'Discord' WHERE ProcessName = 'discord' AND (DisplayName IS NULL OR DisplayName = 'discord');
UPDATE Sessions SET DisplayName = 'Telegram' WHERE ProcessName = 'Telegram' AND (DisplayName IS NULL OR DisplayName = 'Telegram');

-- 媒体播放器
UPDATE Sessions SET DisplayName = 'PotPlayer' WHERE ProcessName = 'PotPlayerMini64' AND (DisplayName IS NULL OR DisplayName = 'PotPlayerMini64');
UPDATE Sessions SET DisplayName = 'VLC Media Player' WHERE ProcessName = 'vlc' AND (DisplayName IS NULL OR DisplayName = 'vlc');
UPDATE Sessions SET DisplayName = 'Windows Media Player' WHERE ProcessName = 'wmplayer' AND (DisplayName IS NULL OR DisplayName = 'wmplayer');
UPDATE Sessions SET DisplayName = '网易云音乐' WHERE ProcessName = 'cloudmusic' AND (DisplayName IS NULL OR DisplayName = 'cloudmusic');
UPDATE Sessions SET DisplayName = 'QQ音乐' WHERE ProcessName = 'QQMusic' AND (DisplayName IS NULL OR DisplayName = 'QQMusic');

-- 其他常用应用
UPDATE Sessions SET DisplayName = '文件资源管理器' WHERE ProcessName = 'explorer' AND (DisplayName IS NULL OR DisplayName = 'explorer');
UPDATE Sessions SET DisplayName = '任务管理器' WHERE ProcessName = 'Taskmgr' AND (DisplayName IS NULL OR DisplayName = 'Taskmgr');
UPDATE Sessions SET DisplayName = '命令提示符' WHERE ProcessName = 'cmd' AND (DisplayName IS NULL OR DisplayName = 'cmd');
UPDATE Sessions SET DisplayName = 'PowerShell' WHERE ProcessName = 'powershell' AND (DisplayName IS NULL OR DisplayName = 'powershell');
UPDATE Sessions SET DisplayName = 'Windows Terminal' WHERE ProcessName = 'WindowsTerminal' AND (DisplayName IS NULL OR DisplayName = 'WindowsTerminal');
UPDATE Sessions SET DisplayName = 'Everything' WHERE ProcessName = 'Everything' AND (DisplayName IS NULL OR DisplayName = 'Everything');
UPDATE Sessions SET DisplayName = 'Typora' WHERE ProcessName = 'Typora' AND (DisplayName IS NULL OR DisplayName = 'Typora');
UPDATE Sessions SET DisplayName = 'Notion' WHERE ProcessName = 'notion' AND (DisplayName IS NULL OR DisplayName = 'notion');
UPDATE Sessions SET DisplayName = 'Obsidian' WHERE ProcessName = 'obsidian' AND (DisplayName IS NULL OR DisplayName = 'obsidian');
