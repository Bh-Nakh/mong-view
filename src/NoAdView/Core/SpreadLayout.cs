namespace NoAdView.Core;

/// <summary>
/// 한 화면에 보일 장 묶음(뷰)을 계산한다.
/// 1장: [0][1][2]…, 2장: 표지 단독이면 [0][1,2][3,4]…, 아니면 [0,1][2,3]…, 4장: [0..3][4..7]…
/// 각 뷰의 장 번호는 읽는 순서대로 들어 있다(화면 배치 순서는 Arrange 가 정한다).
/// </summary>
public static class SpreadLayout
{
    public static List<int[]> Build(int count, int perView, bool coverSingle)
    {
        var views = new List<int[]>();
        int i = 0;
        if (perView == 2 && coverSingle && count > 0)
        {
            views.Add(new[] { 0 });
            i = 1;
        }
        while (i < count)
        {
            int n = Math.Min(perView, count - i);
            views.Add(Enumerable.Range(i, n).ToArray());
            i += n;
        }
        return views;
    }

    public static int ViewOf(List<int[]> views, int page)
    {
        for (int v = 0; v < views.Count; v++)
            if (Array.IndexOf(views[v], page) >= 0) return v;
        return 0;
    }

    /// <summary>
    /// 읽는 순서의 장들을 화면 칸 순서(왼→오, 위→아래)로 바꾼다.
    /// 오른쪽→왼쪽이면 각 줄 안의 순서를 뒤집는다. 2장 보기에서 마지막 한 장만 남으면 그대로 한 칸.
    /// </summary>
    public static int[] Arrange(int[] view, int perView, bool rightToLeft)
    {
        if (!rightToLeft || view.Length < 2) return view;
        int cols = perView == 1 ? 1 : 2;
        var result = new int[view.Length];
        for (int row = 0; row * cols < view.Length; row++)
        {
            int start = row * cols;
            int len = Math.Min(cols, view.Length - start);
            for (int k = 0; k < len; k++) result[start + k] = view[start + len - 1 - k];
        }
        return result;
    }
}
