using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.CirclePacking
{
    /// <summary>
    /// Represents a circle in a 2D space, defined by its center position, radius, and associated game object.
    /// </summary>
    public class TheCircle
    {
        /// <summary>
        /// The center of the circle represented as a Vector2,
        /// where x is the horizontal position and y is the vertical position.
        /// </summary>
        public Vector2 Center;

        /// <summary>
        /// Gets or sets the X coordinate of the circle's center.
        /// </summary>
        public float X
        {
            get => Center.x;
            set => Center.x = value;
        }

        /// <summary>
        /// Gets or sets the Y coordinate of the circle's center.
        /// </summary>
        public float Y
        {
            get => Center.y;
            set => Center.y = value;
        }

        /// <summary>
        /// The radius of the circle.
        /// </summary>
        public float Radius;

        /// <summary>
        /// The associated game object for this circle, which implements the ILayoutNode interface.
        /// </summary>
        public ILayoutNode GameObject;

        /// <summary>
        /// The unique identifier for the circle, typically derived from the associated game object's ID.
        /// </summary>
        public string ID;
        /// <summary>
        /// Indicates whether the circle has been placed in the layout.
        /// </summary>
        public bool IsPlaced { get; set; }
        /// <summary>
        /// Gets or sets the previous X coordinate of the circle's center,
        /// used for tracking movement during layout calculations.
        /// </summary>
        public float PrevX { get; set; }

        /// <summary>
        /// Gets or sets the previous Y coordinate of the circle's center,
        /// used for tracking movement during layout calculations.
        /// </summary>
        public float PrevY { get; set; }

        /// <summary>
        /// Gets or sets the next center position of the circle,
        /// which can be used for predicting or planning the next layout step.
        /// </summary>
        public Vector2 nextCenter { get; set; }

        /// <summary>
        /// Gets or sets the next radius of the circle,
        /// which can be used for predicting or planning the next layout step.
        /// </summary>
        public float nextRadius { get; set; }

        /// <summary> Initializes a new instance of the <see cref="TheCircle"/> class
        /// with the provided layout node, center position and radius.</summary>
        /// <param name="gameObject">The associated layout node (may be null).
        /// If non-null, the circle's <see cref="ID"/> is taken from <c>gameObject.ID</c>.</param>
        /// <param name="center">The center position of the circle (X/Y coordinates are copied into
        /// <see cref="X"/> and <see cref="Y"/>).</param>
        /// <param name="radius">The radius of the circle. The constructor does not validate this
        /// value (e.g. non-negative) — callers should ensure it is appropriate.</param>
        /// <remarks> The constructor sets <see cref="GameObject"/>, <see cref="Center"/>,
        /// <see cref="Radius"/>, and copies the center coordinates into the <see cref="X"/>
        /// and <see cref="Y"/> properties. The <see cref="IsPlaced"/> flag is initialized to
        /// <c>false</c>.</remarks>
        public TheCircle(ILayoutNode gameObject, Vector2 center, float radius)
        {
            this.GameObject = gameObject;
            this.Center = center;
            this.Radius = radius;
            ID = gameObject != null ? gameObject.ID : null;
            X = center.x;
            Y = center.y;
            IsPlaced = false;
        }
        /// <summary> Returns a concise, human-readable representation of this circle
        /// including its identifier, center position and radius.</summary>
        /// <returns> A string formatted as "(ID={ID} center={Center}, radius={Radius})".
        /// If <see cref="ID"/> is null, "ID=null" will appear. The center uses
        /// <see cref="UnityEngine.Vector2.ToString()"/> formatting.</returns>
        /// <remarks> Intended for debugging and logging; callers should not rely on the
        /// exact string format for parsing.</remarks>
        public override string ToString()
        {
            return "(ID=" + ID + " center= " + Center.ToString() + ", radius=" + Radius + ")";
        }
    }
}
