using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// This class represents a line kind value holder component for the line gameobjects.
    /// </summary>
    public class LineValueHolder : MonoBehaviour
    {
        /// <summary>
        /// The held line kind of the line.
        /// </summary>
        private GameDrawer.LineKind lineKind;

        /// <summary>
        /// The line kind property.
        /// </summary>
        public GameDrawer.LineKind LineKind 
        { 
            get { return lineKind; }
            set { lineKind = value; }
        }

        /// <summary>
        /// The held color kind of the line.
        /// </summary>
        private GameDrawer.ColorKind colorKind;

        /// <summary>
        /// The color kind property.
        /// </summary>
        public GameDrawer.ColorKind ColorKind 
        { 
            get { return colorKind; } 
            set {  colorKind = value; } 
        }
    }
}