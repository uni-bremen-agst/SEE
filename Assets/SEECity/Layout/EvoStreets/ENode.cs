using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.EvoStreets
{
    [System.Serializable]
    public class ENode
    {
        public Vector3 Scale;

        public SEE.DataModel.Node GraphNode;

        public float XPivot;

        public float YPivot;

        public float RotationZ;

        public Vector3 Location;

        public int Depth;

        public bool Left;

        public ENode ParentNode;

        public List<ENode> Children = new List<ENode>();

        public bool IsOverForest;

        //public bool IsHouse => Children.Count == 0;
        public bool IsHouse()
        {
            return GraphNode.IsLeaf();
        }

        //public bool IsStreet => Children.Count > 0;
        public bool IsStreet()
        {
            return ! GraphNode.IsLeaf();
        }

        public float MaxChildZ
        {
            get
            {
                float max = 0;
                foreach (var child in Children)
                {
                    if (child.Scale.z > max) max = child.Scale.z;
                }

                return max;
            }
        }

        public override string ToString()
        {
            var nodeType = GraphNode != null ? GraphNode.Type : "NoType";
            return $"Node[Linkname={GraphNode.LinkName},Type={nodeType}]";
        }
    }
}
