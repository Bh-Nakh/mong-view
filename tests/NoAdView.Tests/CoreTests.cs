using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoAdView.Core;
using Xunit;

namespace NoAdView.Tests;

public class NaturalSortTests
{
    [Fact]
    public void 숫자는_값_순서로_정렬한다()
    {
        var names = new List<string> { "10.jpg", "2.jpg", "1.jpg", "img20.png", "img3.png" };
        names.Sort(NaturalSort.Comparer);
        Assert.Equal(new[] { "1.jpg", "2.jpg", "10.jpg", "img3.png", "img20.png" }, names);
    }

    [Fact]
    public void 크기순과_거꾸로_정렬()
    {
        var pages = new List<PageEntry>
        {
            new() { Name = "a", Size = 30 }, new() { Name = "b", Size = 10 }, new() { Name = "c", Size = 20 },
        };
        NaturalSort.Sort(pages, SortKey.Size, descending: false);
        Assert.Equal("b,c,a", string.Join(",", pages.Select(p => p.Name)));
        NaturalSort.Sort(pages, SortKey.Size, descending: true);
        Assert.Equal("a,c,b", string.Join(",", pages.Select(p => p.Name)));
    }
}

public class SpreadLayoutTests
{
    static string Show(List<int[]> v) => string.Join(" ", v.Select(x => "[" + string.Join(",", x) + "]"));

    [Fact]
    public void 두장_표지단독() => Assert.Equal("[0] [1,2] [3,4]", Show(SpreadLayout.Build(5, 2, true)));

    [Fact]
    public void 두장_표지없이() => Assert.Equal("[0,1] [2,3] [4]", Show(SpreadLayout.Build(5, 2, false)));

    [Fact]
    public void 네장() => Assert.Equal("[0,1,2,3] [4,5,6,7] [8]", Show(SpreadLayout.Build(9, 4, true)));

    [Fact]
    public void 한장_표지설정_무시() => Assert.Equal("[0] [1] [2]", Show(SpreadLayout.Build(3, 1, true)));

    [Fact]
    public void 빈목록() => Assert.Empty(SpreadLayout.Build(0, 2, true));

    [Fact]
    public void 오른쪽에서_왼쪽_배치()
    {
        Assert.Equal(new[] { 2, 1 }, SpreadLayout.Arrange(new[] { 1, 2 }, 2, true));
        Assert.Equal(new[] { 1, 2 }, SpreadLayout.Arrange(new[] { 1, 2 }, 2, false));
        Assert.Equal(new[] { 1, 0, 3, 2 }, SpreadLayout.Arrange(new[] { 0, 1, 2, 3 }, 4, true));
        Assert.Equal(new[] { 5, 4, 6 }, SpreadLayout.Arrange(new[] { 4, 5, 6 }, 4, true));
        Assert.Equal(new[] { 7 }, SpreadLayout.Arrange(new[] { 7 }, 2, true));
    }

    [Fact]
    public void 장번호로_뷰찾기()
    {
        var v = SpreadLayout.Build(5, 2, true);
        Assert.Equal(0, SpreadLayout.ViewOf(v, 0));
        Assert.Equal(1, SpreadLayout.ViewOf(v, 2));
        Assert.Equal(2, SpreadLayout.ViewOf(v, 3));
    }
}

public class SettingsTests
{
    [Fact]
    public void 저장후_다시읽기()
    {
        string dir = TestUtil.TempDir();
        var s = new Settings { PagesPerView = 2, RightToLeft = true, Fit = FitMode.Width, Sort = SortKey.Modified };
        s.Bookmarks.Add(new Bookmark { SourcePath = @"C:\만화\1권.zip", Page = "001.jpg", Added = new DateTime(2026, 1, 2) });
        s.RememberPosition(@"C:\만화\1권.zip", "010.jpg");
        SettingsStore.Save(dir, s);

        var r = SettingsStore.Load(dir);
        Assert.Equal(2, r.PagesPerView);
        Assert.True(r.RightToLeft);
        Assert.Equal(FitMode.Width, r.Fit);
        Assert.Equal(SortKey.Modified, r.Sort);
        Assert.Single(r.Bookmarks);
        Assert.Equal("010.jpg", r.LastPageOf(@"c:\만화\1권.ZIP"));
        Assert.True(double.IsNaN(r.WinLeft));
    }

