namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Super class of all net actions dealing with a single game edge.
    /// </summary>
    public abstract class EdgeNetAction : GraphElementNetAction
    {
        /// <summary>
        /// The id of the gameObject to which the edge should be drawn (target node).
        /// </summary>
        public string TargetGameNodeId;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sourceGameNodeID">Unique id of the game object representing the source node of the edge.</param>
        /// <param name="targetGameNodeId">Unique id of the game object representing the target node of the edge.</param>
        protected EdgeNetAction(string sourceGameNodeID, string targetGameNodeId) : base(sourceGameNodeID)
        {
            TargetGameNodeId = targetGameNodeId;
        }
    }
}
