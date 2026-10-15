using System.Text;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace NoAdView.Core;

/// <summary>
/// 압축 파일 안 이미지들을 풀지 않고 연다.
/// 일반 압축(ZIP 등)은 필요한 항목만 그때그때 읽고,
/// 솔리드 압축(대부분의 7Z, 솔리드 RAR)은 처음부터 순서대로 풀어야 하므로
/// 백그라운드에서 임시 폴더로 차례로 풀어 두고 다 풀린 항목부터 보여 준다.
/// </summary>
public sealed class ArchiveSource : IPageSource
{
    static ArchiveSource() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public string Path { get; }
    public bool IsArchive => true;
    public IReadOnlyList<PageEntry> Pages { get; }

    readonly IArchive archive;
    readonly object gate = new();

    // 솔리드 압축용
    readonly bool solid;
    string tempDir;
    Thread extractThread;
    volatile bool cancel;
    readonly Dictionary<string, Slot> slots = new(StringComparer.Ordinal);

    sealed class Slot
    {
        public string TempFile;
        public readonly ManualResetEventSlim Ready = new(false);
        public Exception Error;
    }

    public ArchiveSource(string path, SortKey sort, bool descending)
    {
        Path = System.IO.Path.GetFullPath(path);
        // ZIP 의 한글 파일명은 UTF-8 표시가 없으면 CP949 로 읽는다.
        var options = new ReaderOptions
        {
            ArchiveEncoding = new ArchiveEncoding { Default = Encoding.GetEncoding(949) },
        };
        archive = ArchiveFactory.OpenArchive(Path, options);
        try
        {
            var entries = archive.Entries
                .Where(e => !e.IsDirectory && Formats.IsImage(e.Key))
                .ToList();
            if (entries.Count > 0 && entries.Any(e => e.IsEncrypted))
                throw new NotSupportedException("암호가 걸린 압축 파일은 지원하지 않습니다.");

            solid = archive.IsSolid || entries.Any(e => e.IsSolid);
            var list = new List<PageEntry>(entries.Count);
            foreach (var e in entries)
            {
                string key = e.Key.Replace('\\', '/');
                var entry = e;
                list.Add(new PageEntry
                {
                    Name = key,
                    Size = e.Size,
                    Modified = e.LastModifiedTime ?? DateTime.MinValue,
                    Open = solid ? () => OpenSolid(key) : () => OpenDirect(entry),
                });
                if (solid) slots[key] = new Slot();
            }
            NaturalSort.Sort(list, sort, descending);
            Pages = list;
            if (solid && list.Count > 0) StartExtract();
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    Stream OpenDirect(IArchiveEntry entry)
    {
        // SharpCompress 압축 객체는 여러 스레드에서 동시에 읽으면 안 된다.
        lock (gate)
        {
            if (cancel) throw new ObjectDisposedException(nameof(ArchiveSource));
            var ms = new MemoryStream(entry.Size > 0 && entry.Size < int.MaxValue ? (int)entry.Size : 0);
            using (var s = entry.OpenEntryStream()) s.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }
    }

    Stream OpenSolid(string key)
    {
        var slot = slots[key];
        while (!slot.Ready.Wait(200))
            if (cancel) throw new ObjectDisposedException(nameof(ArchiveSource));
        if (slot.Error != null) throw new IOException("압축을 푸는 중 오류: " + slot.Error.Message, slot.Error);
        return new FileStream(slot.TempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }

    void StartExtract()
    {
        tempDir = System.IO.Path.Combine(TempRoot, Environment.ProcessId + "_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        extractThread = new Thread(ExtractAll) { IsBackground = true, Name = "NoAdView solid extract" };
        extractThread.Start();
    }

    void ExtractAll()
    {
        int n = 0;
        try
        {
            lock (gate)
            {
                using var reader = archive.ExtractAllEntries();
                while (!cancel && reader.MoveToNextEntry())
                {
                    var e = reader.Entry;
                    if (e.IsDirectory) continue;
                    string key = e.Key?.Replace('\\', '/');
                    if (key == null || !slots.TryGetValue(key, out var slot) || slot.Ready.IsSet) continue;
                    string file = System.IO.Path.Combine(tempDir, (n++).ToString("D6") + ".bin");
                    try
                    {
                        using (var fs = File.Create(file)) reader.WriteEntryTo(fs);
                        slot.TempFile = file;
                    }
                    catch (Exception ex) { slot.Error = ex; }
                    slot.Ready.Set();
                }
            }
        }
        catch (Exception ex)
        {
            foreach (var s in slots.Values)
                if (!s.Ready.IsSet) { s.Error = ex; s.Ready.Set(); }
        }
        // 목록에는 있었는데 풀리지 않은 항목은 오류로 끝낸다(무한 대기 방지).
        foreach (var s in slots.Values)
            if (!s.Ready.IsSet) { s.Error ??= new IOException("항목을 찾지 못했습니다."); s.Ready.Set(); }
    }

    public static string TempRoot => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NoAdView");

    /// <summary>이전 실행이 비정상 종료돼 남긴 임시 폴더 정리(하루 지난 것만).</summary>
    public static void CleanStaleTemp()
    {
        try
        {
            if (!Directory.Exists(TempRoot)) return;
            foreach (var d in Directory.GetDirectories(TempRoot))
                if (Directory.GetLastWriteTime(d) < DateTime.Now.AddDays(-1))
                    try { Directory.Delete(d, true); } catch { }
        }
        catch { }
    }

    public void Dispose()
    {
        cancel = true;
        extractThread?.Join(5000);
        lock (gate) archive.Dispose();
        if (tempDir != null)
            try { Directory.Delete(tempDir, true); } catch { }
    }
}
