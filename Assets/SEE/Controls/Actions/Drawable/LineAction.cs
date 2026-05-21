using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This abstract class is the base for all actions that work with lines.
    /// It provides the basic functionality for all line actions.
    /// </summary>
    public abstract class LineAction : DrawableAction
    {
        /// <summary>
        /// True if the action is active.
        /// </summary>
        protected bool isActive = false;
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        protected Memento memento;

        /// <summary>
        /// This struct can store all the information needed to
        /// revert or repeat a <see cref="LineAction"/>.
        /// </summary>
        protected class Memento
        {
            /// <summary>
            /// Is the configuration of line before it was split.
            /// </summary>
            public readonly LineConf OriginalLine;
            /// <summary>
            /// Is the drawable surface on which the lines are displayed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The list of lines that resulted from splitting the original line.
            /// </summary>
            public readonly List<LineConf> Lines;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="originalLine">Is the configuration of line before it was split.</param>
            /// <param name="surface">The drawable surface where the lines are displayed.</param>
            /// <param name="lines">The list of lines that resulted from splitting the original line.</param>
            public Memento(GameObject originalLine, GameObject surface, List<LineConf> lines)
            {
                OriginalLine = LineConf.GetLine(originalLine);
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                Lines = lines;
            }
        }
    }
}
