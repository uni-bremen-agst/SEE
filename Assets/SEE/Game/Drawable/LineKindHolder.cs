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
    public class LineKindHolder : MonoBehaviour
    {
        /// <summary>
        /// The holded line kind of the line.
        /// </summary>
        private GameDrawer.LineKind lineKind;

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
        /// <returns></returns>
        public GameDrawer.LineKind GetLineKind()
        {
            return lineKind;
        }
    }
}