using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE
{
    /// <summary>
    /// Creates a balloon layout according to "Reconfigurable Disc Trees for Visualizing 
    /// Large Hierarchical Information Space" by Chang-Sung Jeong and Alex Pang.
    /// Published in: Proceeding INFOVIS '98 Proceedings of the 1998 IEEE Symposium on 
    /// Information Visualization, Pages 19-25.
    /// </summary>
    class BalloonLayout : ILayout
    {
        void ILayout.Draw(IGraph graph, Dictionary<string, GameObject> nodes, List<GameObject> edges)
        {
            CreateNodes(graph, nodes);
        }

        private void CreateNodes(IGraph graph, Dictionary<string, GameObject> nodes)
        {
            List<INode> roots = graph.GetRoots();
            foreach (INode node in roots)
            {

            }
            //CalculateRadius2D()
        }
    }
}