namespace NoAdView.Core;

/// <summary>한 묶음(폴더 또는 압축 파일)의 장 목록.</summary>
public interface IPageSource : IDisposable
{
    /// <summary>폴더 경로 또는 압축 파일 경로.</summary>
    string Path { get; }
    bool IsArchive { get; }
    IReadOnlyList<PageEntry> Pages { get; }
}

/// <summary>폴더 안 이미지 파일들(하위 폴더는 보지 않음).</summary>
public sealed class FolderSource : IPageSource
{
    public string Path { get; }
    public bool IsArchive => false;
    public IReadOnlyList<PageEntry> Pages { get; }

    public FolderSource(string folder, SortKey sort, bool descending)
    {
        Path = System.IO.Path.GetFullPath(folder);
        var list = new List<PageEntry>();
        foreach (var f in new DirectoryInfo(Path).EnumerateFiles())
        {
            if (!Formats.IsImage(f.Name)) continue;
            if ((f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;
            string full = f.FullName;
            list.Add(new PageEntry
            {
                Name = f.Name,
                FilePath = full,
                Size = f.Length,
                Modified = f.LastWriteTime,
                Open = () => new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1 << 16),
            });
        }
        NaturalSort.Sort(list, sort, descending);
        Pages = list;
    }

    public static bool HasImages(string folder)
    {
        try { return Directory.EnumerateFiles(folder).Any(Formats.IsImage); }
        catch { return false; }
    }

    public void Dispose() { }
}

public static class PageSources
{
    /// <summary>경로 종류에 맞는 원본을 연다. 이미지 파일이면 그 폴더를 열고 startName 에 파일 이름을 돌려준다.</summary>
    public static IPageSource Open(string path, SortKey sort, bool desc, out string startName)
    {
        startName = null;
        if (Directory.Exists(path)) return new FolderSource(path, sort, desc);
        if (!File.Exists(path)) throw new FileNotFoundException("파일을 찾을 수 없습니다.", path);
        if (Formats.IsArchive(path)) return new ArchiveSource(path, sort, desc);
        if (Formats.IsImage(path))
        {
            startName = System.IO.Path.GetFileName(path);
            return new FolderSource(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)), sort, desc);
        }
        throw new NotSupportedException("지원하지 않는 형식입니다: " + System.IO.Path.GetExtension(path));
    }

    /// <summary>같은 상위 폴더에서 다음(또는 이전) 폴더·압축 파일을 찾는다. 이미지가 없는 폴더는 건너뛴다.</summary>
    public static string FindSibling(string sourcePath, int dir)
    {
        string full = System.IO.Path.GetFullPath(sourcePath).TrimEnd('\\');
        string parent = System.IO.Path.GetDirectoryName(full);
        if (parent == null) return null;
        List<string> items;
        try
        {
            items = new DirectoryInfo(parent).EnumerateFileSystemInfos()
                .Where(i => (i.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                .Where(i => i is DirectoryInfo || Formats.IsArchive(i.Name))
                .Select(i => i.FullName)
                .ToList();
        }
        catch { return null; }
        items.Sort((a, b) => NaturalSort.Compare(System.IO.Path.GetFileName(a), System.IO.Path.GetFileName(b)));
        int idx = items.FindIndex(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return null;
        for (int i = idx + dir; i >= 0 && i < items.Count; i += dir)
        {
            string p = items[i];
            if (File.Exists(p) || FolderSource.HasImages(p)) return p;
        }
        return null;
    }
}
