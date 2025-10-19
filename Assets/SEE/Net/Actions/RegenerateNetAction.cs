using SEE.Game.City;
using SEE.Utils;
using System.Collections.Generic;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class regenerates objects previously deleted at all clients
    /// in the network.
    /// </summary>
    public abstract class RegenerateNetAction : AbstractNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// A serialization of the map of the node types to be restored.
        /// The type is assumed to be Dictionary of string (key) and
        /// VisualNodeAttributes (value).
        /// </summary>
        public string NodeTypeList;

        /// <summary>
        /// Constructor. Sets <see cref="NodeTypeList"/> to the serialization
        /// of the given <paramref name="nodeTypes"/>.
        /// </summary>
        public RegenerateNetAction(Dictionary<string, VisualNodeAttributes> nodeTypes)
        {
            NodeTypeList = nodeTypes != null && nodeTypes.Count > 0 ?
                NodeTypesSerializer.Serialize(nodeTypes) : "";
        }

        /// <summary>
        /// Returns <see cref="NodeTypeList"/> as a dictionary.
        /// </summary>
        /// <returns>Unserialized <see cref="NodeTypeList"/></returns>
        protected Dictionary<string, VisualNodeAttributes> ToMap()
        {
            return string.IsNullOrEmpty(NodeTypeList) ?
                new() : NodeTypesSerializer.Unserialize(NodeTypeList);
        }
    }
}
