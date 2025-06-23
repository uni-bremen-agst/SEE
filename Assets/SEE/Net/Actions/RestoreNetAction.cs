using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using System.Collections.Generic;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class propagates an undo of <see cref="DeleteAction"/> to all clients
    /// in the network, that is, it restores game objects representing a node
    /// or edge previously deleted.
    /// </summary>
    public class RestoreNetAction : AbstractNetAction
    {
        /// <summary>
        /// A serialization of the map of the graph elements
        /// and their corresponding layouts, if applicable.
        /// </summary>
        public string NodesOrEdges;

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
        public RestoreNetAction
            (List<RestoreGraphElement> nodesOrEdges,
             Dictionary<string, VisualNodeAttributes> nodeTypes)
        {
            NodesOrEdges = RestoreGraphElementListSerializer.Serialize(nodesOrEdges);
            NodeTypeList = nodeTypes != null && nodeTypes.Count > 0 ?
                NodeTypesSerializer.Serialize(nodeTypes) : "";
        }

        /// <summary>
        /// Restores the nodes or edges on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Dictionary<string, VisualNodeAttributes> nodeTypes = !string.IsNullOrEmpty(NodeTypeList)?
                NodeTypesSerializer.Unserialize(NodeTypeList) : new();
            List<RestoreGraphElement> nodesOrEdges = RestoreGraphElementListSerializer.Unserialize(NodesOrEdges);
            GameElementDeleter.Restore(nodesOrEdges, nodeTypes);
        }
    }
}
