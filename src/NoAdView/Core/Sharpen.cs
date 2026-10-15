using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NoAdView.Core;

/// <summary>화면에 보이는 크기로 줄인 뒤 3×3 샤픈을 적용한다(CPU).</summary>
public static class Sharpen
{
    public static BitmapSource Apply(BitmapSource src, double scale, double amount = 0.6)
    {
        BitmapSource s = src;
        if (scale < 0.999)
            s = new TransformedBitmap(src, new ScaleTransform(scale, scale));
        var conv = new FormatConvertedBitmap(s, PixelFormats.Bgra32, null, 0);
        int w = conv.PixelWidth, h = conv.PixelHeight, stride = w * 4;
        if (w < 3 || h < 3) return src;
        var px = new byte[stride * h];
        conv.CopyPixels(px, stride, 0);
        var outp = (byte[])px.Clone();

        Parallel.For(1, h - 1, y =>
        {
            int row = y * stride;
            for (int x = 1; x < w - 1; x++)
            {
                int i = row + x * 4;
                for (int c = 0; c < 3; c++)
                {
                    int p = px[i + c];
                    int nb = px[i + c - 4] + px[i + c + 4] + px[i + c - stride] + px[i + c + stride];
                    double v = p + amount * (4 * p - nb);
                    outp[i + c] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                }
            }
        });
        var result = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, outp, stride);
        result.Freeze();
        return result;
    }
}
