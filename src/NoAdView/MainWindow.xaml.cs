using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NoAdView.Core;
using NoAdView.Views;

namespace NoAdView;

/// <summary>메인 창: 원본 열기, 장 표시, 확대·이동·회전 계산.</summary>
public partial class MainWindow : Window
{
    readonly App.StartArgs startArgs;
    readonly string dataDir;
    readonly Settings S;
    readonly PageCache cache = new();

    IPageSource source;
    List<int[]> views = new();
    int viewIndex;
    int showVersion;

    /// <summary>화면에 놓인 한 장.</summary>
    sealed class Shown
    {
        public int Page;
        public LoadedImage Img;
        public FrameworkElement El;
        public Image Image => El as Image;
        public FormatConvertedBitmap Pixels; // 픽셀 정보용(필요할 때 만든다)
    }

    readonly List<Shown> shown = new();
    double compW, compH; // 화면에 놓인 장들을 합친 크기(이미지 픽셀 단위)

    // 보기 상태
    double zoom = 1;
    bool manualZoom, lockZoom;
    double panX, panY; // 화면 중앙에서 내용 중앙까지 거리(장치 픽셀)
    int rotation;       // 0, 90, 180, 270
    bool flipH, flipV;

    bool fullscreen;
    WindowState prevState;
    WindowStyle prevStyle;
    System.Windows.ResizeMode prevResize;

    readonly DispatcherTimer toastTimer = new() { Interval = TimeSpan.FromSeconds(1.8) };

    public MainWindow(App.StartArgs args)
    {
        startArgs = args;
        dataDir = args.DataDir;
        S = SettingsStore.Load(dataDir);
        cache.LimitBytes = (long)S.CacheMB << 20;
        cache.AutoRotate = S.AutoRotateExif;

        InitializeComponent();
        StatusVersion.Text = "v" + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "");
        ApplyPlacement();
        Topmost = S.TopMost;
        Thumbs.ItemsSource = thumbItems;

        toastTimer.Tick += (_, _) => { toastTimer.Stop(); FadeToast(0); };
        InitInput();
        InitFeatures();

