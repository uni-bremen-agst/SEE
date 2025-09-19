using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Reference to all author spheres and edges of a file game node.
    /// This component will be attached to all file game nodes.
    /// </summary>
    public class AuthorRef : SerializedMonoBehaviour
    {
        /// <summary>
        /// A list of author-sphere game objects which contributed to that file.
        /// </summary>
        public ISet<GameObject> AuthorSpheres = new HashSet<GameObject>();

        /// <summary>
        /// A list of tuples of edges to author spheres and the churn of the author of this specific file.
        /// </summary>
        public IList<(GameObject, int)> Edges = new List<(GameObject, int)>();
    }
}
