using System.Collections.Generic;
using UnityEngine;

namespace SEE
{
    interface ILayout
    {
        void Draw(IGraph graph, Dictionary<string, GameObject> nodes, List<GameObject> edges);
    }
}

