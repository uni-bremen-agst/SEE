using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// The position, scaling, and rotation of a node game object as determined by a node layout.
    /// </summary>
    /// Note: Structs are value types and are copied on assignment. 
    public struct NodeTransform
    {
        public NodeTransform(Vector3 position, Vector3 scale)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = 0.0f;
        }

        public NodeTransform(Vector3 position, Vector3 scale, float rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

        /// <summary>
        /// The position in the scene.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The scale (width, height, depth) of the game object.
        /// </summary>
        public Vector3 scale;
        /// <summary>
        /// The rotation of the object relative to the ground (spanned by x and z co-ordindate) in degree.
        /// </summary>
        public float rotation;

        public override string ToString()
        {
            return "position=" + position.ToString() + " scale=" + scale.ToString();
        }
    }
}
