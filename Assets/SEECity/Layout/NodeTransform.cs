using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// The position and scaling of a node game object as determined by a node layout.
    /// </summary>
    /// Note: Structs are value types and are copied on assignment. 
    public struct NodeTransform
    {
        public NodeTransform(Vector3 position, Vector3 scale)
        {
            this.position = position;
            this.scale = scale;
        }
        public Vector3 position;
        public Vector3 scale;

        public override string ToString()
        {
            return "position=" + position.ToString() + " scale=" + scale.ToString();
        }
    }
}
