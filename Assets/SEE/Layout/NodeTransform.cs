using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// The position, scaling, and rotation of a node game object as determined by a node layout.
    /// </summary>
    public class NodeTransform
    {
        /// <summary>
        /// Constructor setting the position and scale. The rotation will be 0 degrees.
        /// The y co-ordinate will be calculated such that the ground of the object is 0.
        /// </summary>
        /// <param name="x">worldspace x center position</param>
        /// <param name="z">worldspace z center position</param>
        /// <param name="scale">the absolute scale of the object</param>
        public NodeTransform(float x, float z, Vector3 scale)
        {
            centerPosition = new Vector3(x, scale.y, z);
            Scale = scale;
            Rotation = 0.0f;
        }

        /// <summary>
        /// Constructor setting the position, scale, and rotation.
        /// The y co-ordinate will be calculated such that the ground of the object is 0.
        /// </summary>
        /// <param name="x">worldspace x center position</param>
        /// <param name="z">worldspace z center position</param>
        /// <param name="scale">the absolute scale of the object</param>
        /// <param name="rotation">rotation of the object around the y axis in degree</param>
        public NodeTransform(float x, float z, Vector3 scale, float rotation)
        {
            centerPosition = new Vector3(x, scale.y, z);
            Scale = scale;
            Rotation = rotation;
        }

        /// <summary>
        /// Constructor setting the position, scale, and rotation.
        /// The position will be exactly as given by <paramref name="position"/>,
        /// that is, no adjustment will be made to the y co-ordinate.
        /// The rotation will be 0 degrees.
        /// </summary>
        /// <param name="position">worldspace center position</param>
        /// <param name="scale">the absolute scale of the object</param>
        public NodeTransform(Vector3 position, Vector3 scale)
        {
            centerPosition = position;
            Scale = scale;
            Rotation = 0.0f;
        }

        /// <summary>
        /// Constructor setting the position, scale, and rotation.
        /// The position will be exactly as given by <paramref name="position"/>,
        /// that is, no adjustment will be made to the y co-ordinate.
        /// The rotation will be <paramref name="rotation"/> degrees.
        /// </summary>
        /// <param name="position">worldspace center position</param>
        /// <param name="scale">the absolute scale of the object</param>
        /// <param name="rotation">rotation of the object around the y axis in degree</param>
        public NodeTransform(Vector3 position, Vector3 scale, float rotation)
        {
            centerPosition = position;
            Scale = scale;
            Rotation = rotation;
        }

        /// <summary>
        /// The worldspace position along the x axis.
        /// </summary>
        public float X => centerPosition.x;
        /// <summary>
        /// The worldspace position along the z axis.
        /// </summary>
        public float Z => centerPosition.z;

        /// <summary>
        /// The absolute scale (width, height, depth) of the game object.
        /// </summary>
        public Vector3 Scale;
        /// <summary>
        /// The rotation of the object relative to the ground (spanned by x and z co-ordindate)
        /// in degree. In other words, the rotation is around the y axis.
        /// </summary>
        public float Rotation;

        /// <summary>
        /// The roof (y axis) of this node transform.
        /// </summary>
        public float Roof => centerPosition.y + Scale.y / 2.0f;

        /// <summary>
        /// The worldspace center position of the node.
        /// </summary>
        /// <remarks>We keep this a separate field declaration (not using a
        /// default property for <see cref="CenterPosition"/>) to be able
        /// to modify the co-ordinates individually.</remarks>
        private Vector3 centerPosition;

        /// <summary>
        /// The worldspace center position of the node.
        /// </summary>
        public Vector3 CenterPosition => centerPosition;

        /// <summary>
        /// Scales the width (x) and depth (z) by the given <paramref name="factor"/>.
        /// The height will be maintained.
        /// </summary>
        /// <param name="factor">factory by which to scale the width and depth of the node</param>
        public void ScaleXZBy(float factor)
        {
            Scale.x *= factor;
            Scale.z *= factor;
        }

        public void ScaleXZBy(float factor, Vector2 relativeTo)
        {
            Vector2 newPosition = relativeTo + factor * (new Vector2(X, Z) - relativeTo);
            centerPosition.x = newPosition.x;
            centerPosition.z = newPosition.y;
            ScaleXZBy(factor);
        }

        /// <summary>
        /// Translate (moves) this transform by given <paramref name="offset"/>.
        /// </summary>
        /// <param name="offset">the relative offset by which to move</param>
        internal void TranslateBy(Vector3 offset)
        {
            centerPosition += offset;
        }

        /// <summary>
        /// Lifts this transform along the y axis such that the ground of the object is at the
        /// given <paramref name="ground"/> level.
        /// </summary>
        /// <param name="ground">the absolute worldspace y co-ordindate of the ground where to lift</param>
        internal void LiftGroundTo(float ground)
        {
            centerPosition.y = ground + Scale.y / 2.0f;
        }

        /// <summary>
        /// Moves this transform to the given <paramref name="x"/> and <paramref name="z"/> co-ordinates.
        /// The y co-ordinate will be maintained.
        /// </summary>
        /// <param name="x">target x co-ordinate</param>
        /// <param name="z">target z co-ordinate</param>
        internal void MoveTo(float x, float z)
        {
            centerPosition.x = x;
            centerPosition.z = z;
        }

        /// <summary>
        /// Moves this transform by the given <paramref name="xOffset"/> along the
        /// x axis and <paramref name="zOffset"/> along the y axis. Equivalent to
        /// MoveTo(X + xOffset, Z + zOffset).
        /// </summary>
        /// <param name="xOffset">to be added to <see cref="X"/></param>
        /// <param name="zOffset">to be added to <see cref="Z"/></param>
        internal void MoveBy(float xOffset, float zOffset)
        {
            centerPosition.x += xOffset;
            centerPosition.z += zOffset;
        }

        /// <summary>
        /// Expands the scale of this transform by the given <paramref name="xOffset"/> along the
        /// x axis and by <paramref name="zOffset"/> along the y axis. Equivalent to
        /// Scale.x += xOffset and Scale.z += zOffset. The offsets can be negative, in
        /// which case the node is actually shrunk.
        /// </summary>
        /// <param name="xOffset">to be added to <see cref="Scale.x"/></param>
        /// <param name="zOffset">to be added to <see cref="Scale.z"/></param>
        internal void ExpandBy(float xOffset, float zOffset)
        {
            Scale.x += xOffset;
            Scale.z += zOffset;
        }

        /// <summary>
        /// Human readable string representation of this node transform for debugging.
        /// </summary>
        /// <returns>string representation of this node transform</returns>
        public override string ToString()
        {
            return "position=" + centerPosition.ToString() + " scale=" + Scale.ToString() + " rotation=" + Rotation;
        }
    }
}
