
public class LinkedListEntry
{
    // TODO: DoubleLinkedList mit neuer Klasse data -> int kumulierteZeit, string text, data previous, data next

    private readonly string text;

    private readonly int cumulatedTime;

    private readonly int index;

    public LinkedListEntry(int index, string text, int cumulatedTime)
    {
        this.text = text;
        this.cumulatedTime = cumulatedTime;
        this.index = index;
    }

    public int CumulatedTime { get => cumulatedTime; }
    public string Text { get => text;}
    public int Index { get => index; }
}
