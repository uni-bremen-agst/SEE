namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Super class of all net actions dealing with a single game node.
    /// </summary>
    public abstract class NodeNetAction : GraphElementNetAction
    {
        protected NodeNetAction(string gameNodeID) : base(gameNodeID)
        {
        }
    }
}
