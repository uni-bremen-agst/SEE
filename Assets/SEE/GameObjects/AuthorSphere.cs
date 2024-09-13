using System.Collections.Generic;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// This component will be attached to all author spheres.
    /// </summary>
    public class AuthorSphere : MonoBehaviour
    {
        /// <summary>
        /// The E-Mail/Name of the author.
        /// </summary>
        public string Author = "";

        /// <summary>
        /// A List of all edges the author sphere has and the churn amount.
        /// </summary>
        public IList<(GameObject, int)> Edges = new List<(GameObject, int)>();
    }
}
