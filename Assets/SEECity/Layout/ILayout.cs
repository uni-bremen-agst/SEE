using System.Collections.Generic;
using UnityEngine;

namespace SEE
{
    public interface ILayout
    {
        void Draw(IGraph graph, Dictionary<string, GameObject> nodes, List<GameObject> edges);
    }
}

