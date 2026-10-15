; 몽뷰 설치 스크립트 (Inno Setup 6). release.bat 이 /DSrcDir=<최종 빌드 폴더> 로 실행한다.
#ifndef SrcDir
  #define SrcDir "..\release"
#endif
#define AppName "몽뷰"
#define AppExe "몽뷰.exe"
#define AppVersion "0.1.4"

[Setup]
AppId={{3B7E9D12-5A4C-4F68-8E21-C0D9A6B14F35}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=NoAd
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\setup
OutputBaseFilename=몽뷰-Setup-{#AppVersion}
SetupIconFile=..\res\app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ChangesAssociations=yes
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoCompany=NoAd

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SrcDir}\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; 이미지·압축 파일과 폴더의 우클릭 메뉴 "몽뷰(으)로 열기" (제거 시 함께 삭제)
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jpg\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jpg\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jpg\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jpeg\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jpeg\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jpeg\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jpe\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jpe\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jpe\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jfif\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jfif\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jfif\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.png\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.png\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.png\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.gif\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.gif\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.gif\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.bmp\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.bmp\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.bmp\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.dib\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.dib\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.dib\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.tif\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.tif\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.tif\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.tiff\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.tiff\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.tiff\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.webp\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.webp\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.webp\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.heic\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.heic\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.heic\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.heif\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.heif\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.heif\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.avif\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.avif\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.avif\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ico\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ico\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ico\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jxr\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jxr\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.jxr\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wdp\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wdp\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wdp\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.hdp\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.hdp\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.hdp\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.dds\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.dds\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.dds\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.zip\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.zip\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.zip\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cbz\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cbz\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cbz\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.rar\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.rar\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.rar\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cbr\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cbr\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cbr\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.7z\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.7z\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.7z\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cb7\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cb7\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cb7\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.tar\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.tar\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.tar\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cbt\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cbt\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.cbt\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Directory\shell\NoAdView"; ValueType: string; ValueName: ""; ValueData: "{#AppName}(으)로 열기"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Directory\shell\NoAdView"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#AppExe}"",0"
Root: HKA; Subkey: "Software\Classes\Directory\shell\NoAdView\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""

[InstallDelete]
; 이름이 NoAdView 였던 옛 버전이 남긴 파일·바로가기를 정리한다 (같은 AppId 라 옛 설치 폴더에 그대로 덮어 설치됨).
Type: files; Name: "{app}\NoAdView.exe"
Type: files; Name: "{autoprograms}\NoAdView.lnk"
Type: files; Name: "{autodesktop}\NoAdView.lnk"

[Registry]
Root: HKA; Subkey: "Software\Classes\Applications\NoAdView.exe"; Flags: dontcreatekey deletekey

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
