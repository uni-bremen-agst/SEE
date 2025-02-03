using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// The position, scaling, and rotation of a node game object as determined by a node layout.
    /// The y co-ordinate of the position will be interpreted as the ground position of
    /// the game object (unlike in Unity where it is the center height).
    /// </summary>
    /// Note: Structs are value types and are copied on assignment.
    public struct NodeTransform
    {
        /// <summary>
        /// Constructor setting the position and scale. The rotation will be 0 degrees.
        /// The y co-ordinate of the position will be interpreted as the ground position of
        /// the game object (unlike in Unity where it is the center height).
        /// </summary>
        /// <param name="position">worldspace position in the scene; y co-ordinate denotes the ground</param>
        /// <param name="scale">the absolute scale of the object</param>
        public NodeTransform(Vector3 position, Vector3 scale)
        {
            Position = position;
            Scale = scale;
            Rotation = 0.0f;
        }

        /// <summary>
        /// Constructor setting the position, scale, and rotation.
        /// The y co-ordinate of the position will be interpreted as the ground position of
        /// the game object (unlike in Unity where it is the center height). The x and z
        /// co-ordinate refer to the center of an object in the x/z plane (ground).
        /// </summary>
        /// <param name="position">worldspace position in the scene; y co-ordinate denotes the ground</param>
        /// <param name="scale">the absolute scale of the object</param>
        /// <param name="rotation">rotation of the object around the y axis in degree</param>
        public NodeTransform(Vector3 position, Vector3 scale, float rotation)
        {
            Position = position;
            Scale = scale;
            Rotation = rotation;
        }

        /// <summary>
        /// The worldspace position in the scene.
        ///
        /// IMPORTANT NOTE: The y co-ordinate will be interpreted as the ground position of
        /// the game object (unlike in Unity where it is the center height).  The x and z
        /// co-ordinate refer to the center of an object in the x/z plane (ground).
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// The absolute scale (width, height, depth) of the game object.
        /// </summary>
        public Vector3 Scale;
        /// <summary>
        /// The rotation of the object relative to the ground (spanned by x and z co-ordindate)
        /// in degree. In other words, the rotation is around the y axis.
        /// </summary>
        public float Rotation;

        public override string ToString()
        {
            return "position=" + Position.ToString() + " scale=" + Scale.ToString();
        }
    }
}