    [Fact]
    public void 잘못된값은_고친다()
    {
        string dir = TestUtil.TempDir();
        File.WriteAllText(Path.Combine(dir, "settings.json"), "{\"PagesPerView\": 3, \"CacheMB\": 1}");
        var r = SettingsStore.Load(dir);
        Assert.Equal(1, r.PagesPerView);
        Assert.Equal(128, r.CacheMB);
    }

    [Fact]
    public void 깨진파일이면_기본값()
    {
        string dir = TestUtil.TempDir();
        File.WriteAllText(Path.Combine(dir, "settings.json"), "{ 깨짐");
        Assert.Equal(1, SettingsStore.Load(dir).PagesPerView);
    }

    [Fact]
    public void 최근위치는_200개까지()
    {
        var s = new Settings();
        for (int i = 0; i < 250; i++) s.RememberPosition("p" + i, "x");
        Assert.Equal(Settings.MaxRecent, s.Recent.Count);
        Assert.Equal("p249", s.Recent[0].SourcePath);
    }
}

public class SourceTests
{
    [Fact]
    public void ZIP_한글CP949이름과_자연정렬()
    {
        string dir = TestUtil.TempDir();
        string zip = Path.Combine(dir, "만화.zip");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using (var fs = File.Create(zip))
        using (var za = new ZipArchive(fs, ZipArchiveMode.Create, false, Encoding.GetEncoding(949)))
        {
            foreach (var (name, w) in new[] { ("한글/10.png", 10), ("한글/2.png", 20), ("한글/표지.png", 30), ("설명.txt", 0) })
            {
                var e = za.CreateEntry(name);
                using var s = e.Open();
                var bytes = w > 0 ? TestUtil.Png(w, 5) : Encoding.UTF8.GetBytes("text");
                s.Write(bytes);
            }
        }

        using var src = new ArchiveSource(zip, SortKey.Name, false);
        Assert.Equal(new[] { "한글/2.png", "한글/10.png", "한글/표지.png" }, src.Pages.Select(p => p.Name));
        var img = ImageLoader.Load(src.Pages[1], autoRotate: true);
        Assert.Null(img.Error);
        Assert.Equal(10, img.Width);
    }

    [Fact]
    public void 솔리드7Z_순서대로_풀어서_읽기()
    {
        string file = Path.Combine(AppContext.BaseDirectory, "fixtures", "solid.7z");
        using var src = new ArchiveSource(file, SortKey.Name, false);
        Assert.Equal(new[] { "권1/2.png", "권1/10.png", "권1/표지.png" }, src.Pages.Select(p => p.Name));
        // 압축 안 순서와 다르게(뒤에서부터) 요청해도 다 풀린 뒤 읽힌다.
        var widths = src.Pages.Reverse().Select(p => ImageLoader.Load(p, true).Width).ToArray();
        Assert.Equal(new[] { 32, 30, 31 }, widths);
    }

    [Fact]
    public void 이웃_폴더와_압축찾기()
    {
        string root = TestUtil.TempDir();
        Directory.CreateDirectory(Path.Combine(root, "1권"));
        File.WriteAllBytes(Path.Combine(root, "1권", "a.png"), TestUtil.Png(4, 4));
        Directory.CreateDirectory(Path.Combine(root, "2권-빈폴더"));
        File.WriteAllBytes(Path.Combine(root, "10권.zip"), Array.Empty<byte>());
        Directory.CreateDirectory(Path.Combine(root, "3권"));
        File.WriteAllBytes(Path.Combine(root, "3권", "b.jpg"), TestUtil.Png(4, 4));

        Assert.Equal(Path.Combine(root, "3권"), PageSources.FindSibling(Path.Combine(root, "1권"), +1));
        Assert.Equal(Path.Combine(root, "10권.zip"), PageSources.FindSibling(Path.Combine(root, "3권"), +1));
        Assert.Equal(Path.Combine(root, "1권"), PageSources.FindSibling(Path.Combine(root, "3권"), -1));
        Assert.Null(PageSources.FindSibling(Path.Combine(root, "10권.zip"), +1));
    }

