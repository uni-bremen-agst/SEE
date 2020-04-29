using OdinSerializer;
using System;

using SEE.DataModel;

namespace SEE.GO
{
    /// <summary>
    /// A reference to a graph edge that can be attached to a game object as a component.
    /// </summary>
    public class EdgeRef : SerializedMonoBehaviour
    {
        [NonSerialized, OdinSerialize]
        public Edge edge; // serialized by Odin only
    }
}
