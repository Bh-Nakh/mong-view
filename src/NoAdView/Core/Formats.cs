namespace NoAdView.Core;

/// <summary>지원 확장자 목록.</summary>
public static class Formats
{
    // WIC 로 여는 형식. WebP·HEIC·AVIF 는 Windows 코덱 확장이 설치돼 있어야 열린다.
    public static readonly string[] ImageExts =
    {
        ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".gif", ".bmp", ".dib", ".tif", ".tiff",
        ".webp", ".heic", ".heif", ".avif", ".ico", ".jxr", ".wdp", ".hdp", ".dds",
    };

    public static readonly string[] ArchiveExts =
    {
        ".zip", ".cbz", ".rar", ".cbr", ".7z", ".cb7", ".tar", ".cbt",
    };

    static readonly HashSet<string> imageSet = new(ImageExts, StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> archiveSet = new(ArchiveExts, StringComparer.OrdinalIgnoreCase);

    public static bool IsImage(string path) => imageSet.Contains(Path.GetExtension(path ?? ""));
    public static bool IsArchive(string path) => archiveSet.Contains(Path.GetExtension(path ?? ""));
}
