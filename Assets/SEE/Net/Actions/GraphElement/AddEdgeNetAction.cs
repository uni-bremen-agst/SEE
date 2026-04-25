using SEE.GraphElementRefs;
using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Creates a new edge through the network on each client.
    /// </summary>
    public class AddEdgeNetAction : EdgeNetAction
    {
        /// <summary>
        /// The unique id of the edge. May be empty or null, in which case a random
        /// unique ID will be created on the client side.
        /// </summary>
        public string EdgeType;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fromId">The id of the gameObject from which the edge should be drawn.</param>
        /// <param name="toId">The id of the gameObject to which the edge should be drawn.</param>
        /// <param name="edgeType">The type of the edge.</param>
        public AddEdgeNetAction(string fromId, string toId, string edgeType) : base(fromId, toId)
        {
            EdgeType = edgeType;
        }

        /// <summary>
        /// Creates the new edge on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameEdgeAdder.Add(Find(GraphElementID), GraphElementIDMap.Find(TargetGameNodeId), EdgeType);
        }
    }
}
