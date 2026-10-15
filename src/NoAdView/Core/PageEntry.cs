namespace NoAdView.Core;

/// <summary>보기 대상 한 장. 폴더 파일이든 압축 안 항목이든 같은 모양으로 다룬다.</summary>
public sealed class PageEntry
{
    /// <summary>표시용 이름. 압축 안에서는 '/' 로 구분된 상대 경로.</summary>
    public string Name { get; init; }

    /// <summary>폴더 원본일 때 실제 파일 경로. 압축 안 항목이면 null.</summary>
    public string FilePath { get; init; }

    public long Size { get; init; }
    public DateTime Modified { get; init; }

    /// <summary>읽을 때마다 새 스트림을 연다(호출한 쪽이 닫는다).</summary>
    public Func<Stream> Open { get; init; }

    public bool IsFile => FilePath != null;

    public override string ToString() => Name;
}
