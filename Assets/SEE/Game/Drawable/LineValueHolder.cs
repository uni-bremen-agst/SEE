using SEE.Game;
using System;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class represents a line kind value holder component for the line gameobjects.
    /// </summary>
    public class LineValueHolder : MonoBehaviour
    {
        /// <summary>
        /// The holded line kind of the line.
        /// </summary>
        private GameDrawer.LineKind lineKind;

        /// <summary>
        /// The holded color kind of the line.
        /// </summary>
        private GameDrawer.ColorKind colorKind;

        /// <summary>
        /// Sets the given line kind.
        /// </summary>
        /// <param name="lineKind">The given line kind that should be set.</param>
        public void SetLineKind(GameDrawer.LineKind lineKind)
        {
            this.lineKind = lineKind;
        }

        /// <summary>
        /// Gets the current line kind of the line gameObject.
        /// </summary>
        /// <returns>the line kind</returns>
        public GameDrawer.LineKind GetLineKind()
        {
            return lineKind;
        }

        /// <summary>
        /// Sets the given color kind.
        /// </summary>
        /// <param name="colorKind">The given color kind that should be set.</param>
        public void SetColorKind(GameDrawer.ColorKind colorKind)
        {
            this.colorKind = colorKind;
        }

        /// <summary>
        /// Gets the current color kind of the line gameObject.
        /// </summary>
        /// <returns>the color kind</returns>
        public GameDrawer.ColorKind GetColorKind()
        {
            return colorKind;
        }
    }
}