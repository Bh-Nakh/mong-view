using System.Runtime.InteropServices;

namespace NoAdView.Core;

public enum SortKey { Name, Modified, Size }

/// <summary>탐색기와 같은 자연 정렬("2.jpg" 가 "10.jpg" 앞).</summary>
public static class NaturalSort
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    static extern int StrCmpLogicalW(string a, string b);

    public static int Compare(string a, string b) => StrCmpLogicalW(a ?? "", b ?? "");

    public static readonly IComparer<string> Comparer = Comparer<string>.Create(Compare);

    public static void Sort(List<PageEntry> pages, SortKey key, bool descending)
    {
        Comparison<PageEntry> cmp = key switch
        {
            SortKey.Modified => (a, b) => Tie(a.Modified.CompareTo(b.Modified), a, b),
            SortKey.Size => (a, b) => Tie(a.Size.CompareTo(b.Size), a, b),
            _ => (a, b) => Compare(a.Name, b.Name),
        };
        pages.Sort(descending ? (a, b) => cmp(b, a) : cmp);
    }

    static int Tie(int c, PageEntry a, PageEntry b) => c != 0 ? c : Compare(a.Name, b.Name);
}
