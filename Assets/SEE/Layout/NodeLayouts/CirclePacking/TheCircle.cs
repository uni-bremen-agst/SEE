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

        /// <summary>
        /// Initializes a new instance of the TheCircle class with the specified game object, center position, and radius.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
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


        /// <summary>
        /// Returns a string representation of the circle, including its ID, center position, and radius.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "(ID=" + ID + " center= " + Center.ToString() + ", radius=" + Radius + ")";
        }
    }
}