        Viewport.SizeChanged += (_, _) => ApplyFit(resetPan: false);
        Loaded += (_, _) =>
        {
            UpdateChrome();
            // 앱을 실행할 때 새 버전이 있는지 확인 (있을 때만 안내, 사용자가 눌러야 받는다). 시험용 --data 실행에서는 하지 않는다.
            if (args.DataDir == SettingsStore.DefaultDir) MongUpdate.UpdateUi.CheckAtStartup(this, "mongview", "몽뷰", AppVersionNumber, null);
            if (!string.IsNullOrEmpty(args.Path)) OpenPath(args.Path);
            else UpdateStatus();
        };
        Closing += (_, _) => SaveOnExit();
        Closed += (_, _) =>
        {
            var old = source;
            source = null;
            old?.Dispose();
        };
    }

    /// <summary>업데이트 비교에 쓰는 앱 버전 (NoAdView.csproj 의 Version, 예: 0.1.3)</summary>
    static string AppVersionNumber => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0";

    double Dpi => VisualTreeHelper.GetDpi(this).PixelsPerDip;
    int PageCount => source?.Pages.Count ?? 0;
    int[] CurrentView => views.Count > 0 ? views[Math.Clamp(viewIndex, 0, views.Count - 1)] : Array.Empty<int>();
    int CurrentPage => CurrentView.Length > 0 ? CurrentView[0] : -1;
    PageEntry CurrentEntry => CurrentPage >= 0 ? source.Pages[CurrentPage] : null;

    // ───────────── 창 위치 ─────────────

    void ApplyPlacement()
    {
        if (startArgs.Pos is Point p)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = p.X;
            Top = p.Y;
            if (startArgs.Size is Size z) { Width = z.Width; Height = z.Height; }
            ShowActivated = !startArgs.NoActivate;
            return;
        }
        ShowActivated = !startArgs.NoActivate;
        Width = Math.Max(MinWidth, S.WinWidth);
        Height = Math.Max(MinHeight, S.WinHeight);
        var vs = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        if (!double.IsNaN(S.WinLeft) && !double.IsNaN(S.WinTop) &&
            vs.IntersectsWith(new Rect(S.WinLeft, S.WinTop, Width, Height)) &&
            vs.Contains(new Point(S.WinLeft + 40, S.WinTop + 20)))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = S.WinLeft;
            Top = S.WinTop;
        }
        else WindowStartupLocation = WindowStartupLocation.CenterScreen;
        if (S.WinMaximized) WindowState = WindowState.Maximized;
    }

    void SaveOnExit()
    {
        RememberPosition();
        if (startArgs.Pos == null)
        {
            if (fullscreen) ToggleFullscreen();
            var r = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
            if (!r.IsEmpty)
            {
                S.WinLeft = r.Left; S.WinTop = r.Top; S.WinWidth = r.Width; S.WinHeight = r.Height;
            }
            S.WinMaximized = WindowState == WindowState.Maximized;
        }
        SaveSettings();
    }

    void SaveSettings()
    {
        try { SettingsStore.Save(dataDir, S); }
        catch (Exception ex) { Toast("설정을 저장하지 못했습니다: " + ex.Message); }
    }

    void RememberPosition()
    {
        if (source != null && CurrentEntry != null) S.RememberPosition(source.Path, CurrentEntry.Name);
    }

    // ───────────── 열기 ─────────────

    /// <summary>파일·폴더·압축을 연다. startName 이 있으면 그 장부터, atEnd 면 마지막 장부터.</summary>
    bool OpenPath(string path, string startName = null, bool atEnd = false, bool resume = true)
    {
        IPageSource src;
        string fileStart;
        try
        {
            path = System.IO.Path.GetFullPath(path);
            src = PageSources.Open(path, S.Sort, S.SortDescending, out fileStart);
        }
        catch (Exception ex)
        {
            Toast("열 수 없습니다: " + ex.Message);
            return false;
        }
        if (src.Pages.Count == 0)
        {
            src.Dispose();
            Toast("이미지가 없습니다: " + System.IO.Path.GetFileName(path.TrimEnd('\\')));
            return false;
        }

        RememberPosition();
        var old = source;
        source = src;
        if (old != null) Task.Run(old.Dispose); // 솔리드 압축 정리는 시간이 걸릴 수 있어 뒤에서

        cache.Reset(src);
        endPromptTime = DateTime.MinValue;
        rotation = 0; flipH = flipV = false;
        RebuildViews();

        int page = 0;
        startName ??= fileStart;
        if (startName != null) page = Math.Max(0, IndexOfName(startName));
        else if (atEnd) page = PageCount - 1;
        else if (resume && S.ResumeLastPage)
        {
            var last = S.LastPageOf(src.Path);
            int idx = last == null ? -1 : IndexOfName(last);
            if (idx > 0)
            {
                page = idx;
                Toast($"이어 보기: {idx + 1}쪽 / {PageCount}");
            }
        }
        viewIndex = SpreadLayout.ViewOf(views, page);
        RebuildThumbs();
        ShowView(scrollToEnd: atEnd);
        return true;
    }

    int IndexOfName(string name)
    {
        var pages = source.Pages;
        for (int i = 0; i < pages.Count; i++)
            if (string.Equals(pages[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    void RebuildViews() => views = SpreadLayout.Build(PageCount, S.PagesPerView, S.CoverSingle);

    /// <summary>여러 장 설정이 바뀌었을 때 지금 보던 장을 유지하며 다시 배치.</summary>
    void Relayout()
    {
        int page = CurrentPage;
        RebuildViews();
        viewIndex = page < 0 ? 0 : SpreadLayout.ViewOf(views, page);
        ShowView();
    }

    /// <summary>폴더 내용이 바뀌었을 때(삭제·이동·이름 변경·F5) 다시 읽는다.</summary>
    void ReloadSource(string keepName = null, int fallbackIndex = -1)
    {
        if (source == null) return;
        string path = source.Path;
        keepName ??= CurrentEntry?.Name;
        IPageSource src;
        try
        {
            src = source.IsArchive ? new ArchiveSource(path, S.Sort, S.SortDescending)
                                   : new FolderSource(path, S.Sort, S.SortDescending);
        }
        catch (Exception ex)
        {
            Toast("다시 읽지 못했습니다: " + ex.Message);
            return;
        }
        var old = source;
        source = src;
        Task.Run(old.Dispose);
        cache.Reset(src);
        RebuildViews();
        if (PageCount == 0)
        {
            ClearStage();
            RebuildThumbs();
            UpdateStatus();
            Toast("이 폴더에 이미지가 더 없습니다.");
            return;
        }
        int idx = keepName == null ? -1 : IndexOfName(keepName);
        if (idx < 0) idx = Math.Clamp(fallbackIndex, 0, PageCount - 1);
        viewIndex = SpreadLayout.ViewOf(views, idx);
        RebuildThumbs();
        ShowView();
    }

    // ───────────── 표시 ─────────────

    async void ShowView(bool resetZoom = true, bool scrollToEnd = false)
    {
        if (source == null || views.Count == 0) { ClearStage(); UpdateStatus(); return; }
        viewIndex = Math.Clamp(viewIndex, 0, views.Count - 1);
        int ver = ++showVersion;
        int[] pages = CurrentView;
        var keep = new HashSet<int>(pages);
        var tasks = pages.Select(p => cache.Get(p, keep)).ToArray();
        UpdateStatus(loading: !tasks.All(t => t.IsCompleted));
        SyncThumbSelection();

        LoadedImage[] imgs = await Task.WhenAll(tasks);
        if (ver != showVersion || source == null) return;

        BuildStage(pages, imgs);
        if (resetZoom && !lockZoom) manualZoom = false;
        ApplyFit(resetPan: true, toEnd: scrollToEnd);
        UpdateStatus();
        UpdateInfoPanel();
        ScheduleSharpen();
        Prefetch(keep);
    }

    void Prefetch(HashSet<int> keep)
    {
        var order = new List<int>();
        for (int d = 1; d <= 3; d++)
            if (viewIndex + d < views.Count) order.AddRange(views[viewIndex + d]);
        if (viewIndex - 1 >= 0) order.AddRange(views[viewIndex - 1]);
        foreach (var p in order) keep.Add(p);
        cache.Prefetch(order, keep);
    }

    void ClearStage()
    {
        Stage.Children.Clear();
        shown.Clear();
        compW = compH = 0;
        EmptyHint.Visibility = source == null ? Visibility.Visible : Visibility.Collapsed;
        UpdateInfoPanel();
    }

    /// <summary>
    /// 장들을 한 덩어리로 배치한다(단위: 이미지 픽셀).
    /// 두 장은 높이를 맞춰 나란히, 네 장은 2×2 칸에 각각 맞춰 넣는다.
    /// </summary>
    void BuildStage(int[] pages, LoadedImage[] imgs)
    {
        Stage.Children.Clear();
        shown.Clear();
        sharpened.Clear();
        EmptyHint.Visibility = Visibility.Collapsed;
        var byPage = pages.Zip(imgs).ToDictionary(x => x.First, x => x.Second);
        int[] order = SpreadLayout.Arrange(pages, S.PagesPerView, S.RightToLeft);

        var rects = new List<Rect>();
        if (order.Length == 1)
        {
            var im = byPage[order[0]];
            rects.Add(new Rect(0, 0, im.Width, im.Height));
            compW = im.Width; compH = im.Height;
        }
        else if (S.PagesPerView == 2)
        {
            double h = order.Max(p => (double)byPage[p].Height);
            double x = 0;
            foreach (var p in order)
            {
                var im = byPage[p];
                double w = im.Width * h / im.Height;
                rects.Add(new Rect(x, 0, w, h));
                x += w;
            }
            compW = x; compH = h;
        }
        else
        {
            double cw = order.Max(p => (double)byPage[p].Width);
            double ch = order.Max(p => (double)byPage[p].Height);
            double gap = Math.Max(cw, ch) * 0.01;
            int rows = (order.Length + 1) / 2;
            for (int k = 0; k < order.Length; k++)
            {
                var im = byPage[order[k]];
                double s = Math.Min(cw / im.Width, ch / im.Height);
                double w = im.Width * s, h = im.Height * s;
                double cx = (k % 2) * (cw + gap), cy = (k / 2) * (ch + gap);
                rects.Add(new Rect(cx + (cw - w) / 2, cy + (ch - h) / 2, w, h));
            }
            compW = cw * 2 + gap;
            compH = ch * rows + gap * (rows - 1);
        }

        for (int k = 0; k < order.Length; k++)
        {
            var im = byPage[order[k]];
            var r = rects[k];
            FrameworkElement el;
            if (im.Bitmap != null)
            {
                var img = new Image { Source = im.Bitmap, Stretch = Stretch.Fill };
                RenderOptions.SetBitmapScalingMode(img,
                    S.Interp == Interp.Pixel ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
                el = img;
            }
            else
            {
                el = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                    BorderThickness = new Thickness(4),
                    Child = new Viewbox
                    {
                        Margin = new Thickness(40),
                        Child = new TextBlock
                        {
                            Foreground = Brushes.Gainsboro,
                            TextAlignment = TextAlignment.Center,
                            Text = "열 수 없는 파일\n" + source.Pages[order[k]].Name + "\n" + im.Error,
                        },
                    },
                };
            }
            el.Width = r.Width;
            el.Height = r.Height;
            Canvas.SetLeft(el, r.X);
            Canvas.SetTop(el, r.Y);
            Stage.Children.Add(el);
            shown.Add(new Shown { Page = order[k], Img = im, El = el });
        }
        Stage.Width = compW;
        Stage.Height = compH;
    }

    (double w, double h) RotatedSize() => rotation % 180 == 0 ? (compW, compH) : (compH, compW);
    (double w, double h) ViewSize() => (Viewport.ActualWidth * Dpi, Viewport.ActualHeight * Dpi);

    double FitZoom()
    {
        var (rw, rh) = RotatedSize();
        var (vw, vh) = ViewSize();
        if (rw <= 0 || rh <= 0 || vw <= 0 || vh <= 0) return 1;
        double z = S.Fit switch
        {
            FitMode.Width => vw / rw,
            FitMode.Height => vh / rh,
            FitMode.Original => 1,
            _ => Math.Min(vw / rw, vh / rh),
        };
        if (!S.UpscaleSmall && S.Fit != FitMode.Original) z = Math.Min(z, 1);
        return z;
    }

    /// <summary>맞춤 배율을 다시 계산한다. resetPan 이면 읽기 시작 위치(위쪽, 우→좌면 오른쪽)로.</summary>
    void ApplyFit(bool resetPan, bool toEnd = false)
    {
        if (compW <= 0) return;
        if (!manualZoom) zoom = FitZoom();
        if (resetPan)
        {
            var (rw, rh) = RotatedSize();
            var (vw, vh) = ViewSize();
            double ex = Math.Max(0, (rw * zoom - vw) / 2), ey = Math.Max(0, (rh * zoom - vh) / 2);
            panY = toEnd ? -ey : ey;
            panX = S.RightToLeft ? -ex : ex;
            if (toEnd) panX = -panX;
        }
        ClampPan();
        UpdateTransform();
    }

    void ClampPan()
    {
        var (rw, rh) = RotatedSize();
        var (vw, vh) = ViewSize();
        double ex = Math.Max(0, (rw * zoom - vw) / 2), ey = Math.Max(0, (rh * zoom - vh) / 2);
        panX = Math.Clamp(panX, -ex, ex);
        panY = Math.Clamp(panY, -ey, ey);
    }

    void UpdateTransform()
    {
        double dpi = Dpi;
        var m = Matrix.Identity;
        m.Translate(-compW / 2, -compH / 2);
        m.Scale(flipH ? -1 : 1, flipV ? -1 : 1);
        m.Rotate(rotation);
        m.Scale(zoom / dpi, zoom / dpi);
        m.Translate(Viewport.ActualWidth / 2 + panX / dpi, Viewport.ActualHeight / 2 + panY / dpi);
        Stage.RenderTransform = new MatrixTransform(m);
        UpdateStatusRight();
    }

    /// <summary>화면 한 점(DIP)을 기준으로 배율을 바꾼다.</summary>
    void ZoomAt(double newZoom, Point? anchor = null)
    {
        if (compW <= 0) return;
        newZoom = Math.Clamp(newZoom, 0.02, 32);
        double dpi = Dpi;
        var a = anchor ?? new Point(Viewport.ActualWidth / 2, Viewport.ActualHeight / 2);
        double ax = (a.X - Viewport.ActualWidth / 2) * dpi, ay = (a.Y - Viewport.ActualHeight / 2) * dpi;
        panX = ax - (ax - panX) * newZoom / zoom;
        panY = ay - (ay - panY) * newZoom / zoom;
        zoom = newZoom;
        manualZoom = true;
        ClampPan();
        UpdateTransform();
        ScheduleSharpen();
    }

    /// <summary>내용을 위아래로 민다. 실제로 움직였으면 true.</summary>
    bool ScrollBy(double dyDevice)
    {
        double old = panY;
        panY -= dyDevice;
        ClampPan();
        if (Math.Abs(old - panY) < 0.5) { panY = old; return false; }
        UpdateTransform();
        return true;
    }

    // ───────────── 상태 표시 ─────────────

    void UpdateStatus(bool loading = false)
    {
        if (source == null)
        {
            Title = "몽뷰";
            StatusLeft.Text = "열린 파일 없음";
            StatusRight.Text = "";
            EmptyHint.Visibility = Visibility.Visible;
            return;
        }
        var view = CurrentView;
        string range = view.Length == 0 ? "0" :
            view.Length == 1 ? $"{view[0] + 1}" : $"{view.Min() + 1}-{view.Max() + 1}";
        string name = CurrentEntry?.Name ?? "";
        string srcName = System.IO.Path.GetFileName(source.Path.TrimEnd('\\'));
        Title = $"{System.IO.Path.GetFileName(name)} [{range}/{PageCount}] - {srcName} - 몽뷰";
        StatusLeft.Text = $"[{range}/{PageCount}]  {name}" + (loading ? "  (불러오는 중…)" : "");
        UpdateStatusRight();
    }

    string pixelText;

    void UpdateStatusRight()
    {
        if (source == null) return;
        var parts = new List<string>();
        if (pixelText != null) parts.Add(pixelText);
        if (shown.Count == 1 && shown[0].Img.Bitmap != null) parts.Add($"{shown[0].Img.Width}×{shown[0].Img.Height}");
        parts.Add($"{zoom * 100:0}%" + (lockZoom ? " 고정" : ""));
        parts.Add(S.Fit switch { FitMode.Width => "너비", FitMode.Height => "높이", FitMode.Original => "원본", _ => "창" } + " 맞춤");
        if (S.PagesPerView > 1) parts.Add($"{S.PagesPerView}장 " + (S.RightToLeft ? "오→왼" : "왼→오"));
        if (rotation != 0) parts.Add($"{rotation}°");
        if (slideTimer.IsEnabled) parts.Add($"슬라이드 {slideTimer.Interval.TotalSeconds:0}초");
        StatusRight.Text = string.Join("  ·  ", parts);
    }

    void UpdateChrome()
    {
        StatusBar.Visibility = S.ShowStatusBar && !fullscreen ? Visibility.Visible : Visibility.Collapsed;
        Thumbs.Visibility = S.ShowThumbs ? Visibility.Visible : Visibility.Collapsed;
    }

    void Toast(string msg, double seconds = 1.8)
    {
        ToastText.Text = msg;
        toastTimer.Stop();
        toastTimer.Interval = TimeSpan.FromSeconds(seconds);
        FadeToast(1);
        toastTimer.Start();
    }

    void FadeToast(double to) =>
        ToastBox.BeginAnimation(OpacityProperty, new DoubleAnimation(to, TimeSpan.FromMilliseconds(to > 0 ? 120 : 400)));

    // ───────────── 전체 화면 ─────────────

    void ToggleFullscreen()
    {
        if (!fullscreen)
        {
            prevState = WindowState;
            prevStyle = WindowStyle;
            prevResize = ResizeMode;
            fullscreen = true;
            WindowStyle = WindowStyle.None;
            ResizeMode = System.Windows.ResizeMode.NoResize;
            if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
            WindowState = WindowState.Maximized;
        }
        else
        {
            fullscreen = false;
            WindowStyle = prevStyle;
            ResizeMode = prevResize;
            WindowState = WindowState.Normal;
            if (prevState == WindowState.Maximized) WindowState = WindowState.Maximized;
        }
        UpdateChrome();
    }
}
