using SEE.DataModel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class CirclePackingNodeLayout : NodeLayout
    {
        public CirclePackingNodeLayout(float groundLevel, BlockFactory blockFactory) 
            : base(groundLevel, blockFactory)
        {
            name = "CirclePackingNode"; // FIXME: change to "CirclePacking".
        }

        Dictionary<GameObject, NodeTransform> layout;

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            layout = new Dictionary<GameObject, NodeTransform>();

            List<Node> roots = GetRoots(gameNodes);
            if (roots.Count == 0)
            {
                throw new System.Exception("Graph has no root nodes.");
            }
            to_game_node = NodeMapping(gameNodes);
            GameObject artificialRootNode = new GameObject("Nodes");
            artificialRootNode.tag = Tags.Node;
            DrawNodes(artificialRootNode, roots, out float out_radius);

            return layout;
        }

        private void DrawNodes(GameObject parent, List<Node> children, out float out_radius)
        {
            List<Circle> circles = new List<Circle>(children.Count);

            int i = 0;
            foreach (Node child in children)
            {
                GameObject childObject = to_game_node[child];

                float radius;
                if (child.IsLeaf())
                {
                    DrawLeaf(childObject, out float out_leaf_radius);
                    radius = out_leaf_radius;
                }
                else
                {
                    DrawNodes(childObject, child.Children(), out float out_nodes_radius);
                    radius = out_nodes_radius;
                }

                childObject.transform.parent = parent.transform;

                float radians = ((float)i / (float)children.Count) * (2.0f * Mathf.PI);
                childObject.transform.localPosition = new Vector3(Mathf.Cos(radians), 0.0f, Mathf.Sin(radians)) * radius;
                childObject.transform.position = childObject.transform.position + new Vector3(0.0f, 0.1f, 0.0f);
                circles.Add(new Circle(childObject.transform, radius));
                i++;
            }

            Vector3 position = parent.transform.position;
            parent.transform.position = position;

            CirclePacker.Pack(circles, out float out_outer_radius);
            if (circles.Count > 1)
            {
                DrawOutline(parent, ref out_outer_radius);
            }
            out_radius = out_outer_radius;
        }

        private void DrawOutline(GameObject parent, ref float out_outer_radius)
        {
            throw new NotImplementedException();
        }

        private void DrawLeaf(GameObject block, out float out_leaf_radius)
        {
            Vector3 size = blockFactory.GetSize(block);
            // FIXME: This is the local position
            layout[block] = new NodeTransform(new Vector3(0.0f, size.y / 2.0f, 0.0f), Vector3.zero);
            out_leaf_radius = Mathf.Sqrt(size.x * size.x + size.z * size.z);
        }
    }
}
