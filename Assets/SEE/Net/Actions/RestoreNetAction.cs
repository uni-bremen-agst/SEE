using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using System.Collections.Generic;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class re-creates nodes and/or edges previously deleted at all clients
    /// in the network.
    ///
    /// Precondition: The objects to be re-created were in fact previously deleted.
    /// </summary>
    /// <remarks>If the objects to be re-created were actually only marked as deleted,
    ///  use <see cref="ReviveNetAction"/> instead.</remarks>
    public class RestoreNetAction : RegenerateNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// A serialization of the map of the graph elements
        /// and their corresponding layouts, if applicable.
        /// The type is assumed to be a list of <see cref="RestoreGraphElement"/>.
        /// </summary>
        public string NodesOrEdges;

        /// <summary>
        /// The constructor.
        /// It serializes the maps into strings.
        /// </summary>
        /// <param name="nodesOrEdges">The deleted graph elements and their corresponding
        /// layouts, if applicable.</param>
        /// <param name="nodeTypes">The deleted node types.</param>
        public RestoreNetAction
            (List<RestoreGraphElement> nodesOrEdges,
             Dictionary<string, VisualNodeAttributes> nodeTypes)
            : base(nodeTypes)
        {
            NodesOrEdges = RestoreGraphElementListSerializer.Serialize(nodesOrEdges);
        }

        /// <summary>
        /// Re-creates the nodes and/or edges at each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameElementDeleter.Restore(RestoreGraphElementListSerializer.Unserialize(NodesOrEdges),
                                       ToMap());
        }
    }
}
