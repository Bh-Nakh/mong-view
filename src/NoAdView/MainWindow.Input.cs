using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NoAdView.Core;
using NoAdView.Views;

namespace NoAdView;

/// <summary>키보드·마우스·끌어다 놓기·우클릭 메뉴와 명령 실행.</summary>
public partial class MainWindow
{
    Point dragStart;
    bool dragging;
    double dragPanX, dragPanY;

    // 마지막 장에서 한 번 더 넘겨야 다음 폴더로 가게 하는 확인 상태
    DateTime endPromptTime = DateTime.MinValue;
    int endPromptDir;

    void InitInput()
    {
        PreviewKeyDown += OnKey;

        Viewport.MouseLeftButtonDown += OnLeftDown;
        Viewport.MouseMove += OnMouseMove;
        Viewport.MouseLeftButtonUp += (_, _) => EndDrag();
        Viewport.LostMouseCapture += (_, _) => EndDrag();
        Viewport.PreviewMouseWheel += OnWheel;
        Viewport.MouseDown += OnOtherButton;
        Viewport.MouseRightButtonUp += (_, e) => { ShowContextMenu(); e.Handled = true; };
        Viewport.MouseLeave += (_, _) => { if (pixelText != null) { pixelText = null; UpdateStatusRight(); } };

        Thumbs.PreviewMouseWheel += OnThumbWheel;

        DragOver += (_, e) =>
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        };
        Drop += (_, e) =>
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) OpenPath(files[0]);
        };
    }

    void OnKey(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var bind = KeyMap.Find(key, Keyboard.Modifiers);
        if (bind == null) return;
        e.Handled = true;
        Run(bind.Cmd, bind.Arg, e.IsRepeat);
    }

    void Run(Cmd cmd, int arg = 0, bool isRepeat = false)
    {
        var (_, vh) = ViewSize();
        switch (cmd)
        {
            case Cmd.Next: Next(isRepeat: isRepeat); break;
            case Cmd.Prev: Prev(isRepeat: isRepeat); break;
            case Cmd.RightKey: if (ArrowsReversed) Prev(isRepeat: isRepeat); else Next(isRepeat: isRepeat); break;
            case Cmd.LeftKey: if (ArrowsReversed) Next(isRepeat: isRepeat); else Prev(isRepeat: isRepeat); break;
            case Cmd.ScrollDownOrNext:
                if (!ScrollBy(vh * (Keyboard.IsKeyDown(Key.Space) ? 0.85 : 0.2))) Next(isRepeat: isRepeat);
                break;
            case Cmd.ScrollUpOrPrev:
                if (!ScrollBy(-vh * 0.2)) Prev(toEnd: true, isRepeat: isRepeat);
                break;
            case Cmd.First: GoToView(0); break;
            case Cmd.Last: GoToView(views.Count - 1, toEnd: true); break;
            case Cmd.NextSource: JumpSource(+1); break;
            case Cmd.PrevSource: JumpSource(-1); break;
            case Cmd.ToggleLoop: S.Loop = !S.Loop; Toast(S.Loop ? "끝에서 처음으로 반복: 켬" : "끝에서 처음으로 반복: 끔"); break;

            case Cmd.FitWindow: SetFit(FitMode.Window, "창에 맞춤"); break;
            case Cmd.FitWidth: SetFit(FitMode.Width, "너비에 맞춤"); break;
            case Cmd.FitHeight: SetFit(FitMode.Height, "높이에 맞춤"); break;
            case Cmd.FitOriginal: SetFit(FitMode.Original, "원본 크기"); break;
            case Cmd.ToggleUpscale:
                S.UpscaleSmall = !S.UpscaleSmall;
                manualZoom = false;
                ApplyFit(resetPan: true);
                ScheduleSharpen();
                Toast(S.UpscaleSmall ? "작은 이미지도 확대: 켬" : "작은 이미지는 원래 크기로");
                break;
            case Cmd.ZoomIn: ZoomAt(zoom * 1.25); break;
            case Cmd.ZoomOut: ZoomAt(zoom / 1.25); break;
            case Cmd.Zoom100: ZoomAt(1); Toast("100%"); break;
            case Cmd.LockZoom:
                lockZoom = !lockZoom;
                if (lockZoom) manualZoom = true;
                UpdateStatusRight();
                Toast(lockZoom ? $"배율 고정: {zoom * 100:0}%" : "배율 고정 해제");
                break;
            case Cmd.CycleInterp:
                S.Interp = S.Interp switch { Interp.Smooth => Interp.Sharpen, Interp.Sharpen => Interp.Pixel, _ => Interp.Smooth };
                ApplyInterp();
                break;

            case Cmd.RotateRight: Rotate(90); break;
            case Cmd.RotateLeft: Rotate(270); break;
            case Cmd.Rotate180: Rotate(180); break;
            case Cmd.RotateReset: rotation = 0; flipH = flipV = false; Refit(); Toast("회전 초기화"); break;
            case Cmd.FlipH: flipH = !flipH; UpdateTransform(); Toast("좌우 뒤집기"); break;
            case Cmd.FlipV: flipV = !flipV; UpdateTransform(); Toast("상하 뒤집기"); break;

            case Cmd.OnePage: SetPages(1); break;
            case Cmd.TwoPage: SetPages(2); break;
            case Cmd.FourPage: SetPages(4); break;
            case Cmd.ToggleDirection:
                S.RightToLeft = !S.RightToLeft;
                Relayout();
                Toast(S.RightToLeft ? "오른쪽 → 왼쪽으로 읽기 (만화)" : "왼쪽 → 오른쪽으로 읽기");
                break;
            case Cmd.ToggleCover:
                S.CoverSingle = !S.CoverSingle;
                Relayout();
                Toast(S.CoverSingle ? "첫 장(표지) 단독 표시: 켬" : "첫 장(표지) 단독 표시: 끔");
                break;

            case Cmd.Fullscreen: ToggleFullscreen(); break;
            case Cmd.Escape:
                if (fullscreen) ToggleFullscreen();
                else if (slideTimer.IsEnabled) StopSlideshow();
                else Close();
                break;
            case Cmd.ToggleMaximize:
                if (fullscreen) ToggleFullscreen();
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                break;
            case Cmd.Minimize: WindowState = WindowState.Minimized; break;
            case Cmd.TopMost:
                S.TopMost = !S.TopMost;
                Topmost = S.TopMost;
                Toast(S.TopMost ? "항상 위: 켬" : "항상 위: 끔");
                break;
            case Cmd.ToggleThumbs: S.ShowThumbs = !S.ShowThumbs; UpdateChrome(); SyncThumbSelection(); break;
            case Cmd.ToggleStatus: S.ShowStatusBar = !S.ShowStatusBar; UpdateChrome(); break;
            case Cmd.ToggleInfo:
                InfoPanel.Visibility = InfoPanel.IsVisible ? Visibility.Collapsed : Visibility.Visible;
                UpdateInfoPanel();
                break;
            case Cmd.TogglePixel:
                pixelInfo = !pixelInfo;
                pixelText = null;
                UpdateStatusRight();
                Toast(pixelInfo ? "픽셀 정보: 마우스를 올리면 상태 표시줄에 표시" : "픽셀 정보: 끔");
                if (pixelInfo && !S.ShowStatusBar) { S.ShowStatusBar = true; UpdateChrome(); }
                break;

            case Cmd.Slideshow: StartSlideshow(arg); break;
            case Cmd.SlideStop: StopSlideshow(); break;
            case Cmd.AddBookmark: AddBookmark(); break;
            case Cmd.ShowBookmarks: ShowBookmarks(); break;

            case Cmd.OpenFile: OpenFileDialogCmd(); break;
            case Cmd.OpenFolder: OpenFolderDialogCmd(); break;
            case Cmd.Reload: ReloadSource(); Toast("새로고침"); break;
            case Cmd.OpenLocation: OpenLocation(); break;
            case Cmd.CopyClipboard: CopyToClipboard(); break;
            case Cmd.CopyToFolder: CopyOrMove(move: false); break;
            case Cmd.MoveToFolder: CopyOrMove(move: true); break;
            case Cmd.Rename: RenameCurrent(); break;
            case Cmd.Delete: DeleteCurrent(); break;
            case Cmd.SaveAs: SaveAsCmd(); break;
            case Cmd.Wallpaper: SetWallpaperCmd(); break;
            case Cmd.Settings: ShowSettings(); break;
            case Cmd.Help: Dialogs.ShowHelp(this); break;
            case Cmd.CheckUpdate: MongUpdate.UpdateUi.CheckNow(this, "mongview", "몽뷰", AppVersionNumber, null); break;
        }
    }

    bool ArrowsReversed => S.RightToLeft && S.ArrowsFollowDirection;

    // ───────────── 이동 ─────────────

    void Next(bool auto = false, bool isRepeat = false)
    {
        if (source == null) return;
        if (viewIndex < views.Count - 1) { viewIndex++; ShowView(); }
        else AtEdge(+1, auto, isRepeat);
    }

    void Prev(bool toEnd = false, bool isRepeat = false)
    {
        if (source == null) return;
        if (viewIndex > 0) { viewIndex--; ShowView(scrollToEnd: toEnd); }
        else AtEdge(-1, false, isRepeat);
    }

    void GoToView(int v, bool toEnd = false)
    {
        if (source == null || views.Count == 0) return;
        viewIndex = Math.Clamp(v, 0, views.Count - 1);
        ShowView(scrollToEnd: toEnd);
    }

    void GoToPage(int page)
    {
        if (source == null || page < 0 || page >= PageCount) return;
        if (Array.IndexOf(CurrentView, page) >= 0) return;
        GoToView(SpreadLayout.ViewOf(views, page));
    }

    /// <summary>
    /// 첫 장·마지막 장에서 더 넘길 때. 반복이면 처음/끝으로, 아니면 이웃 폴더·압축으로.
    /// 실수로 넘어가지 않게, 사람이 넘길 때는 안내를 띄우고 한 번 더 눌러야(키 반복 제외) 넘어간다.
    /// </summary>
    void AtEdge(int dir, bool auto, bool isRepeat)
    {
        if (S.Loop)
        {
            if (isRepeat) return;
            GoToView(dir > 0 ? 0 : views.Count - 1, toEnd: dir < 0);
            Toast(dir > 0 ? "처음 장으로 돌아왔습니다" : "마지막 장으로 갔습니다");
            return;
        }
        string edgeText = dir > 0 ? "마지막 장입니다" : "첫 장입니다";
        if (!S.AutoNextSource)
        {
            Toast(edgeText);
            if (auto) StopSlideshow();
            return;
        }
        string sib = PageSources.FindSibling(source.Path, dir);
        if (sib == null)
        {
            Toast(edgeText + (dir > 0 ? " (다음 폴더·압축 없음)" : " (이전 폴더·압축 없음)"));
            if (auto) StopSlideshow();
            return;
        }
        string sibName = System.IO.Path.GetFileName(sib);
        if (!auto)
        {
            if (isRepeat) return;
            double since = (DateTime.Now - endPromptTime).TotalSeconds;
            bool confirmed = endPromptDir == dir && since > 0.25 && since < 4;
            if (!confirmed)
            {
                endPromptDir = dir;
                endPromptTime = DateTime.Now;
                Toast($"{edgeText}. 한 번 더 넘기면 {(dir > 0 ? "다음" : "이전")}: {sibName}", 4);
                return;
            }
        }
        endPromptTime = DateTime.MinValue;
        if (OpenPath(sib, atEnd: dir < 0, resume: false))
            Toast((dir > 0 ? "다음: " : "이전: ") + sibName);
    }

    void JumpSource(int dir)
    {
        if (source == null) return;
        string sib = PageSources.FindSibling(source.Path, dir);
        if (sib == null) { Toast(dir > 0 ? "다음 폴더·압축이 없습니다" : "이전 폴더·압축이 없습니다"); return; }
        if (OpenPath(sib)) Toast((dir > 0 ? "다음: " : "이전: ") + System.IO.Path.GetFileName(sib));
    }

    // ───────────── 보기 설정 ─────────────

    void SetFit(FitMode mode, string text)
    {
        S.Fit = mode;
        manualZoom = false;
        lockZoom = false;
        ApplyFit(resetPan: true);
        ScheduleSharpen();
        Toast(text);
    }

    void SetPages(int n)
    {
        if (S.PagesPerView != n)
        {
            S.PagesPerView = n;
            Relayout();
        }
        Toast(n switch { 2 => "두 장 보기", 4 => "네 장 보기", _ => "한 장 보기" } + (n > 1 ? (S.RightToLeft ? " (오→왼)" : " (왼→오)") : ""));
    }

    void Rotate(int deg)
    {
        rotation = (rotation + deg) % 360;
        Refit();
        Toast($"회전 {rotation}°");
    }

    void Refit()
    {
        if (!lockZoom) manualZoom = false;
        ApplyFit(resetPan: true);
        ScheduleSharpen();
    }

    void ApplyInterp()
    {
        foreach (var sh in shown)
        {
            if (sh.Image == null) continue;
            RenderOptions.SetBitmapScalingMode(sh.Image,
                S.Interp == Interp.Pixel ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
        }
        ScheduleSharpen();
        Toast(S.Interp switch { Interp.Sharpen => "필터: 부드럽게 + 선명하게", Interp.Pixel => "필터: 픽셀 그대로", _ => "필터: 부드럽게" });
    }

    // ───────────── 마우스 ─────────────

    void OnLeftDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            Run(Cmd.ToggleMaximize);
            e.Handled = true;
            return;
        }
        dragging = true;
        dragStart = e.GetPosition(Viewport);
        dragPanX = panX;
        dragPanY = panY;
        Viewport.CaptureMouse();
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(Viewport);
        if (dragging)
        {
            double dpi = Dpi;
            panX = dragPanX + (p.X - dragStart.X) * dpi;
            panY = dragPanY + (p.Y - dragStart.Y) * dpi;
            ClampPan();
            UpdateTransform();
            Viewport.Cursor = Cursors.SizeAll;
        }
        if (pixelInfo) UpdatePixelInfo(p);
    }

    void EndDrag()
    {
        if (!dragging) return;
        dragging = false;
        Viewport.ReleaseMouseCapture();
        Viewport.Cursor = null;
    }

    void OnWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ZoomAt(zoom * (e.Delta > 0 ? 1.15 : 1 / 1.15), e.GetPosition(Viewport));
            return;
        }
        var (_, vh) = ViewSize();
        if (e.Delta < 0) { if (!ScrollBy(vh * 0.2)) Next(); }
        else { if (!ScrollBy(-vh * 0.2)) Prev(toEnd: true); }
    }

    void OnOtherButton(object sender, MouseButtonEventArgs e)
    {
        switch (e.ChangedButton)
        {
            case MouseButton.Middle:
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) Run(Cmd.Zoom100);
                else ToggleFullscreen();
                e.Handled = true;
                break;
            case MouseButton.XButton1: Prev(); e.Handled = true; break;
            case MouseButton.XButton2: Next(); e.Handled = true; break;
        }
    }

    void OnThumbWheel(object sender, MouseWheelEventArgs e)
    {
        var sv = FindChild<ScrollViewer>(Thumbs);
        if (sv == null) return;
        sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

    static T FindChild<T>(DependencyObject d) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
        {
            var c = VisualTreeHelper.GetChild(d, i);
            if (c is T t) return t;
            var r = FindChild<T>(c);
            if (r != null) return r;
        }
        return null;
    }

    // ───────────── 열기 대화상자 ─────────────

    void OpenFileDialogCmd()
    {
        string imgs = string.Join(";", Formats.ImageExts.Select(x => "*" + x));
        string arcs = string.Join(";", Formats.ArchiveExts.Select(x => "*" + x));
        var dlg = new OpenFileDialog
        {
            Title = "열기",
            Filter = $"이미지·압축 파일|{imgs};{arcs}|이미지|{imgs}|압축 파일|{arcs}|모든 파일|*.*",
            InitialDirectory = SourceDir(),
        };
        if (dlg.ShowDialog(this) == true) OpenPath(dlg.FileName);
    }

    void OpenFolderDialogCmd()
    {
        var dlg = new OpenFolderDialog { Title = "폴더 열기", InitialDirectory = SourceDir() };
        if (dlg.ShowDialog(this) == true) OpenPath(dlg.FolderName);
    }

    string SourceDir()
    {
        if (source == null) return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        return source.IsArchive ? System.IO.Path.GetDirectoryName(source.Path) : source.Path;
    }

    void OpenLocation()
    {
        if (source == null) return;
        string target = CurrentEntry?.FilePath ?? source.Path;
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{target}\"") { UseShellExecute = true });
    }

    // ───────────── 우클릭 메뉴 ─────────────

    void ShowContextMenu()
    {
        bool has = source != null;
        bool isFile = CurrentEntry?.IsFile == true;

        MenuItem Item(string header, Action act, string gesture = "", bool check = false, bool enabled = true)
        {
            var mi = new MenuItem { Header = header, InputGestureText = gesture, IsChecked = check, IsEnabled = enabled };
            mi.Click += (_, _) => act();
            return mi;
        }
        MenuItem C(string header, Cmd cmd, int arg = 0, bool check = false, bool enabled = true) =>
            Item(header, () => Run(cmd, arg), KeyMap.GestureOf(cmd, arg), check, enabled);
        MenuItem Sub(string header, params object[] items)
        {
            var mi = new MenuItem { Header = header };
            foreach (var it in items) mi.Items.Add(it);
            return mi;
        }
        Separator Sep() => new();
        MenuItem SortItem(string header, SortKey k) => Item(header, () =>
        {
            S.Sort = k;
            ReloadSource();
            Toast(header + " 정렬");
        }, "", S.Sort == k);

        var m = new ContextMenu();
        m.Items.Add(C("파일 열기…", Cmd.OpenFile));
        m.Items.Add(C("폴더 열기…", Cmd.OpenFolder));
        m.Items.Add(Sep());
        m.Items.Add(Sub("이동",
            C("처음 장", Cmd.First, enabled: has),
            C("마지막 장", Cmd.Last, enabled: has),
            Sep(),
            C("다음 폴더·압축", Cmd.NextSource, enabled: has),
            C("이전 폴더·압축", Cmd.PrevSource, enabled: has),
            Sep(),
            C("끝에서 처음으로 반복", Cmd.ToggleLoop, check: S.Loop),
            Item("마지막 장에서 다음 폴더·압축으로", () => { S.AutoNextSource = !S.AutoNextSource; }, "", S.AutoNextSource)));
        m.Items.Add(Sub("여러 장 보기",
            C("한 장", Cmd.OnePage, check: S.PagesPerView == 1),
            C("두 장", Cmd.TwoPage, check: S.PagesPerView == 2),
            C("네 장", Cmd.FourPage, check: S.PagesPerView == 4),
            Sep(),
            C("오른쪽 → 왼쪽 (만화)", Cmd.ToggleDirection, check: S.RightToLeft),
            C("첫 장(표지) 단독", Cmd.ToggleCover, check: S.CoverSingle)));
        m.Items.Add(Sub("맞춤·확대",
            C("창에 맞춤", Cmd.FitWindow, check: S.Fit == FitMode.Window),
            C("너비에 맞춤", Cmd.FitWidth, check: S.Fit == FitMode.Width),
            C("높이에 맞춤", Cmd.FitHeight, check: S.Fit == FitMode.Height),
            C("원본 크기", Cmd.FitOriginal, check: S.Fit == FitMode.Original),
            Sep(),
            C("작은 이미지도 확대", Cmd.ToggleUpscale, check: S.UpscaleSmall),
            C("배율 고정", Cmd.LockZoom, check: lockZoom),
            Sep(),
            C("확대", Cmd.ZoomIn),
            C("축소", Cmd.ZoomOut),
            C("100%", Cmd.Zoom100)));
        m.Items.Add(Sub("회전·뒤집기",
            C("오른쪽으로 90°", Cmd.RotateRight),
            C("왼쪽으로 90°", Cmd.RotateLeft),
            C("180°", Cmd.Rotate180),
            C("좌우 뒤집기", Cmd.FlipH, check: flipH),
            C("상하 뒤집기", Cmd.FlipV, check: flipV),
            Sep(),
            C("초기화", Cmd.RotateReset)));
        m.Items.Add(Sub("필터",
            Item("부드럽게", () => { S.Interp = Interp.Smooth; ApplyInterp(); }, "", S.Interp == Interp.Smooth),
            Item("부드럽게 + 선명하게", () => { S.Interp = Interp.Sharpen; ApplyInterp(); }, "", S.Interp == Interp.Sharpen),
            Item("픽셀 그대로", () => { S.Interp = Interp.Pixel; ApplyInterp(); }, "", S.Interp == Interp.Pixel)));
        m.Items.Add(Sub("정렬",
            SortItem("이름순", SortKey.Name),
            SortItem("수정한 날짜순", SortKey.Modified),
            SortItem("크기순", SortKey.Size),
            Sep(),
            Item("거꾸로", () => { S.SortDescending = !S.SortDescending; ReloadSource(); }, "", S.SortDescending)));
        m.Items.Add(Sub("슬라이드쇼",
            C("3초 간격으로 시작", Cmd.Slideshow, 3, enabled: has),
            C("5초 간격으로 시작", Cmd.Slideshow, 5, enabled: has),
            C("중지", Cmd.SlideStop, enabled: slideTimer.IsEnabled)));
        m.Items.Add(Sub("책갈피",
            C("책갈피 추가", Cmd.AddBookmark, enabled: has),
            C("책갈피 목록…", Cmd.ShowBookmarks)));
        m.Items.Add(Sep());
        m.Items.Add(Sub("파일",
            C("이미지 복사", Cmd.CopyClipboard, enabled: has),
            C(isFile ? "다른 폴더로 복사…" : "다른 폴더로 꺼내기…", Cmd.CopyToFolder, enabled: has),
            C("다른 폴더로 이동…", Cmd.MoveToFolder, enabled: isFile),
            C("이름 바꾸기…", Cmd.Rename, enabled: isFile),
            C("휴지통으로 삭제", Cmd.Delete, enabled: isFile),
            Sep(),
            C("다른 형식·크기로 저장…", Cmd.SaveAs, enabled: has),
            C("바탕화면으로 지정", Cmd.Wallpaper, enabled: has),
            C("탐색기에서 위치 열기", Cmd.OpenLocation, enabled: has),
            C("새로고침", Cmd.Reload, enabled: has)));
        m.Items.Add(Sub("표시",
            C("썸네일 막대", Cmd.ToggleThumbs, check: S.ShowThumbs),
            C("상태 표시줄", Cmd.ToggleStatus, check: S.ShowStatusBar),
            C("사진 정보", Cmd.ToggleInfo, check: InfoPanel.IsVisible),
            C("픽셀 정보", Cmd.TogglePixel, check: pixelInfo),
            Sep(),
            C("항상 위", Cmd.TopMost, check: S.TopMost),
            C("전체 화면", Cmd.Fullscreen, check: fullscreen)));
        m.Items.Add(Sep());
        m.Items.Add(C("설정…", Cmd.Settings));
        m.Items.Add(C("단축키 도움말", Cmd.Help));
        m.Items.Add(C("업데이트 확인", Cmd.CheckUpdate));
        m.Items.Add(Item("끝내기", Close, "Esc"));

        m.PlacementTarget = Viewport;
        m.IsOpen = true;
    }
}
