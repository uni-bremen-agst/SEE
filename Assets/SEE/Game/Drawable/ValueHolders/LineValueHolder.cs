using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// This class represents a value holder component for the line gameobjects.
    /// </summary>
    public class LineValueHolder : MonoBehaviour
    {
        /// <summary>
        /// The line kind property.
        /// </summary>
        public GameDrawer.LineKind LineKind { get; set; }

        /// <summary>
        /// The color kind property.
        /// </summary>
        public GameDrawer.ColorKind ColorKind { get; set; }
    }
}