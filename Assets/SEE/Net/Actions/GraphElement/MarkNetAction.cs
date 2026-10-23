using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{

/// <summary>
/// Replicates the marking state of a game node to remote clients.
/// </summary>
internal class MarkNetAction : NodeNetAction
{
    public bool Marked;

    internal MarkNetAction(string gameNodeID, bool marked)
        : base(gameNodeID)
    {
        Marked = marked;
    }

    public override void ExecuteOnClient()
    {
        GameNodeMarker.SetMarked(Find(GraphElementID), Marked);
    }
}
}