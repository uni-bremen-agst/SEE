using OdinSerializer;
using System;

using SEE.DataModel;

namespace SEE.GO
{
    /// <summary>
    /// A reference to a graph node that can be attached to a game object as a component.
    /// </summary>
    public class NodeRef : SerializedMonoBehaviour
    {
        [NonSerialized, OdinSerialize]
        public Node node; // serialized by Odin only
    }
}
