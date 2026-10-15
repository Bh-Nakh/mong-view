using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NoAdView.Core;

/// <summary>디코딩된 한 장. 백그라운드에서 만들고 Freeze 해서 UI 스레드로 넘긴다.</summary>
public sealed class LoadedImage
{
    public BitmapSource Bitmap { get; init; }
    public string Error { get; init; }
    public int Width => Bitmap?.PixelWidth ?? 800;
    public int Height => Bitmap?.PixelHeight ?? 1100;
    public long Bytes => Bitmap == null ? 0 : (long)Bitmap.PixelWidth * Bitmap.PixelHeight * 4;
    /// <summary>EXIF 등 사진 정보(표시용 이름 → 값).</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Info { get; init; } = Array.Empty<KeyValuePair<string, string>>();
    public string Format { get; init; }
}

public static class ImageLoader
{
    public static LoadedImage Load(PageEntry page, bool autoRotate)
    {
        try
        {
            var ms = new MemoryStream();
            using (var s = page.Open()) s.CopyTo(ms);
            ms.Position = 0;

            var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapSource frame = decoder.Frames[0];
            var meta = SafeMeta(frame);
            int orientation = autoRotate ? ReadOrientation(meta) : 1;

            BitmapSource bmp = frame;
            if (orientation > 1)
            {
                var t = OrientationTransform(orientation);
                bmp = new CachedBitmap(new TransformedBitmap(frame, t), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            if (bmp.CanFreeze) bmp.Freeze();

            return new LoadedImage
            {
                Bitmap = bmp,
                Format = decoder.CodecInfo?.FriendlyName,
                Info = ReadInfo(page, frame, meta, decoder),
            };
        }
        catch (Exception ex)
        {
            return new LoadedImage { Error = Friendly(ex, page.Name) };
        }
    }

    /// <summary>썸네일용 축소 디코딩(JPEG 는 디코더가 직접 줄여 빠르다).</summary>
    public static BitmapSource LoadThumb(PageEntry page, int height)
    {
        try
        {
            var ms = new MemoryStream();
            using (var s = page.Open()) s.CopyTo(ms);
            ms.Position = 0;
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bi.DecodePixelHeight = height;
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
        catch { return null; }
    }

    // 이 확장자들은 Windows 에 기본 내장돼 있어서, 못 열면 코덱이 아니라 파일 손상일 가능성이 크다.
    static readonly string[] builtInExts = { ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".gif", ".bmp", ".dib", ".tif", ".tiff", ".ico" };

    static string Friendly(Exception ex, string name) => ex switch
    {
        NotSupportedException or FileFormatException =>
            builtInExts.Contains(Path.GetExtension(name ?? "").ToLowerInvariant())
                ? "손상되었거나 올바른 이미지 파일이 아닙니다."
                : "이 형식을 열 수 있는 Windows 코덱이 설치돼 있지 않습니다.",
        FileNotFoundException => "파일을 찾을 수 없습니다.",
        UnauthorizedAccessException => "파일에 접근할 수 없습니다.",
        _ => ex.Message,
    };

    static BitmapMetadata SafeMeta(BitmapSource frame)
    {
        try { return frame.Metadata as BitmapMetadata; } catch { return null; }
    }

    static object Q(BitmapMetadata m, string query)
    {
        if (m == null) return null;
        try { return m.GetQuery(query); } catch { return null; }
    }

    static int ReadOrientation(BitmapMetadata m)
    {
        var o = Q(m, "System.Photo.Orientation");
        return o is ushort u && u >= 1 && u <= 8 ? u : 1;
    }

    /// <summary>EXIF 방향값(1~8)을 바로 세우는 변환.</summary>
    public static Transform OrientationTransform(int o)
    {
        var g = new TransformGroup();
        switch (o)
        {
            case 2: g.Children.Add(new ScaleTransform(-1, 1)); break;
            case 3: g.Children.Add(new RotateTransform(180)); break;
            case 4: g.Children.Add(new ScaleTransform(1, -1)); break;
            case 5: g.Children.Add(new ScaleTransform(-1, 1)); g.Children.Add(new RotateTransform(270)); break;
            case 6: g.Children.Add(new RotateTransform(90)); break;
            case 7: g.Children.Add(new ScaleTransform(-1, 1)); g.Children.Add(new RotateTransform(90)); break;
            case 8: g.Children.Add(new RotateTransform(270)); break;
        }
        return g;
    }

    static IReadOnlyList<KeyValuePair<string, string>> ReadInfo(PageEntry page, BitmapSource frame, BitmapMetadata m, BitmapDecoder decoder)
    {
        var list = new List<KeyValuePair<string, string>>();
        void Add(string k, string v) { if (!string.IsNullOrWhiteSpace(v)) list.Add(new(k, v.Trim())); }

        Add("파일", page.Name);
        Add("크기", $"{frame.PixelWidth} × {frame.PixelHeight}");
        Add("용량", FormatBytes(page.Size));
        if (page.Modified > DateTime.MinValue) Add("수정일", page.Modified.ToString("yyyy-MM-dd HH:mm:ss"));
        Add("형식", decoder.CodecInfo?.FriendlyName);
        if (decoder.Frames.Count > 1) Add("프레임", decoder.Frames.Count + "개 (첫 프레임만 표시)");
        if (m == null) return list;

        string Str(string q) => Q(m, q)?.ToString();
        try { Add("카메라", string.Join(" ", new[] { m.CameraManufacturer, m.CameraModel }.Where(x => !string.IsNullOrWhiteSpace(x)))); } catch { }
        try { Add("촬영일", m.DateTaken); } catch { }
        Add("렌즈", Str("System.Photo.LensModel"));
        if (Q(m, "System.Photo.ExposureTime") is double et && et > 0)
            Add("노출", et >= 1 ? $"{et:0.#}초" : $"1/{Math.Round(1 / et)}초");
        if (Q(m, "System.Photo.FNumber") is double fn && fn > 0) Add("조리개", $"f/{fn:0.#}");
        if (Q(m, "System.Photo.ISOSpeed") is ushort iso && iso > 0) Add("ISO", iso.ToString());
        if (Q(m, "System.Photo.FocalLength") is double fl && fl > 0) Add("초점거리", $"{fl:0.#}mm");
        var o = Q(m, "System.Photo.Orientation");
        if (o is ushort ou && ou > 1) Add("EXIF 방향", ou.ToString());

        double? lat = Gps(Q(m, "System.GPS.Latitude"), Str("System.GPS.LatitudeRef"));
        double? lon = Gps(Q(m, "System.GPS.Longitude"), Str("System.GPS.LongitudeRef"));
        if (lat != null && lon != null) Add("GPS", $"{lat:0.000000}, {lon:0.000000}");
        try { Add("프로그램", m.ApplicationName); } catch { }
        return list;
    }

    static double? Gps(object v, string reference)
    {
        if (v is not double[] d || d.Length < 3) return null;
        double deg = d[0] + d[1] / 60 + d[2] / 3600;
        if (reference is "S" or "W") deg = -deg;
        return deg;
    }

    public static string FormatBytes(long b) =>
        b >= 1 << 20 ? $"{b / 1048576.0:0.##} MB" : b >= 1024 ? $"{b / 1024.0:0.#} KB" : $"{b} B";
}
