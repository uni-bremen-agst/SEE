using System.Collections.Generic;
using SEE.GraphProviders.VCS;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Attributes of an author sphere.
    /// </summary>
    /// <remarks>This component will be attached to all author spheres.</remarks>
    public class AuthorSphere : MonoBehaviour
    {
        /// <summary>
        /// The E-Mail/Name of the author.
        /// </summary>
        public GitFileAuthor Author;

        /// <summary>
        /// The list of all edges the author sphere has and their associated churn.
        /// </summary>
        public IList<(GameObject, int)> Edges = new List<(GameObject, int)>();
    }
}
