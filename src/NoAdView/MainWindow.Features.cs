using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using NoAdView.Core;
using NoAdView.Views;

namespace NoAdView;

/// <summary>슬라이드쇼·책갈피·썸네일·정보창·픽셀 정보·샤픈·파일 작업·설정.</summary>
public partial class MainWindow
{
    readonly DispatcherTimer slideTimer = new();
    readonly DispatcherTimer sharpenTimer = new() { Interval = TimeSpan.FromMilliseconds(160) };
    readonly ObservableCollection<ThumbItem> thumbItems = new();
    readonly SemaphoreSlim thumbWorkers = new(2);
    int thumbGen;
    bool syncingThumbs;
    bool pixelInfo;

    void InitFeatures()
    {
        slideTimer.Tick += (_, _) => Next(auto: true);
        sharpenTimer.Tick += (_, _) => { sharpenTimer.Stop(); DoSharpen(); };
        // 항목이 포커스를 못 받게 해 두었으므로(키 입력을 뺏기지 않으려고) 선택 대신 클릭을 직접 처리한다.
        Thumbs.PreviewMouseLeftButtonDown += (_, e) =>
        {
            var d = e.OriginalSource as DependencyObject;
            while (d != null && d is not ListBoxItem)
                d = d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
            if (d is ListBoxItem { DataContext: ThumbItem t })
            {
                GoToPage(t.Index);
                e.Handled = true;
            }
        };
    }

    // ───────────── 슬라이드쇼 ─────────────

