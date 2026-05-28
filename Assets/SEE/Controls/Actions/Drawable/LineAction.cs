using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Handles the shared logic for line-based erase actions.
        /// </summary>
        /// <param name="hitObject">The selected line object or line cap hit by the raycast.</param>
        /// <param name="hitPoint">The world-space hit position used to determine the nearest line points.</param>
        /// <param name="splitAction">The split operation to execute for the selected line.
        /// This allows different erase actions to define their own splitting behavior.</param>
        /// <returns>A <see cref="Memento"/> containing all information required
        /// for undoing or redoing the erase action.</returns>
        protected Memento EraseSelectedLinePart(
            GameObject hitObject,
            Vector3 hitPoint,
            Action<GameObject, LineConf, List<int>, List<Vector3>, List<LineConf>> splitAction)
        {
            GameObject selectedLine = hitObject.CompareTag(Tags.LineCap)
                ? hitObject.transform.parent.gameObject
                : hitObject;

            LineConf originLine = LineConf.GetLine(selectedLine);
            List<LineConf> lines = new();

            List<int> matchedIndices;
            if (hitObject.CompareTag(Tags.LineCap))
            {
                bool startCapSelected = hitObject.name.StartsWith(ValueHolder.LineStartCapPrefix);
                int lastIndex = selectedLine.GetComponent<LineRenderer>().positionCount - 1;

                matchedIndices = new List<int>
                    {
                        startCapSelected ? 0 : lastIndex
                    };
            }
            else
            {
                NearestPoints.GetNearestPoints(selectedLine, hitPoint, out List<Vector3> _, out matchedIndices);
            }

            GameObject surface = GameFinder.GetDrawableSurface(selectedLine);
            List<Vector3> originalPositions = GameDrawer.GetOriginalLinePositions(selectedLine).ToList();

            splitAction(surface, originLine, matchedIndices, originalPositions, lines);

            Memento result = new(selectedLine, surface, lines);

            new EraseNetAction(result.Surface.ID, result.Surface.ParentID, result.OriginalLine.ID).Execute();
            Destroyer.Destroy(selectedLine);

            return result;
        }
    }
}
