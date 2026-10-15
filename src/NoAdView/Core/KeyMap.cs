using System.Windows.Input;

namespace NoAdView.Core;

public enum Cmd
{
    Next, Prev, First, Last, NextSource, PrevSource, ScrollDownOrNext, ScrollUpOrPrev, LeftKey, RightKey,
    FitWindow, FitWidth, FitHeight, FitOriginal, ToggleUpscale, ZoomIn, ZoomOut, Zoom100, LockZoom,
    RotateRight, RotateLeft, Rotate180, RotateReset, FlipH, FlipV,
    OnePage, TwoPage, FourPage, ToggleDirection, ToggleCover,
    Fullscreen, Escape, ToggleMaximize, Minimize,
    ToggleThumbs, ToggleStatus, ToggleInfo, TogglePixel, TopMost, CycleInterp,
    AddBookmark, ShowBookmarks, Slideshow, SlideStop, ToggleLoop,
    Reload, Delete, Rename, CopyToFolder, MoveToFolder, CopyClipboard, SaveAs, Wallpaper,
    OpenFile, OpenFolder, Settings, Help, OpenLocation, CheckUpdate,
}

public sealed record KeyBind(Key Key, ModifierKeys Mods, Cmd Cmd, string Desc, int Arg = 0)
{
    public string KeyText
    {
        get
        {
            string k = Key switch
            {
                Key.PageDown => "PgDn", Key.PageUp => "PgUp",
                Key.OemPlus or Key.Add => "+", Key.OemMinus or Key.Subtract => "-", Key.Multiply => "*",
                Key.Left => "←", Key.Right => "→", Key.Up => "↑", Key.Down => "↓",
                Key.Return => "Enter", Key.Escape => "Esc", Key.Back => "Backspace", Key.Delete => "Del",
                >= Key.D0 and <= Key.D9 => ((int)(Key - Key.D0)).ToString(),
                _ => Key.ToString(),
            };
            string m = "";
            if (Mods.HasFlag(ModifierKeys.Control)) m += "Ctrl+";
            if (Mods.HasFlag(ModifierKeys.Alt)) m += "Alt+";
            if (Mods.HasFlag(ModifierKeys.Shift)) m += "Shift+";
            return m + k;
        }
    }
}

/// <summary>단축키 표. 꿀뷰에 익숙한 사람이 바로 쓸 수 있게 비슷하게 배치했다.</summary>
public static class KeyMap
{
    const ModifierKeys N = ModifierKeys.None, C = ModifierKeys.Control, A = ModifierKeys.Alt, S = ModifierKeys.Shift;

