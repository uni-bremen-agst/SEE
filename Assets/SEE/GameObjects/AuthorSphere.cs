using System.Collections.Generic;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// This component will be attached to all author spheres
    /// </summary>
    public class AuthorSphere : MonoBehaviour
    {
        public string Author = "";

        public IList<(GameObject, int)> Edges = new List<(GameObject, int)>();
    }
}
