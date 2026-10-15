using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace NoAdView.Core;

/// <summary>
/// 탐색기 연결(현재 사용자 범위, HKCU 만 사용).
/// Windows 10 부터는 프로그램이 기본 앱을 직접 바꿀 수 없으므로,
/// "연결 프로그램" 목록과 기본 앱 설정에 몽뷰를 등록하고 기본 앱 화면을 열어 사용자가 고르게 한다.
/// </summary>
public static class FileAssoc
{
    const string AppKey = "NoAdView";
    const string ImageProgId = "NoAdView.Image";
    const string ArchiveProgId = "NoAdView.Archive";
    const string Classes = @"Software\Classes";
    const string CapPath = @"Software\NoAdView\Capabilities";

    const string MenuKeyName = "NoAdView";
    const string MenuText = "몽뷰(으)로 열기";

    public static string ExePath => Environment.ProcessPath;

    static string ExeName => Path.GetFileName(ExePath);
    const string LegacyExeName = "NoAdView.exe";

    static string OpenCommand => $"\"{ExePath}\" \"%1\"";

    /// <summary>우클릭 메뉴가 현재 실행 파일 경로로 등록돼 있는지.</summary>
    public static bool IsContextMenuRegistered()
    {
        using var k = Registry.CurrentUser.OpenSubKey($@"{Classes}\SystemFileAssociations\.jpg\shell\{MenuKeyName}\command");
        return string.Equals(k?.GetValue(null) as string, OpenCommand, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 이미지·압축 파일과 폴더의 우클릭 메뉴에 "몽뷰(으)로 열기"를 등록한다(HKCU).
    /// 이미 같은 경로로 등록돼 있으면 아무것도 하지 않는다.
    /// </summary>
    public static void EnsureContextMenu()
    {
        if (IsContextMenuRegistered()) return;
        foreach (var ext in AllExts())
            WriteMenu($@"{Classes}\SystemFileAssociations\{ext}\shell\{MenuKeyName}");
        WriteMenu($@"{Classes}\Directory\shell\{MenuKeyName}");
        NotifyShell();
    }

    public static void RemoveContextMenu()
    {
        foreach (var ext in AllExts())
            Registry.CurrentUser.DeleteSubKeyTree($@"{Classes}\SystemFileAssociations\{ext}\shell\{MenuKeyName}", false);
        Registry.CurrentUser.DeleteSubKeyTree($@"{Classes}\Directory\shell\{MenuKeyName}", false);
        NotifyShell();
    }

    static void WriteMenu(string keyPath)
    {
        using var k = Registry.CurrentUser.CreateSubKey(keyPath);
        k.SetValue(null, MenuText);
        k.SetValue("Icon", $"\"{ExePath}\",0");
        using var c = k.CreateSubKey("command");
        c.SetValue(null, OpenCommand);
    }

    public static bool IsRegistered()
    {
        using var k = Registry.CurrentUser.OpenSubKey($@"{Classes}\{ImageProgId}\shell\open\command");
        var v = k?.GetValue(null) as string;
        return v != null && v.Contains(ExePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void Register()
    {
        string exe = ExePath;
        string cmd = $"\"{exe}\" \"%1\"";

        WriteProgId(ImageProgId, "몽뷰 이미지", exe, cmd);
        WriteProgId(ArchiveProgId, "몽뷰 압축 만화", exe, cmd);

        // 실행 파일 이름이 몽뷰.exe 로 바뀌었으므로 옛 이름(NoAdView.exe)으로 등록한 항목은 지운다.
        Registry.CurrentUser.DeleteSubKeyTree($@"{Classes}\Applications\{LegacyExeName}", false);
        using (var app = Registry.CurrentUser.CreateSubKey($@"{Classes}\Applications\{ExeName}"))
        {
            app.SetValue("FriendlyAppName", "몽뷰");
            using (var c = app.CreateSubKey(@"shell\open\command")) c.SetValue(null, cmd);
            using var st = app.CreateSubKey("SupportedTypes");
            foreach (var ext in AllExts()) st.SetValue(ext, "");
        }

        using (var cap = Registry.CurrentUser.CreateSubKey(CapPath))
        {
            cap.SetValue("ApplicationName", "몽뷰");
            cap.SetValue("ApplicationDescription", "광고 없는 이미지·만화 뷰어");
            cap.SetValue("ApplicationIcon", $"\"{exe}\",0");
            using var fa = cap.CreateSubKey("FileAssociations");
            foreach (var ext in Formats.ImageExts) fa.SetValue(ext, ImageProgId);
            foreach (var ext in Formats.ArchiveExts) fa.SetValue(ext, ArchiveProgId);
        }
        using (var ra = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
            ra.SetValue(AppKey, CapPath);

        foreach (var ext in Formats.ImageExts) AddOpenWith(ext, ImageProgId);
        foreach (var ext in Formats.ArchiveExts) AddOpenWith(ext, ArchiveProgId);

        EnsureContextMenu();
        NotifyShell();
    }

    public static void Unregister()
    {
        RemoveContextMenu();
        foreach (var ext in Formats.ImageExts) RemoveOpenWith(ext, ImageProgId);
        foreach (var ext in Formats.ArchiveExts) RemoveOpenWith(ext, ArchiveProgId);
        Registry.CurrentUser.DeleteSubKeyTree($@"{Classes}\{ImageProgId}", false);
        Registry.CurrentUser.DeleteSubKeyTree($@"{Classes}\{ArchiveProgId}", false);
        Registry.CurrentUser.DeleteSubKeyTree($@"{Classes}\Applications\{ExeName}", false);
        Registry.CurrentUser.DeleteSubKeyTree($@"{Classes}\Applications\{LegacyExeName}", false);
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\NoAdView", false);
        using (var ra = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", true))
            ra?.DeleteValue(AppKey, false);
        NotifyShell();
    }

    /// <summary>Windows 기본 앱 설정 화면을 연다.</summary>
    public static void OpenDefaultAppsSettings()
    {
        Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
    }

    static IEnumerable<string> AllExts() => Formats.ImageExts.Concat(Formats.ArchiveExts);

    static void WriteProgId(string progId, string name, string exe, string cmd)
    {
        using var k = Registry.CurrentUser.CreateSubKey($@"{Classes}\{progId}");
        k.SetValue(null, name);
        k.SetValue("FriendlyTypeName", name);
        using (var icon = k.CreateSubKey("DefaultIcon")) icon.SetValue(null, $"\"{exe}\",0");
        using var c = k.CreateSubKey(@"shell\open\command");
        c.SetValue(null, cmd);
    }

    static void AddOpenWith(string ext, string progId)
    {
        using var k = Registry.CurrentUser.CreateSubKey($@"{Classes}\{ext}\OpenWithProgids");
        k.SetValue(progId, Array.Empty<byte>(), RegistryValueKind.None);
    }

    static void RemoveOpenWith(string ext, string progId)
    {
        using var k = Registry.CurrentUser.OpenSubKey($@"{Classes}\{ext}\OpenWithProgids", true);
        k?.DeleteValue(progId, false);
    }

    [DllImport("shell32.dll")]
    static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

    static void NotifyShell() => SHChangeNotify(0x08000000 /* SHCNE_ASSOCCHANGED */, 0, IntPtr.Zero, IntPtr.Zero);
}