    public static readonly List<(string Group, KeyBind[] Binds)> Groups = new()
    {
        ("이동", new KeyBind[]
        {
            new(Key.PageDown, N, Cmd.Next, "다음 장"),
            new(Key.PageUp, N, Cmd.Prev, "이전 장"),
            new(Key.Right, N, Cmd.RightKey, "오른쪽 장 (우→좌 보기에서는 이전)"),
            new(Key.Left, N, Cmd.LeftKey, "왼쪽 장 (우→좌 보기에서는 다음)"),
            new(Key.Space, N, Cmd.ScrollDownOrNext, "아래로 스크롤, 끝이면 다음 장"),
            new(Key.Back, N, Cmd.Prev, "이전 장"),
            new(Key.Down, N, Cmd.ScrollDownOrNext, "아래로 스크롤, 끝이면 다음 장"),
            new(Key.Up, N, Cmd.ScrollUpOrPrev, "위로 스크롤, 끝이면 이전 장"),
            new(Key.Home, N, Cmd.First, "처음 장"),
            new(Key.End, N, Cmd.Last, "마지막 장"),
            new(Key.PageDown, C, Cmd.NextSource, "다음 폴더·압축 파일"),
            new(Key.PageUp, C, Cmd.PrevSource, "이전 폴더·압축 파일"),
            new(Key.L, C | S, Cmd.ToggleLoop, "끝에서 처음으로 반복 켜기/끄기"),
        }),
        ("여러 장 보기", new KeyBind[]
        {
            new(Key.D1, N, Cmd.OnePage, "한 장 보기"),
            new(Key.D2, N, Cmd.TwoPage, "두 장 보기"),
            new(Key.D4, N, Cmd.FourPage, "네 장 보기"),
            new(Key.D, N, Cmd.ToggleDirection, "왼→오 / 오→왼 방향 바꾸기"),
            new(Key.C, N, Cmd.ToggleCover, "두 장 보기에서 첫 장(표지) 단독 표시"),
        }),
        ("맞춤·확대", new KeyBind[]
        {
            new(Key.D9, N, Cmd.FitWindow, "창에 맞춤"),
            new(Key.D8, N, Cmd.FitWidth, "너비에 맞춤"),
            new(Key.D7, N, Cmd.FitHeight, "높이에 맞춤"),
            new(Key.D0, N, Cmd.FitOriginal, "원본 크기"),
            new(Key.Z, N, Cmd.ToggleUpscale, "작은 이미지도 확대해서 맞춤"),
            new(Key.OemPlus, N, Cmd.ZoomIn, "확대"),
            new(Key.Add, N, Cmd.ZoomIn, "확대"),
            new(Key.OemMinus, N, Cmd.ZoomOut, "축소"),
            new(Key.Subtract, N, Cmd.ZoomOut, "축소"),
            new(Key.Multiply, N, Cmd.Zoom100, "잠깐 100% 보기"),
            new(Key.K, C, Cmd.LockZoom, "배율 고정 (장을 넘겨도 유지)"),
            new(Key.U, N, Cmd.CycleInterp, "필터 바꾸기 (부드럽게 → 선명하게 → 픽셀)"),
        }),
        ("회전", new KeyBind[]
        {
            new(Key.R, C, Cmd.RotateRight, "오른쪽으로 90° 회전"),
            new(Key.L, C, Cmd.RotateLeft, "왼쪽으로 90° 회전"),
            new(Key.Right, A, Cmd.RotateRight, "오른쪽으로 90° 회전"),
            new(Key.Left, A, Cmd.RotateLeft, "왼쪽으로 90° 회전"),
            new(Key.Down, A, Cmd.Rotate180, "180° 회전"),
            new(Key.Up, A, Cmd.RotateReset, "회전·뒤집기 초기화"),
            new(Key.M, C, Cmd.FlipH, "좌우 뒤집기"),
            new(Key.F, C, Cmd.FlipV, "상하 뒤집기"),
        }),
        ("창·표시", new KeyBind[]
        {
            new(Key.F11, N, Cmd.Fullscreen, "전체 화면"),
            new(Key.Return, A, Cmd.Fullscreen, "전체 화면"),
            new(Key.Return, N, Cmd.Fullscreen, "전체 화면"),
            new(Key.Escape, N, Cmd.Escape, "전체 화면 해제 / 슬라이드쇼 중지 / 끝내기"),
            new(Key.M, N, Cmd.ToggleMaximize, "최대화 / 복원"),
            new(Key.Q, N, Cmd.Minimize, "최소화"),
            new(Key.A, C, Cmd.TopMost, "항상 위"),
            new(Key.T, N, Cmd.ToggleThumbs, "썸네일 막대"),
            new(Key.Tab, N, Cmd.ToggleStatus, "상태 표시줄"),
            new(Key.I, N, Cmd.ToggleInfo, "사진 정보(EXIF)"),
            new(Key.P, N, Cmd.TogglePixel, "픽셀 좌표·RGB 표시"),
        }),
        ("슬라이드쇼·책갈피", new KeyBind[]
        {
            new(Key.D1, C, Cmd.Slideshow, "슬라이드쇼 1초", 1),
            new(Key.D2, C, Cmd.Slideshow, "슬라이드쇼 2초", 2),
            new(Key.D3, C, Cmd.Slideshow, "슬라이드쇼 3초", 3),
            new(Key.D4, C, Cmd.Slideshow, "슬라이드쇼 4초", 4),
            new(Key.D5, C, Cmd.Slideshow, "슬라이드쇼 5초", 5),
            new(Key.D6, C, Cmd.Slideshow, "슬라이드쇼 6초", 6),
            new(Key.D7, C, Cmd.Slideshow, "슬라이드쇼 7초", 7),
            new(Key.D8, C, Cmd.Slideshow, "슬라이드쇼 8초", 8),
            new(Key.D9, C, Cmd.Slideshow, "슬라이드쇼 9초", 9),
            new(Key.D0, C, Cmd.SlideStop, "슬라이드쇼 중지"),
            new(Key.B, N, Cmd.AddBookmark, "책갈피 추가"),
            new(Key.F3, N, Cmd.ShowBookmarks, "책갈피 목록"),
        }),
        ("파일", new KeyBind[]
        {
            new(Key.O, C, Cmd.OpenFile, "파일 열기"),
            new(Key.O, C | S, Cmd.OpenFolder, "폴더 열기"),
            new(Key.F5, N, Cmd.Reload, "새로고침"),
            new(Key.C, C, Cmd.CopyClipboard, "이미지 복사(클립보드)"),
            new(Key.F7, N, Cmd.CopyToFolder, "다른 폴더로 복사"),
            new(Key.F8, N, Cmd.MoveToFolder, "다른 폴더로 이동"),
            new(Key.F2, N, Cmd.Rename, "이름 바꾸기"),
            new(Key.Delete, N, Cmd.Delete, "휴지통으로 삭제"),
            new(Key.S, C, Cmd.SaveAs, "다른 형식·크기로 저장"),
            new(Key.E, C, Cmd.OpenLocation, "탐색기에서 위치 열기"),
            new(Key.F4, N, Cmd.Settings, "설정"),
            new(Key.F1, N, Cmd.Help, "단축키 도움말"),
        }),
    };

    public static IEnumerable<KeyBind> All => Groups.SelectMany(g => g.Binds);

    public static KeyBind Find(Key key, ModifierKeys mods) =>
        All.FirstOrDefault(b => b.Key == key && b.Mods == mods);

    /// <summary>메뉴 옆에 보여 줄 대표 단축키.</summary>
    public static string GestureOf(Cmd cmd, int arg = 0) =>
        All.FirstOrDefault(b => b.Cmd == cmd && (arg == 0 || b.Arg == arg))?.KeyText ?? "";
}
