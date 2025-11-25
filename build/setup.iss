; RecordTime Inno Setup 安装脚本
; 用于创建 Windows 安装程序

#define MyAppName "RecordTime"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "RecordTime"
#define MyAppURL "https://github.com/yourusername/recordtime"
#define MyAppExeName "RecordTime.exe"
#define MyAppDescription "Windows桌面应用使用时长追踪工具"

[Setup]
; 应用程序基本信息
AppId={{A5B3C4D5-E6F7-4A8B-9C0D-1E2F3A4B5C6D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppComments={#MyAppDescription}

; 安装路径
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; 输出设置
OutputDir=..\build\publish
OutputBaseFilename=RecordTime-v{#MyAppVersion}-Setup
SetupIconFile=..\src\RecordTime.Avalonia\Assets\Icons\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; 压缩设置
Compression=lzma2/max
SolidCompression=yes

; 权限设置
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; 架构设置
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; 界面设置
WizardStyle=modern
DisableWelcomePage=no

; 卸载设置
UninstallDisplayName={#MyAppName}
UninstallFilesDir={app}\uninstall

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; 主程序文件
Source: "..\build\output\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; 配置文件
Source: "..\build\output\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion

; 图标文件
Source: "..\build\output\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; 开始菜单快捷方式
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; 桌面快捷方式
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; 快速启动快捷方式
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; 安装完成后运行程序
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; 卸载时删除用户数据目录（可选）
Type: filesandordirs; Name: "{localappdata}\RecordTime"

[Code]
// 检查是否已安装旧版本
function InitializeSetup(): Boolean;
var
  OldVersion: String;
  UninstallString: String;
  ErrorCode: Integer;
begin
  Result := True;

  // 检查注册表中的旧版本
  if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1',
     'UninstallString', UninstallString) then
  begin
    if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1',
       'DisplayVersion', OldVersion) then
    begin
      if MsgBox('检测到已安装 ' + '{#MyAppName}' + ' ' + OldVersion + #13#10#13#10 +
                '建议先卸载旧版本。是否继续安装？', mbConfirmation, MB_YESNO) = IDYES then
      begin
        Result := True;
      end
      else
      begin
        Result := False;
      end;
    end;
  end;
end;

// 安装前检查应用是否正在运行
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';

  // 尝试关闭正在运行的应用
  if CheckForMutexes('{#MyAppName}Mutex') then
  begin
    Result := '检测到 {#MyAppName} 正在运行。' + #13#10 +
              '请先关闭应用程序后再继续安装。';
  end;
end;
