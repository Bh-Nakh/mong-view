using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoAdView.Core;

namespace NoAdView.Views;

/// <summary>코드로 만드는 작은 대화상자들(이름 바꾸기, 책갈피, 설정, 단축키, 변환 저장).</summary>
public static class Dialogs
{
    static Window MakeWindow(Window owner, string title, double width = double.NaN)
    {
        var w = new Window
        {
            Title = title,
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = double.IsNaN(width) ? SizeToContent.WidthAndHeight : SizeToContent.Height,
            ShowInTaskbar = false,
            FontFamily = new FontFamily("Malgun Gothic"),
            FontSize = 13,
            Icon = owner?.Icon,
        };
        if (!double.IsNaN(width)) w.Width = width;
        return w;
    }

    static StackPanel Buttons(Window w, string okText, Func<bool> onOk, string cancelText = "취소")
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        var ok = new Button { Content = okText, MinWidth = 80, Padding = new Thickness(10, 3, 10, 3), IsDefault = true };
        ok.Click += (_, _) => { if (onOk()) w.DialogResult = true; };
        p.Children.Add(ok);
        if (cancelText != null)
        {
            var cancel = new Button { Content = cancelText, MinWidth = 80, Padding = new Thickness(10, 3, 10, 3), IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };
            p.Children.Add(cancel);
        }
        return p;
    }

    static TextBlock Label(string text, double top = 0) =>
        new() { Text = text, Margin = new Thickness(0, top, 0, 4), TextWrapping = TextWrapping.Wrap };

    // ───────────── 이름 바꾸기 ─────────────

    public static string Prompt(Window owner, string title, string label, string initial, bool selectStem = false)
    {
        var w = MakeWindow(owner, title, 440);
        var box = new TextBox { Text = initial, Padding = new Thickness(3) };
        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(Label(label));
        root.Children.Add(box);
        string result = null;
        root.Children.Add(Buttons(w, "확인", () => { result = box.Text; return true; }));
        w.Content = root;
        w.Loaded += (_, _) =>
        {
            box.Focus();
            int dot = selectStem ? initial.LastIndexOf('.') : -1;
            box.Select(0, dot > 0 ? dot : initial.Length);
        };
        return w.ShowDialog() == true ? result : null;
    }

    // ───────────── 책갈피 ─────────────

    public static Bookmark ShowBookmarks(Window owner, List<Bookmark> list)
    {
        var w = MakeWindow(owner, "책갈피", 560);
        var lb = new ListBox { Height = 320 };
        void Fill()
        {
            lb.Items.Clear();
            foreach (var b in list)
                lb.Items.Add(new ListBoxItem
                {
                    Tag = b,
                    Content = $"{System.IO.Path.GetFileName(b.SourcePath.TrimEnd('\\'))}  ›  {b.Page}    ({b.Added:yyyy-MM-dd HH:mm})",
                    ToolTip = b.SourcePath,
                });
            if (lb.Items.Count > 0) lb.SelectedIndex = 0;
        }
        Fill();

        Bookmark chosen = null;
        bool Open()
        {
            if (lb.SelectedItem is not ListBoxItem it) return false;
            chosen = (Bookmark)it.Tag;
            return true;
        }
        lb.MouseDoubleClick += (_, _) => { if (Open()) w.DialogResult = true; };

        var del = new Button { Content = "삭제", MinWidth = 80, Padding = new Thickness(10, 3, 10, 3), HorizontalAlignment = HorizontalAlignment.Left };
        del.Click += (_, _) =>
        {
            if (lb.SelectedItem is not ListBoxItem it) return;
            int i = lb.SelectedIndex;
            list.Remove((Bookmark)it.Tag);
            Fill();
            if (lb.Items.Count > 0) lb.SelectedIndex = Math.Min(i, lb.Items.Count - 1);
        };

        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(Label(list.Count == 0 ? "책갈피가 없습니다. 보는 중에 B 키를 누르면 추가됩니다." : "열 책갈피를 고르세요 (두 번 클릭해도 열립니다)."));
        root.Children.Add(lb);
        var bottom = new DockPanel { Margin = new Thickness(0, 14, 0, 0) };
        bottom.Children.Add(del);
        var btns = Buttons(w, "열기", Open, "닫기");
        btns.Margin = new Thickness(0);
        DockPanel.SetDock(btns, Dock.Right);
        bottom.Children.Add(btns);
        root.Children.Add(bottom);
        w.Content = root;
        return w.ShowDialog() == true ? chosen : null;
    }

    // ───────────── 단축키 도움말 ─────────────

    public static void ShowHelp(Window owner)
    {
        var w = MakeWindow(owner, "단축키 도움말", 560);
        w.SizeToContent = SizeToContent.Manual;
        w.Height = Math.Min(760, SystemParameters.WorkArea.Height - 40);
        w.ResizeMode = ResizeMode.CanResize;

        var grid = new Grid { Margin = new Thickness(16, 10, 16, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        int row = 0;
        void AddRow(UIElement a, UIElement b = null)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(a, row);
            if (b == null) Grid.SetColumnSpan(a, 2);
            grid.Children.Add(a);
            if (b != null)
            {
                Grid.SetRow(b, row);
                Grid.SetColumn(b, 1);
                grid.Children.Add(b);
            }
            row++;
        }
        foreach (var (group, binds) in KeyMap.Groups)
        {
            AddRow(new TextBlock { Text = group, FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 12, 0, 4) });
            foreach (var g in binds.GroupBy(b => b.Desc))
            {
                string keys = string.Join(", ", g.Select(b => b.KeyText).Distinct());
                AddRow(new TextBlock { Text = keys, Foreground = Brushes.SteelBlue, Margin = new Thickness(8, 2, 0, 2), TextWrapping = TextWrapping.Wrap },
                       new TextBlock { Text = g.Key, Margin = new Thickness(0, 2, 0, 2), TextWrapping = TextWrapping.Wrap });
            }
        }
        AddRow(new TextBlock { Text = "마우스", FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 12, 0, 4) });
        foreach (var (k, d) in new[]
        {
            ("휠", "스크롤, 끝이면 다음/이전 장"), ("Ctrl+휠", "마우스 위치 기준 확대/축소"), ("끌기", "확대한 이미지 이동"),
            ("두 번 클릭", "최대화 / 복원"), ("가운데 버튼", "전체 화면"), ("앞/뒤 버튼", "다음/이전 장"), ("오른쪽 버튼", "메뉴"),
            ("끌어다 놓기", "파일·폴더·압축 열기"),
        })
            AddRow(new TextBlock { Text = k, Foreground = Brushes.SteelBlue, Margin = new Thickness(8, 2, 0, 2) },
                   new TextBlock { Text = d, Margin = new Thickness(0, 2, 0, 2) });

        var dock = new DockPanel();
        var close = new Button { Content = "닫기", MinWidth = 80, Padding = new Thickness(10, 3, 10, 3), IsCancel = true, IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16, 8, 16, 12) };
        close.Click += (_, _) => w.Close();
        DockPanel.SetDock(close, Dock.Bottom);
        dock.Children.Add(close);
        dock.Children.Add(new ScrollViewer { Content = grid, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        w.Content = dock;
        w.ShowDialog();
    }

    // ───────────── 설정 ─────────────

    public static bool ShowSettings(Window owner, Settings s)
    {
        var w = MakeWindow(owner, "설정", 520);
        var root = new StackPanel { Margin = new Thickness(18, 14, 18, 14) };

        CheckBox Check(string text, bool value) =>
            new() { Content = text, IsChecked = value, Margin = new Thickness(0, 3, 0, 3) };
        TextBlock Header(string text) =>
            new() { Text = text, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 12, 0, 4) };

        root.Children.Add(Header("보기"));
        var cUpscale = Check("작은 이미지도 창 크기에 맞게 확대", s.UpscaleSmall);
        var cCover = Check("두 장 보기에서 첫 장(표지)은 한 장만", s.CoverSingle);
        var cExif = Check("사진의 EXIF 방향대로 자동 회전", s.AutoRotateExif);
        root.Children.Add(cUpscale); root.Children.Add(cCover); root.Children.Add(cExif);

        root.Children.Add(Header("이동"));
        var cLoop = Check("마지막 장에서 처음 장으로 반복", s.Loop);
        var cNext = Check("마지막 장에서 다음 폴더·압축 파일로 넘어가기", s.AutoNextSource);
        var cArrow = Check("오른쪽→왼쪽 보기에서는 ←/→ 방향키도 반대로", s.ArrowsFollowDirection);
        var cResume = Check("다시 열면 마지막으로 보던 장부터", s.ResumeLastPage);
        root.Children.Add(cLoop); root.Children.Add(cNext); root.Children.Add(cArrow); root.Children.Add(cResume);

        var sortRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
        sortRow.Children.Add(new TextBlock { Text = "정렬:", VerticalAlignment = VerticalAlignment.Center, Width = 60 });
        var sortBox = new ComboBox { Width = 150, ItemsSource = new[] { "이름순", "수정한 날짜순", "크기순" }, SelectedIndex = (int)s.Sort };
        var cDesc = new CheckBox { Content = "거꾸로", IsChecked = s.SortDescending, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        sortRow.Children.Add(sortBox); sortRow.Children.Add(cDesc);
        root.Children.Add(sortRow);

        root.Children.Add(Header("파일·성능"));
        var cConfirm = Check("삭제하기 전에 확인", s.ConfirmDelete);
        root.Children.Add(cConfirm);
        var cacheRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
        cacheRow.Children.Add(new TextBlock { Text = "미리 읽기 메모리(MB):", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        var cacheBox = new TextBox { Text = s.CacheMB.ToString(), Width = 80 };
        cacheRow.Children.Add(cacheBox);
        root.Children.Add(cacheRow);

        root.Children.Add(Header("탐색기 연결"));
        var assocState = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6), Foreground = Brushes.DimGray };
        void RefreshAssoc()
        {
            bool reg = false;
            try { reg = FileAssoc.IsRegistered(); } catch { }
            assocState.Text = (reg ? "등록됨. " : "등록 안 됨. ") +
                "Windows 10 이상은 프로그램이 기본 앱을 직접 바꿀 수 없어, 등록한 뒤 열리는 '기본 앱' 화면에서 몽뷰를 골라 주세요. " +
                "(사진 → 몽뷰, 또는 '파일 형식별 기본 앱 선택')";
        }
        RefreshAssoc();
        root.Children.Add(assocState);
        var assocRow = new StackPanel { Orientation = Orientation.Horizontal };
        Button Btn(string text, Action act)
        {
            var b = new Button { Content = text, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(0, 0, 8, 0) };
            b.Click += (_, _) =>
            {
                try { act(); }
                catch (Exception ex) { MessageBox.Show(w, ex.Message, "탐색기 연결", MessageBoxButton.OK, MessageBoxImage.Warning); }
                RefreshAssoc();
            };
            return b;
        }
        assocRow.Children.Add(Btn("연결 등록", () =>
        {
            FileAssoc.Register();
            FileAssoc.OpenDefaultAppsSettings();
        }));
        assocRow.Children.Add(Btn("연결 해제", FileAssoc.Unregister));
        assocRow.Children.Add(Btn("기본 앱 설정 열기", FileAssoc.OpenDefaultAppsSettings));
        root.Children.Add(assocRow);

        root.Children.Add(Buttons(w, "확인", () =>
        {
            if (!int.TryParse(cacheBox.Text, out int mb) || mb < 128 || mb > 8192)
            {
                MessageBox.Show(w, "미리 읽기 메모리는 128 ~ 8192 사이 숫자로 적어 주세요.", "설정");
                return false;
            }
            s.UpscaleSmall = cUpscale.IsChecked == true;
            s.CoverSingle = cCover.IsChecked == true;
            s.AutoRotateExif = cExif.IsChecked == true;
            s.Loop = cLoop.IsChecked == true;
            s.AutoNextSource = cNext.IsChecked == true;
            s.ArrowsFollowDirection = cArrow.IsChecked == true;
            s.ResumeLastPage = cResume.IsChecked == true;
            s.Sort = (Core.SortKey)Math.Max(0, sortBox.SelectedIndex);
            s.SortDescending = cDesc.IsChecked == true;
            s.ConfirmDelete = cConfirm.IsChecked == true;
            s.CacheMB = mb;
            return true;
        }));
        w.Content = root;
        return w.ShowDialog() == true;
    }

    // ───────────── 다른 형식·크기로 저장 ─────────────

    public static ExportOptions ShowExport(Window owner, BitmapSource bmp, int quality, int rotation, bool flipH, bool flipV)
    {
        var w = MakeWindow(owner, "다른 형식·크기로 저장", 460);
        var o = new ExportOptions { Quality = quality };
        var root = new StackPanel { Margin = new Thickness(18, 14, 18, 14) };

        var fmtRow = new StackPanel { Orientation = Orientation.Horizontal };
        fmtRow.Children.Add(new TextBlock { Text = "형식:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
        var fmt = new ComboBox { Width = 120, ItemsSource = new[] { "JPG", "PNG", "BMP", "TIFF", "GIF" }, SelectedIndex = 0 };
        fmtRow.Children.Add(fmt);
        root.Children.Add(fmtRow);

        var qRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        qRow.Children.Add(new TextBlock { Text = "품질:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
        var q = new Slider { Minimum = 10, Maximum = 100, Value = quality, Width = 220, IsSnapToTickEnabled = true, TickFrequency = 1, VerticalAlignment = VerticalAlignment.Center };
        var qText = new TextBlock { Text = quality.ToString(), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Width = 30 };
        q.ValueChanged += (_, _) => qText.Text = ((int)q.Value).ToString();
        qRow.Children.Add(q); qRow.Children.Add(qText);
        root.Children.Add(qRow);
        fmt.SelectionChanged += (_, _) => qRow.IsEnabled = fmt.SelectedIndex == 0;

        root.Children.Add(new TextBlock { Text = "크기", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 14, 0, 4) });
        TextBox Num(string v) => new() { Text = v, Width = 64, Margin = new Thickness(6, 0, 4, 0) };
        RadioButton Radio(string text, bool check) => new() { Content = text, IsChecked = check, GroupName = "size", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 3, 0, 3) };

        var rNone = Radio("원본 크기 그대로", true);
        var rPct = Radio("비율", false); var tPct = Num("50");
        var rLong = Radio("긴 변을", false); var tLong = Num("1920");
        var rBox = Radio("가로×세로 안에 맞춤", false); var tBw = Num("1920"); var tBh = Num("1080");
        StackPanel Line(params UIElement[] els)
        {
            var p = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var e in els) p.Children.Add(e);
            return p;
        }
        TextBlock T(string s) => new() { Text = s, VerticalAlignment = VerticalAlignment.Center };
        root.Children.Add(rNone);
        root.Children.Add(Line(rPct, tPct, T("%")));
        root.Children.Add(Line(rLong, tLong, T("px 로")));
        root.Children.Add(Line(rBox, tBw, T("×"), tBh));
        var cRot = new CheckBox { Content = "지금 보이는 회전·뒤집기 적용", IsChecked = rotation != 0 || flipH || flipV, Margin = new Thickness(0, 10, 0, 0) };
        root.Children.Add(cRot);
        var result = new TextBlock { Margin = new Thickness(0, 10, 0, 0), Foreground = Brushes.DimGray };
        root.Children.Add(result);

        double D(TextBox t) => double.TryParse(t.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var v) ? v : 0;
        bool Read()
        {
            o.Format = (ExportFormat)fmt.SelectedIndex;
            o.Quality = (int)q.Value;
            o.Resize = rPct.IsChecked == true ? ExportResize.Percent : rLong.IsChecked == true ? ExportResize.LongEdge :
                       rBox.IsChecked == true ? ExportResize.Box : ExportResize.None;
            o.Percent = D(tPct);
            o.LongEdge = (int)D(tLong);
            o.BoxWidth = (int)D(tBw);
            o.BoxHeight = (int)D(tBh);
            o.ApplyRotation = cRot.IsChecked == true;
            int bw = bmp.PixelWidth, bh = bmp.PixelHeight;
            if (o.ApplyRotation && rotation % 180 != 0) (bw, bh) = (bh, bw);
            double sc = ImageExport.ScaleFor(bw, bh, o);
            int nw = (int)Math.Round(bw * sc), nh = (int)Math.Round(bh * sc);
            bool ok = sc > 0 && nw >= 1 && nh >= 1 && nw <= 30000 && nh <= 30000;
            result.Text = ok ? $"결과 크기: {nw} × {nh}" : "크기 값을 확인해 주세요.";
            return ok;
        }
        foreach (var t in new[] { tPct, tLong, tBw, tBh }) t.TextChanged += (_, _) => Read();
        foreach (var r in new[] { rNone, rPct, rLong, rBox }) r.Checked += (_, _) => Read();
        cRot.Click += (_, _) => Read();
        Read();

        root.Children.Add(Buttons(w, "저장…", Read));
        w.Content = root;
        return w.ShowDialog() == true ? o : null;
    }
}
