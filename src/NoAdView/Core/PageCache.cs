namespace NoAdView.Core;

/// <summary>
/// 디코딩 결과 캐시. 메모리 상한을 넘으면 오래 안 쓴 장부터 버린다.
/// 같은 장을 동시에 요청하면 하나의 작업을 공유한다.
/// </summary>
public sealed class PageCache
{
    readonly object gate = new();
    readonly Dictionary<int, Node> map = new();
    readonly LinkedList<int> lru = new(); // 앞쪽이 최근
    readonly SemaphoreSlim workers = new(3);
    long bytes;

    IPageSource source;
    int generation;

    public long LimitBytes { get; set; } = 768L << 20;
    public bool AutoRotate { get; set; } = true;

    sealed class Node
    {
        public Task<LoadedImage> Task;
        public LinkedListNode<int> Lru;
        public long Bytes;
    }

    /// <summary>원본이 바뀌면 캐시를 비운다.</summary>
    public void Reset(IPageSource src)
    {
        lock (gate)
        {
            source = src;
            generation++;
            map.Clear();
            lru.Clear();
            bytes = 0;
        }
    }

    public Task<LoadedImage> Get(int index, ICollection<int> keep = null)
    {
        lock (gate)
        {
            if (source == null || index < 0 || index >= source.Pages.Count)
                return Task.FromResult(new LoadedImage { Error = "없는 장" });
            if (map.TryGetValue(index, out var node))
            {
                lru.Remove(node.Lru);
                lru.AddFirst(node.Lru);
                return node.Task;
            }
            var page = source.Pages[index];
            int gen = generation;
            bool rotate = AutoRotate;
            node = new Node { Lru = lru.AddFirst(index) };
            node.Task = Task.Run(async () =>
            {
                await workers.WaitAsync().ConfigureAwait(false);
                try { return ImageLoader.Load(page, rotate); }
                finally { workers.Release(); }
            });
            map[index] = node;
            node.Task.ContinueWith(t =>
            {
                lock (gate)
                {
                    if (gen != generation || !map.TryGetValue(index, out var n) || n != node) return;
                    n.Bytes = t.Result.Bytes;
                    bytes += n.Bytes;
                    Trim(keep);
                }
            }, TaskScheduler.Default);
            return node.Task;
        }
    }

    /// <summary>미리 읽기. 결과를 기다리지 않는다.</summary>
    public void Prefetch(IEnumerable<int> indices, ICollection<int> keep)
    {
        foreach (int i in indices) _ = Get(i, keep);
    }

    /// <summary>특정 장을 캐시에서 지운다(파일이 바뀌었을 때).</summary>
    public void Invalidate(int index)
    {
        lock (gate)
        {
            if (!map.Remove(index, out var n)) return;
            lru.Remove(n.Lru);
            bytes -= n.Bytes;
        }
    }

    void Trim(ICollection<int> keep)
    {
        var cur = lru.Last;
        while (bytes > LimitBytes && cur != null)
        {
            var prev = cur.Previous;
            int idx = cur.Value;
            if ((keep == null || !keep.Contains(idx)) && map.TryGetValue(idx, out var n) && n.Task.IsCompleted)
            {
                map.Remove(idx);
                lru.Remove(cur);
                bytes -= n.Bytes;
            }
            cur = prev;
        }
    }
}
