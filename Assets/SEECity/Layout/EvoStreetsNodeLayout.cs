using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class EvoStreetsNodeLayout : NodeLayout
    {
        public EvoStreetsNodeLayout(float groundLevel, NodeFactory leafNodeFactory) 
            : base(groundLevel, leafNodeFactory)
        {
        }

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            /// The node layout we compute as a result.
            Dictionary<GameObject, NodeTransform> layout_result = new Dictionary<GameObject, NodeTransform>();
            if (gameNodes.Count == 0)
            {
                throw new Exception("No nodes to be laid out.");
            }
            else if (gameNodes.Count == 1)
            {
                GameObject gameNode = gameNodes.GetEnumerator().Current;
                layout_result[gameNode] = new NodeTransform(Vector3.zero,
                                                            new Vector3(width, gameNode.transform.localScale.y, depth));
            }
            else
            {
                to_game_node = NodeMapping(gameNodes);
                CreateTree(gameNodes);
                CalculateSize();
                CalculateLayout();
            }
            return layout_result;
        }
    }
}
