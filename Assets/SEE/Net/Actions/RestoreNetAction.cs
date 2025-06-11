using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using System.Collections.Generic;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class propagates an undo of <see cref="DeleteAction"/> to all clients
    /// in the network, that is, it restores game objects representing a node
    /// or edge previously were deleted.
    /// </summary>
    public class RestoreNetAction : AbstractNetAction
    {
        /// <summary>
        /// A serialization of the map of the graph elements
        /// and their corresponding layouts, if applicable.
        /// </summary>
        public List<RestoreGraphElementHelper> NodesOrEdges;

        /// <summary>
        /// A serialization of the map of the node types to be restored.
        /// </summary>
        public string NodeTypeList;

        /// <summary>
        /// The constructor.
        /// It serializes the maps into strings.
        /// </summary>
        /// <param name="nodesOrEdges">The deleted graph elements and their corresponding layouts, if applicable.</param>
        /// <param name="nodeTypes">The deleted node types.</param>
        public RestoreNetAction(List<RestoreGraphElementHelper> nodesOrEdges,
            Dictionary<string, VisualNodeAttributes> nodeTypes)
        {
            NodesOrEdges = nodesOrEdges;
            NodeTypeList = nodeTypes != null && nodeTypes.Count > 0?
                NodeTypesSerializer.Serialize(nodeTypes) : "";
        }

        /// <summary>
        /// Restores the nodes or edges on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Dictionary<string, VisualNodeAttributes> nodeTypes = NodeTypesSerializer.Unserialize(NodeTypeList);
            GameElementDeleter.Restore(NodesOrEdges, nodeTypes);
        }
    }
}
