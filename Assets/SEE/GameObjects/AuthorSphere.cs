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
        /// The identity of the author.
        /// </summary>
        public FileAuthor Author;

        /// <summary>
        /// All edges connecting the author sphere to the files they contributed to.
        /// </summary>
        public IList<GameObject> Edges = new List<GameObject>();
    }
}
