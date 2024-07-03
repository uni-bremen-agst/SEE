using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.GameObjects
{
    public class AuthorRef : SerializedMonoBehaviour
    {
        public ISet<GameObject> AuthorSpheres = new HashSet<GameObject>();

        public IList<(GameObject, int)> Edges = new List<(GameObject, int)>();
    }
}
