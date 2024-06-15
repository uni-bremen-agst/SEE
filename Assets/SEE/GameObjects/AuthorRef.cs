using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.GameObjects
{
    public class AuthorRef : SerializedMonoBehaviour
    {
        public GameObject AuthorSphere;

        public IList<GameObject> Edges = new List<GameObject>();
    }
}
