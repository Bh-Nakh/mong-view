using System.Globalization;
using System.Windows;
using NoAdView.Core;

namespace NoAdView;

/// <summary>
/// 명령줄: 몽뷰.exe [파일|폴더|압축] [--data 설정폴더] [--pos x,y] [--size w,h] [--no-activate]
/// --data·--pos·--no-activate 는 시험용(사용자 설정을 건드리지 않고 2번 모니터에 포커스 없이 띄우기).
/// </summary>
public partial class App : Application
{
    public sealed class StartArgs
    {
        public string Path;
        public string DataDir = SettingsStore.DefaultDir;
        public Point? Pos;
        public Size? Size;
        public bool NoActivate;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, ev) =>
        {
            MessageBox.Show("예상하지 못한 오류가 났습니다.\n\n" + ev.Exception.Message, "몽뷰",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ev.Handled = true;
        };

        var args = Parse(e.Args);
        ArchiveSource.CleanStaleTemp();
        // 우클릭 메뉴 자동 등록(시험용 --data 실행에서는 사용자 레지스트리를 건드리지 않는다)
        if (args.DataDir == SettingsStore.DefaultDir)
        {
            try { FileAssoc.EnsureContextMenu(); } catch { }
            // 웹페이지 설치 확인용 로컬 핑 서버 (시험 실행에서는 켜지 않음)
            MongWebCheck.WebCheck.Start("mongview", "몽뷰", new int[] { 47141, 47142, 47143 });
        }
        var win = new MainWindow(args);
        MainWindow = win;
        win.Show();
    }

    static StartArgs Parse(string[] argv)
    {
        var a = new StartArgs();
        for (int i = 0; i < argv.Length; i++)
        {
            string s = argv[i];
            string Next() => i + 1 < argv.Length ? argv[++i] : null;
            switch (s.ToLowerInvariant())
            {
                case "--data": a.DataDir = Next() ?? a.DataDir; break;
                case "--pos": if (TryPair(Next(), out var p)) a.Pos = new Point(p.x, p.y); break;
                case "--size": if (TryPair(Next(), out var z)) a.Size = new Size(z.x, z.y); break;
                case "--no-activate": a.NoActivate = true; break;
                default: if (!s.StartsWith("--")) a.Path ??= s; break;
            }
        }
        return a;
    }

    static bool TryPair(string s, out (double x, double y) v)
    {
        v = default;
        var parts = s?.Split(',');
        if (parts?.Length != 2) return false;
        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out v.x)) return false;
        return double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out v.y);
    }
}
