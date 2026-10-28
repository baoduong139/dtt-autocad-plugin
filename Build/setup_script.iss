; =====================================================================
; DTT CAD TOOLSET PRO - INNO SETUP COMPILER SCRIPT (v2026.2.0)
; Tự động cài đặt vào %APPDATA%\Autodesk\ApplicationPlugins\DTT_Tools.bundle
; Tương thích AutoCAD 2021 (R24.0) & AutoCAD Electrical 2021
; =====================================================================

#define MyAppName "DTT CAD TOOLSET PRO"
#define MyAppVersion "2026.2.0"
#define MyAppPublisher "DTT CAD Software Corporation"
#define MyAppURL "https://github.com/baoduong139/dtt-autocad-plugin"

[Setup]
AppId={{9F3A5812-78E2-4C9D-9F01-7F23E4B8C0D1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userappdata}\Autodesk\ApplicationPlugins\DTT_Tools.bundle
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist\release
OutputBaseFilename=DTT_Tools_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\DTT_Tools.bundle\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}\DTT TOOLS Manager"; Filename: "{app}\Contents\DTT_Manager.exe"; WorkingDir: "{app}\Contents"
Name: "{userdesktop}\DTT TOOLS Manager"; Filename: "{app}\Contents\DTT_Manager.exe"; WorkingDir: "{app}\Contents"
Name: "{autoprograms}\{#MyAppName}\Gỡ Cài Đặt {#MyAppName}"; Filename: "{uninstallexe}"

[Messages]
SetupAppTitle=Bộ Cài Đặt DTT CAD TOOLSET PRO (AutoCAD 2021)
SetupWindowTitle=Cài đặt {#MyAppName} - Plug-and-Play AutoCAD Toolset
WelcomeLabel1=Chào mừng bạn đến với bộ cài đặt {#MyAppName} v{#MyAppVersion}
WelcomeLabel2=Phần mềm sẽ tự động tích hợp vào AutoCAD 2021 & AutoCAD Electrical 2021.%n%nSau khi cài đặt xong, bạn có thể quản lý qua DTT TOOLS Manager hoặc mở AutoCAD để sử dụng ngay lập tức!
FinishedHeadingLabel=Cài Đặt Hoàn Tất Thành Công!
FinishedLabel=Bộ công cụ DTT CAD TOOLSET PRO đã sẵn sàng trong AutoCAD.%n%nShortcut DTT TOOLS Manager đã được tạo trên màn hình Desktop!
