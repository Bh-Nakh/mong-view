using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoAdView.Core;

public enum FitMode { Window, Width, Height, Original }
public enum Interp { Smooth, Sharpen, Pixel }

public sealed class Bookmark
{
    public string SourcePath { get; set; }
    public string Page { get; set; }
    public DateTime Added { get; set; }
}

public sealed class LastPosition
{
    public string SourcePath { get; set; }
    public string Page { get; set; }
    public DateTime Time { get; set; }
}

/// <summary>사용자 설정. %APPDATA%\NoAdView\settings.json 에 저장.</summary>
public sealed class Settings
{
    // 보기
    public FitMode Fit { get; set; } = FitMode.Window;
    public bool UpscaleSmall { get; set; } = true;
    public int PagesPerView { get; set; } = 1;
    public bool RightToLeft { get; set; }
    public bool CoverSingle { get; set; } = true;
    public bool AutoRotateExif { get; set; } = true;
    public Interp Interp { get; set; } = Interp.Smooth;

    // 이동
    public SortKey Sort { get; set; } = SortKey.Name;
    public bool SortDescending { get; set; }
    public bool Loop { get; set; }
    public bool AutoNextSource { get; set; } = true;
    public bool ArrowsFollowDirection { get; set; } = true;
    public bool ResumeLastPage { get; set; } = true;

    // 창
    public bool TopMost { get; set; }
    public bool ShowStatusBar { get; set; } = true;
    public bool ShowThumbs { get; set; }
    public double WinLeft { get; set; } = double.NaN;
    public double WinTop { get; set; } = double.NaN;
    public double WinWidth { get; set; } = 1100;
    public double WinHeight { get; set; } = 800;
    public bool WinMaximized { get; set; }

    // 파일 작업
    public bool ConfirmDelete { get; set; } = true;
    public string LastCopyDir { get; set; }
    public string LastMoveDir { get; set; }
    public int JpegQuality { get; set; } = 92;

    // 성능
    public int CacheMB { get; set; } = 768;

    public List<Bookmark> Bookmarks { get; set; } = new();
    public List<LastPosition> Recent { get; set; } = new();

    public const int MaxRecent = 200;

    public void RememberPosition(string sourcePath, string page)
    {
        if (string.IsNullOrEmpty(sourcePath) || page == null) return;
        Recent.RemoveAll(r => string.Equals(r.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase));
        Recent.Insert(0, new LastPosition { SourcePath = sourcePath, Page = page, Time = DateTime.Now });
        if (Recent.Count > MaxRecent) Recent.RemoveRange(MaxRecent, Recent.Count - MaxRecent);
    }

    public string LastPageOf(string sourcePath) =>
        Recent.FirstOrDefault(r => string.Equals(r.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))?.Page;
}

public static class SettingsStore
{
    static readonly JsonSerializerOptions json = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 한글을 그대로 저장
        Converters = { new JsonStringEnumConverter() },
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public static string DefaultDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoAdView");

    public static Settings Load(string dir)
    {
        try
        {
            string file = Path.Combine(dir, "settings.json");
            if (File.Exists(file))
                return Normalize(JsonSerializer.Deserialize<Settings>(File.ReadAllText(file), json) ?? new Settings());
        }
        catch { }
        return new Settings();
    }

    public static void Save(string dir, Settings s)
    {
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, "settings.json");
        string tmp = file + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(s, json));
        File.Move(tmp, file, true);
    }

    static Settings Normalize(Settings s)
    {
        if (s.PagesPerView is not (1 or 2 or 4)) s.PagesPerView = 1;
        s.CacheMB = Math.Clamp(s.CacheMB, 128, 8192);
        s.JpegQuality = Math.Clamp(s.JpegQuality, 1, 100);
        s.Bookmarks ??= new();
        s.Recent ??= new();
        return s;
    }
}
