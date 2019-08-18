using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using System;

namespace SEE
{
    /// <summary>
    /// Creates a balloon layout according to "Reconfigurable Disc Trees for Visualizing 
    /// Large Hierarchical Information Space" by Chang-Sung Jeong and Alex Pang.
    /// Published in: Proceeding INFOVIS '98 Proceedings of the 1998 IEEE Symposium on 
    /// Information Visualization, Pages 19-25.
    /// </summary>
    public class BalloonLayout : ILayout
    {
        public void Draw(ISceneGraph graph)
        {
            DrawNodes(graph);
        }

        private void DrawNodes(ISceneGraph graph)
        {
            List<INode> roots = graph.GetRoots();
            foreach (INode node in roots)
            {

            }
            //CalculateRadius2D()
        }
    }
}