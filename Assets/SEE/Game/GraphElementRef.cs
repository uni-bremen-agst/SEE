using SEE.DataModel.DG;
using System;
using Sirenix.OdinInspector;

namespace SEE.GO
{
    /// <summary>
    /// A reference to a graph element that can be attached to a game object as a component.
    /// Abstract superclass of NodeRef and EdgeRef.
    /// </summary>
    public abstract class GraphElementRef : SerializedMonoBehaviour
    {
        [NonSerialized] public GraphElement elem;
    }
}