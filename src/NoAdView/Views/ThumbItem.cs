using System.ComponentModel;
using System.Windows.Media;

namespace NoAdView.Views;

/// <summary>썸네일 막대 한 칸. 화면에 처음 보일 때(Thumb 를 처음 읽을 때) 불러오기를 요청한다.</summary>
public sealed class ThumbItem : INotifyPropertyChanged
{
    readonly Action<ThumbItem> request;
    ImageSource thumb;
    bool requested;

    public ThumbItem(int index, string label, Action<ThumbItem> request)
    {
        Index = index;
        Label = label;
        this.request = request;
    }

    public int Index { get; }
    public string Label { get; }

    public ImageSource Thumb
    {
        get
        {
            if (!requested)
            {
                requested = true;
                request(this);
            }
            return thumb;
        }
        set
        {
            thumb = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Thumb)));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
