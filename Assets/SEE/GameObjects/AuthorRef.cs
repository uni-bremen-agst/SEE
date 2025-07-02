using System.Collections.Generic;
using SEE.GO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Reference to all author spheres and edges of a node.
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
