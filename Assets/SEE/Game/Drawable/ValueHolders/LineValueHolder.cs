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

        /// <summary>
        /// True if this line was created by the freehand drawing action.
        /// Freehand lines do not support line caps because their first and last
        /// segments are often too short or unstable for reliable cap calculation.
        /// </summary>
        public bool FreehandLine { get; private set; }

        /// <summary>
        /// Whether this holder was already initialized.
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Initializes line-type-specific values.
        /// </summary>
        /// <param name="freehandLine">Whether this line is a freehand line.</param>
        public void Initialize(bool freehandLine)
        {
            if (isInitialized)
            {
                return;
            }

            FreehandLine = freehandLine;
            isInitialized = true;
        }
    }
}