    [Fact]
    public void 이미지파일을_열면_그폴더와_시작장()
    {
        string root = TestUtil.TempDir();
        File.WriteAllBytes(Path.Combine(root, "b.png"), TestUtil.Png(4, 4));
        File.WriteAllBytes(Path.Combine(root, "a.png"), TestUtil.Png(4, 4));
        File.WriteAllText(Path.Combine(root, "c.txt"), "x");
        using var src = PageSources.Open(Path.Combine(root, "b.png"), SortKey.Name, false, out var start);
        Assert.Equal("b.png", start);
        Assert.Equal(new[] { "a.png", "b.png" }, src.Pages.Select(p => p.Name));
    }
}

public class ImageTests
{
    [Fact]
    public void EXIF_방향6이면_세로로_돌린다()
    {
        string dir = TestUtil.TempDir();
        string file = Path.Combine(dir, "rot.jpg");
        var meta = new BitmapMetadata("jpg");
        meta.SetQuery("/app1/ifd/{ushort=274}", (ushort)6);
        var bmp = BitmapSource.Create(40, 20, 96, 96, PixelFormats.Bgr24, null, new byte[40 * 20 * 3], 40 * 3);
        var enc = new JpegBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp, null, meta, null));
        using (var fs = File.Create(file)) enc.Save(fs);

        var page = new PageEntry { Name = "rot.jpg", FilePath = file, Open = () => File.OpenRead(file) };
        var rotated = ImageLoader.Load(page, autoRotate: true);
        Assert.Equal((20, 40), (rotated.Width, rotated.Height));
        var raw = ImageLoader.Load(page, autoRotate: false);
        Assert.Equal((40, 20), (raw.Width, raw.Height));
        Assert.Contains(rotated.Info, kv => kv.Key == "EXIF 방향" && kv.Value == "6");
    }

    [Fact]
    public void 깨진파일은_오류문구()
    {
        var page = new PageEntry { Name = "x.jpg", Open = () => new MemoryStream(new byte[] { 1, 2, 3 }) };
        var r = ImageLoader.Load(page, true);
        Assert.Null(r.Bitmap);
        Assert.Contains("손상", r.Error);

        var webp = new PageEntry { Name = "x.webp", Open = () => new MemoryStream(new byte[] { 1, 2, 3 }) };
        Assert.Contains("코덱", ImageLoader.Load(webp, true).Error);
    }

    [Fact]
    public void 변환저장_크기조절과_회전()
    {
        var src = BitmapSource.Create(400, 200, 96, 96, PixelFormats.Bgr24, null, new byte[400 * 200 * 3], 400 * 3);
        src.Freeze();
        var o = new ExportOptions { Format = ExportFormat.Png, Resize = ExportResize.LongEdge, LongEdge = 100 };
        var outBmp = ImageExport.Prepare(src, o, rotation: 90, flipH: false, flipV: false);
        Assert.Equal((50, 100), (outBmp.PixelWidth, outBmp.PixelHeight));

        string file = Path.Combine(TestUtil.TempDir(), "o.png");
        ImageExport.Save(outBmp, file, o);
        var back = ImageLoader.Load(new PageEntry { Name = "o.png", Open = () => File.OpenRead(file) }, true);
        Assert.Equal((50, 100), (back.Width, back.Height));

        Assert.Equal(0.5, ImageExport.ScaleFor(400, 200, new ExportOptions { Resize = ExportResize.Percent, Percent = 50 }));
        Assert.Equal(0.25, ImageExport.ScaleFor(400, 200, new ExportOptions { Resize = ExportResize.Box, BoxWidth = 100, BoxHeight = 100 }));
    }

    [Fact]
    public void 샤픈결과_크기()
    {
        var src = BitmapSource.Create(100, 60, 96, 96, PixelFormats.Bgr24, null, new byte[100 * 60 * 3], 100 * 3);
        src.Freeze();
        var r = Sharpen.Apply(src, 0.5);
        Assert.Equal((50, 30), (r.PixelWidth, r.PixelHeight));
    }
}

static class TestUtil
{
    public static string TempDir()
    {
        string d = Path.Combine(Path.GetTempPath(), "NoAdViewTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    public static byte[] Png(int w, int h)
    {
        var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgr24, null, new byte[w * h * 3], w * 3);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }
}
