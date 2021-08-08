
public class LinkedListEntry
{
    // TODO: DoubleLinkedList mit neuer Klasse data -> int kumulierteZeit, string text, data previous, data next

    private readonly string text;

    private readonly int cumulatedTime;

    public LinkedListEntry(string text, int cumulatedTime)
    {
        this.text = text;
        this.cumulatedTime = cumulatedTime;
    }

    public int CumulatedTime { get => cumulatedTime; }
    public string Text { get => text;}
}
