using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Holds the connections of a file all its authors.
    /// This component will be attached to all file game nodes.
    /// </summary>
    public class AuthorRef : SerializedMonoBehaviour
    {
        /// <summary>
        /// The edges to the authors of this specific file.
        /// </summary>
        public IList<GameObject> Edges = new List<GameObject>();
    }
}
