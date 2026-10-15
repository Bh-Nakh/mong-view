using System.Runtime.InteropServices;

namespace NoAdView.Core;

/// <summary>휴지통 삭제·복사·이동·이름 바꾸기·바탕화면 지정.</summary>
public static class FileOps
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHFileOperation(ref SHFILEOPSTRUCT op);

    const uint FO_DELETE = 3;
    const ushort FOF_SILENT = 0x4, FOF_NOCONFIRMATION = 0x10, FOF_ALLOWUNDO = 0x40, FOF_NOERRORUI = 0x400;

    public static void Recycle(string path, IntPtr owner)
    {
        var op = new SHFILEOPSTRUCT
        {
            hwnd = owner,
            wFunc = FO_DELETE,
            pFrom = path + "\0\0",
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI,
        };
        int r = SHFileOperation(ref op);
        if (r != 0 || op.fAnyOperationsAborted)
            throw new IOException($"휴지통으로 보내지 못했습니다 (코드 {r}).");
    }

    public static string Rename(string path, string newName)
    {
        newName = newName.Trim();
        if (newName.Length == 0 || newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("파일 이름에 쓸 수 없는 문자가 있습니다.");
        string target = Path.Combine(Path.GetDirectoryName(path), newName);
        if (string.Equals(target, path, StringComparison.Ordinal)) return path;
        if (File.Exists(target) && !string.Equals(target, path, StringComparison.OrdinalIgnoreCase))
            throw new IOException("같은 이름의 파일이 이미 있습니다.");
        File.Move(path, target);
        return target;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SystemParametersInfo(uint action, uint param, string vparam, uint winIni);

    public static void SetWallpaper(string imagePath)
    {
        const uint SPI_SETDESKWALLPAPER = 20, SPIF_UPDATEINIFILE = 1, SPIF_SENDCHANGE = 2;
        if (!SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
            throw new IOException("바탕화면을 바꾸지 못했습니다 (코드 " + Marshal.GetLastWin32Error() + ").");
    }
}