    void StartSlideshow(int seconds)
    {
        if (source == null) return;
        slideTimer.Interval = TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 60));
        slideTimer.Stop();
        slideTimer.Start();
        UpdateStatusRight();
        Toast($"슬라이드쇼 {seconds}초 간격 (Esc 또는 Ctrl+0 으로 중지)");
    }

    void StopSlideshow()
    {
        if (!slideTimer.IsEnabled) return;
        slideTimer.Stop();
        UpdateStatusRight();
        Toast("슬라이드쇼 중지");
    }

    // ───────────── 책갈피 ─────────────

    void AddBookmark()
    {
        if (source == null || CurrentEntry == null) return;
        string page = CurrentEntry.Name;
        if (S.Bookmarks.Any(b => string.Equals(b.SourcePath, source.Path, StringComparison.OrdinalIgnoreCase) && b.Page == page))
        {
            Toast("이미 책갈피에 있습니다");
            return;
        }
        S.Bookmarks.Insert(0, new Bookmark { SourcePath = source.Path, Page = page, Added = DateTime.Now });
        SaveSettings();
        Toast($"책갈피 추가: {CurrentPage + 1}쪽");
    }

    void ShowBookmarks()
    {
        var bm = Dialogs.ShowBookmarks(this, S.Bookmarks);
        SaveSettings();
        if (bm == null) return;
        if (source != null && string.Equals(source.Path, bm.SourcePath, StringComparison.OrdinalIgnoreCase))
        {
            int idx = IndexOfName(bm.Page);
            if (idx >= 0) { GoToPage(idx); return; }
        }
        if (!Directory.Exists(bm.SourcePath) && !File.Exists(bm.SourcePath))
        {
            Toast("책갈피의 폴더·파일을 찾을 수 없습니다");
            return;
        }
        OpenPath(bm.SourcePath, startName: bm.Page);
    }

    // ───────────── 썸네일 막대 ─────────────

    void RebuildThumbs()
    {
        thumbGen++;
        syncingThumbs = true;
        thumbItems.Clear();
        if (source != null)
        {
            var pages = source.Pages;
            for (int i = 0; i < pages.Count; i++)
                thumbItems.Add(new ThumbItem(i, (i + 1).ToString(), RequestThumb));
        }
        syncingThumbs = false;
    }

    void RequestThumb(ThumbItem item)
    {
        var src = source;
        int gen = thumbGen;
        int height = (int)Math.Ceiling(92 * Dpi);
        if (src == null) return;
        Task.Run(async () =>
        {
            await thumbWorkers.WaitAsync();
            try
            {
                if (gen != thumbGen || item.Index >= src.Pages.Count) return;
                var bmp = ImageLoader.LoadThumb(src.Pages[item.Index], height);
                _ = Dispatcher.BeginInvoke(() => { if (gen == thumbGen) item.Thumb = bmp; });
            }
            catch { }
            finally { thumbWorkers.Release(); }
        });
    }

    void SyncThumbSelection()
    {
        if (!S.ShowThumbs || thumbItems.Count == 0 || CurrentPage < 0) return;
        syncingThumbs = true;
        Thumbs.SelectedIndex = CurrentPage;
        Thumbs.ScrollIntoView(thumbItems[CurrentPage]);
        syncingThumbs = false;
    }

    // ───────────── 사진 정보 ─────────────

    void UpdateInfoPanel()
    {
        if (!InfoPanel.IsVisible) return;
        if (shown.Count == 0) { InfoText.Text = "표시 중인 이미지가 없습니다."; return; }
        var sb = new System.Text.StringBuilder();
        foreach (var page in CurrentView)
        {
            var sh = shown.FirstOrDefault(x => x.Page == page);
            if (sh == null) continue;
            if (sb.Length > 0) sb.AppendLine().AppendLine("────────");
            if (sh.Img.Error != null)
            {
                sb.AppendLine("파일: " + source.Pages[page].Name).Append("오류: " + sh.Img.Error);
                continue;
            }
            sb.Append(string.Join("\n", sh.Img.Info.Select(kv => $"{kv.Key}: {kv.Value}")));
        }
        InfoText.Text = sb.ToString();
    }

    // ───────────── 픽셀 정보 ─────────────

    void UpdatePixelInfo(Point viewportPoint)
    {
        string text = null;
        foreach (var sh in shown)
        {
            var bmp = sh.Img.Bitmap;
            if (bmp == null) continue;
            var pt = Viewport.TranslatePoint(viewportPoint, sh.El);
            if (pt.X < 0 || pt.Y < 0 || pt.X >= sh.El.Width || pt.Y >= sh.El.Height) continue;
            int x = Math.Clamp((int)(pt.X / sh.El.Width * bmp.PixelWidth), 0, bmp.PixelWidth - 1);
            int y = Math.Clamp((int)(pt.Y / sh.El.Height * bmp.PixelHeight), 0, bmp.PixelHeight - 1);
            try
            {
                sh.Pixels ??= new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
                var buf = new byte[4];
                sh.Pixels.CopyPixels(new Int32Rect(x, y, 1, 1), buf, 4, 0);
                text = $"({x}, {y})  RGB({buf[2]}, {buf[1]}, {buf[0]})  #{buf[2]:X2}{buf[1]:X2}{buf[0]:X2}" +
                       (buf[3] < 255 ? $"  A{buf[3]}" : "");
            }
            catch { }
            break;
        }
        if (text == pixelText) return;
        pixelText = text;
        UpdateStatusRight();
    }

    // ───────────── 샤픈 ─────────────

    readonly Dictionary<Shown, double> sharpened = new();

    void ScheduleSharpen()
    {
        sharpenTimer.Stop();
        if (S.Interp != Interp.Sharpen)
        {
            foreach (var sh in shown)
                if (sh.Image != null && sh.Img.Bitmap != null && sh.Image.Source != sh.Img.Bitmap)
                    sh.Image.Source = sh.Img.Bitmap;
            sharpened.Clear();
            return;
        }
        sharpenTimer.Start();
    }

    void DoSharpen()
    {
        if (S.Interp != Interp.Sharpen) return;
        int ver = showVersion;
        double z = zoom;
        foreach (var sh in shown.ToList())
        {
            var bmp = sh.Img.Bitmap;
            if (sh.Image == null || bmp == null) continue;
            double scale = Math.Round(Math.Min(1, z * sh.El.Width / bmp.PixelWidth), 3);
            if (scale <= 0) continue;
            if (sharpened.TryGetValue(sh, out var done) && Math.Abs(done - scale) < 0.0005) continue;
            sharpened[sh] = scale;
            Task.Run(() => Sharpen.Apply(bmp, scale)).ContinueWith(t =>
            {
                if (t.IsFaulted || ver != showVersion || S.Interp != Interp.Sharpen) return;
                if (Math.Abs(zoom - z) > 1e-9) return;
                sh.Image.Source = t.Result;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    // ───────────── 파일 작업 ─────────────

    IntPtr Hwnd => new WindowInteropHelper(this).Handle;

    string CurrentFileOrWarn()
    {
        var e = CurrentEntry;
        if (e == null) return null;
        if (!e.IsFile) { Toast("압축 파일 안의 이미지에는 할 수 없습니다"); return null; }
        return e.FilePath;
    }

    void DeleteCurrent()
    {
        string path = CurrentFileOrWarn();
        if (path == null) return;
        if (S.ConfirmDelete &&
            MessageBox.Show(this, $"휴지통으로 보낼까요?\n\n{System.IO.Path.GetFileName(path)}", "삭제",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        int idx = CurrentPage;
        string next = idx + 1 < PageCount ? source.Pages[idx + 1].Name : null;
        try
        {
            FileOps.Recycle(path, Hwnd);
            Toast("휴지통으로 보냈습니다: " + System.IO.Path.GetFileName(path));
            ReloadSource(next, idx);
        }
        catch (Exception ex) { Toast(ex.Message, 3); }
    }

    void RenameCurrent()
    {
        string path = CurrentFileOrWarn();
        if (path == null) return;
        string name = Dialogs.Prompt(this, "이름 바꾸기", "새 파일 이름:", System.IO.Path.GetFileName(path), selectStem: true);
        if (name == null) return;
        try
        {
            string target = FileOps.Rename(path, name);
            ReloadSource(System.IO.Path.GetFileName(target));
            Toast("이름을 바꿨습니다");
        }
        catch (Exception ex) { Toast(ex.Message, 3); }
    }

    void CopyOrMove(bool move)
    {
        var entry = CurrentEntry;
        if (entry == null) return;
        if (move && CurrentFileOrWarn() == null) return;
        string last = move ? S.LastMoveDir : S.LastCopyDir;
        var dlg = new OpenFolderDialog
        {
            Title = move ? "이동할 폴더 선택" : (entry.IsFile ? "복사할 폴더 선택" : "꺼낼 폴더 선택"),
            InitialDirectory = Directory.Exists(last) ? last : SourceDir(),
        };
        if (dlg.ShowDialog(this) != true) return;
        string dir = dlg.FolderName;
        string dest = System.IO.Path.Combine(dir, System.IO.Path.GetFileName(entry.Name.Replace('/', '\\')));
        if (entry.IsFile && string.Equals(dest, entry.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            Toast("같은 폴더입니다");
            return;
        }
        if (File.Exists(dest) &&
            MessageBox.Show(this, $"같은 이름의 파일이 있습니다. 덮어쓸까요?\n\n{dest}", move ? "이동" : "복사",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        try
        {
            if (move)
            {
                int idx = CurrentPage;
                string next = idx + 1 < PageCount ? source.Pages[idx + 1].Name : null;
                File.Move(entry.FilePath, dest, true);
                S.LastMoveDir = dir;
                Toast("이동했습니다: " + dir);
                ReloadSource(next, idx);
            }
            else
            {
                if (entry.IsFile) File.Copy(entry.FilePath, dest, true);
                else
                {
                    using var s = entry.Open();
                    using var f = File.Create(dest);
                    s.CopyTo(f);
                }
                S.LastCopyDir = dir;
                Toast((entry.IsFile ? "복사했습니다: " : "꺼냈습니다: ") + dir);
            }
        }
        catch (Exception ex) { Toast(ex.Message, 3); }
    }

    BitmapSource CurrentBitmap()
    {
        var sh = shown.FirstOrDefault(x => x.Page == CurrentPage);
        if (sh?.Img.Bitmap == null) { Toast("표시 중인 이미지가 없습니다"); return null; }
        return sh.Img.Bitmap;
    }

    void CopyToClipboard()
    {
        var bmp = CurrentBitmap();
        if (bmp == null) return;
        try
        {
            var data = new DataObject();
            data.SetImage(bmp);
            if (CurrentEntry?.FilePath is string f)
                data.SetFileDropList(new System.Collections.Specialized.StringCollection { f });
            Clipboard.SetDataObject(data, true);
            Toast("이미지를 클립보드에 복사했습니다");
        }
        catch (COMException) { Toast("클립보드를 다른 프로그램이 쓰고 있습니다. 잠시 후 다시 해 보세요."); }
    }

    void SaveAsCmd()
    {
        var bmp = CurrentBitmap();
        if (bmp == null) return;
        var opt = Dialogs.ShowExport(this, bmp, S.JpegQuality, rotation, flipH, flipV);
        if (opt == null) return;
        if (opt.Format == ExportFormat.Jpeg) S.JpegQuality = opt.Quality;

        string stem = System.IO.Path.GetFileNameWithoutExtension(CurrentEntry.Name.Replace('/', '\\'));
        var dlg = new SaveFileDialog
        {
            Title = "다른 형식·크기로 저장",
            FileName = stem + opt.Extension,
            DefaultExt = opt.Extension,
            Filter = $"{opt.Format} 파일|*{opt.Extension}",
            InitialDirectory = SourceDir(),
            OverwritePrompt = true,
        };
        if (dlg.ShowDialog(this) != true) return;
        string path = dlg.FileName;
        int rot = rotation; bool fh = flipH, fv = flipV;
        Toast("저장하는 중…", 10);
        Task.Run(() =>
        {
            var outBmp = ImageExport.Prepare(bmp, opt, rot, fh, fv);
            ImageExport.Save(outBmp, path, opt);
        }).ContinueWith(t =>
        {
            if (t.IsFaulted) { Toast("저장하지 못했습니다: " + t.Exception?.InnerException?.Message, 4); return; }
            Toast("저장했습니다: " + System.IO.Path.GetFileName(path));
            if (source is { IsArchive: false } &&
                string.Equals(System.IO.Path.GetDirectoryName(path), source.Path, StringComparison.OrdinalIgnoreCase))
                ReloadSource();
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    void SetWallpaperCmd()
    {
        var bmp = CurrentBitmap();
        if (bmp == null) return;
        if (MessageBox.Show(this, "바탕화면을 이 이미지로 바꿀까요?", "바탕화면으로 지정",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        string file = System.IO.Path.Combine(dataDir, "wallpaper.png");
        int rot = rotation; bool fh = flipH, fv = flipV;
        Task.Run(() =>
        {
            Directory.CreateDirectory(dataDir);
            var opt = new ExportOptions { Format = ExportFormat.Png };
            ImageExport.Save(ImageExport.Prepare(bmp, opt, rot, fh, fv), file, opt);
            FileOps.SetWallpaper(file);
        }).ContinueWith(t =>
        {
            Toast(t.IsFaulted ? "바탕화면을 바꾸지 못했습니다: " + t.Exception?.InnerException?.Message : "바탕화면으로 지정했습니다", 3);
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    // ───────────── 설정 ─────────────

    void ShowSettings()
    {
        var before = (S.Sort, S.SortDescending, S.AutoRotateExif, S.CoverSingle, S.UpscaleSmall);
        if (!Dialogs.ShowSettings(this, S)) return;
        cache.LimitBytes = (long)S.CacheMB << 20;
        SaveSettings();
        if (before.Sort != S.Sort || before.SortDescending != S.SortDescending)
        {
            ReloadSource();
            return;
        }
        if (before.AutoRotateExif != S.AutoRotateExif)
        {
            cache.AutoRotate = S.AutoRotateExif;
            cache.Reset(source);
        }
        if (before.CoverSingle != S.CoverSingle || before.AutoRotateExif != S.AutoRotateExif) Relayout();
        else if (before.UpscaleSmall != S.UpscaleSmall) Refit();
    }
}
