using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NoAdView.Core;

public enum ExportFormat { Jpeg, Png, Bmp, Tiff, Gif }
public enum ExportResize { None, Percent, LongEdge, Box }

public sealed class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Jpeg;
    public int Quality { get; set; } = 92;
    public ExportResize Resize { get; set; } = ExportResize.None;
    public double Percent { get; set; } = 50;
    public int LongEdge { get; set; } = 1920;
    public int BoxWidth { get; set; } = 1920;
    public int BoxHeight { get; set; } = 1080;
    public bool ApplyRotation { get; set; } = true;

    public string Extension => Format switch
    {
        ExportFormat.Png => ".png", ExportFormat.Bmp => ".bmp", ExportFormat.Tiff => ".tif",
        ExportFormat.Gif => ".gif", _ => ".jpg",
    };
}

/// <summary>다른 형식·크기로 저장(포맷 변환, 크기 조절). WebP 는 Windows 에 인코더가 없어 저장하지 않는다.</summary>
public static class ImageExport
{
    public static BitmapSource Prepare(BitmapSource src, ExportOptions o, int rotation, bool flipH, bool flipV)
    {
        BitmapSource bmp = src;
        if (o.ApplyRotation && (rotation % 360 != 0 || flipH || flipV))
        {
            var g = new TransformGroup();
            if (flipH || flipV) g.Children.Add(new ScaleTransform(flipH ? -1 : 1, flipV ? -1 : 1));
            if (rotation % 360 != 0) g.Children.Add(new RotateTransform(rotation));
            bmp = new TransformedBitmap(bmp, g);
        }
        double scale = ScaleFor(bmp.PixelWidth, bmp.PixelHeight, o);
        if (Math.Abs(scale - 1) > 1e-6)
            bmp = new TransformedBitmap(bmp, new ScaleTransform(scale, scale));
        return bmp;
    }

    public static double ScaleFor(int w, int h, ExportOptions o) => o.Resize switch
    {
        ExportResize.Percent => Math.Clamp(o.Percent, 1, 1000) / 100.0,
        ExportResize.LongEdge => o.LongEdge > 0 ? (double)o.LongEdge / Math.Max(w, h) : 1,
        ExportResize.Box => o.BoxWidth > 0 && o.BoxHeight > 0 ? Math.Min((double)o.BoxWidth / w, (double)o.BoxHeight / h) : 1,
        _ => 1,
    };

    public static void Save(BitmapSource bmp, string path, ExportOptions o)
    {
        BitmapEncoder enc = o.Format switch
        {
            ExportFormat.Png => new PngBitmapEncoder(),
            ExportFormat.Bmp => new BmpBitmapEncoder(),
            ExportFormat.Tiff => new TiffBitmapEncoder { Compression = TiffCompressOption.Lzw },
            ExportFormat.Gif => new GifBitmapEncoder(),
            _ => new JpegBitmapEncoder { QualityLevel = Math.Clamp(o.Quality, 1, 100) },
        };
        // JPEG·BMP 는 알파를 못 담으므로 흰 배경 없이 그대로 24비트로 바꾼다.
        BitmapSource frame = bmp;
        if (o.Format is ExportFormat.Jpeg or ExportFormat.Bmp && bmp.Format != PixelFormats.Bgr24)
            frame = new FormatConvertedBitmap(bmp, PixelFormats.Bgr24, null, 0);
        enc.Frames.Add(BitmapFrame.Create(frame));
        string tmp = path + ".tmp";
        using (var fs = File.Create(tmp)) enc.Save(fs);
        File.Move(tmp, path, true);
    }
}
