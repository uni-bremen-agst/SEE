using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Manages the segment and line-cap selection of the line menu.
    /// </summary>
    internal sealed class LineCapMenu
    {
        /// <summary>
        /// The editable segments of a line.
        /// </summary>
        private enum Segment
        {
            Main,
            StartCap,
            EndCap,
        }

        /// <summary>
        /// The label of the segment selector.
        /// </summary>
        private readonly GameObject segmentText;

        /// <summary>
        /// The segment selector object.
        /// </summary>
        private readonly GameObject segmentSelection;

        /// <summary>
        /// The label of the line-cap selector.
        /// </summary>
        private readonly GameObject lineCapText;

        /// <summary>
        /// The line-cap selector object.
        /// </summary>
        private readonly GameObject lineCapSelection;

        /// <summary>
        /// The selector for the line segments.
        /// </summary>
        private readonly HorizontalSelector segmentSelector;

        /// <summary>
        /// The selector for the line caps.
        /// </summary>
        private readonly HorizontalSelector lineCapSelector;

        /// <summary>
        /// The registered action for changing the selected segment.
        /// </summary>
        private UnityAction<int> segmentAction;

        /// <summary>
        /// The registered action for changing the selected line cap.
        /// </summary>
        private UnityAction<int> lineCapAction;

        /// <summary>
        /// The currently selected line segment.
        /// </summary>
        private Segment currentSegment = Segment.Main;

        /// <summary>
        /// Whether the main line segment is currently selected.
        /// </summary>
        internal bool IsMainSelected => currentSegment == Segment.Main;

        /// <summary>
        /// Whether the start cap segment is currently selected.
        /// </summary>
        internal bool IsStartCapSelected => currentSegment == Segment.StartCap;

        /// <summary>
        /// Whether the end cap segment is currently selected.
        /// </summary>
        internal bool IsEndCapSelected => currentSegment == Segment.EndCap;

        /// <summary>
        /// Initializes the segment and line-cap controls.
        /// </summary>
        /// <param name="lineMenu">The game object of the line menu.</param>
        internal LineCapMenu(GameObject lineMenu)
        {
            segmentText =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "SegmentText");

            segmentSelection =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "SegmentSelection");

            lineCapText =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "LineCapText");

            lineCapSelection =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "LineCapSelection");

            segmentSelector = segmentSelection.GetComponent<HorizontalSelector>();
            lineCapSelector = lineCapSelection.GetComponent<HorizontalSelector>();

            foreach (Segment segment in GetSegments())
            {
                segmentSelector.CreateNewItem(segment.ToString());
            }

            segmentSelector.defaultIndex = 0;

            foreach (LineCap lineCap in GetEditableLineCaps())
            {
                lineCapSelector.CreateNewItem(lineCap.ToString());
            }

            lineCapSelector.defaultIndex = 0;
        }

        /// <summary>
        /// Gets all editable line segments.
        /// </summary>
        /// <returns>All editable line segments.</returns>
        private static IList<Segment> GetSegments()
        {
            return Enum.GetValues(typeof(Segment)).Cast<Segment>().ToList();
        }

        /// <summary>
        /// Resets the selected segment to the main line.
        /// </summary>
        internal void Reset()
        {
            currentSegment = Segment.Main;
            segmentSelector.index = 0;
            segmentSelector.UpdateUI();
        }

        /// <summary>
        /// Selects the main line segment without changing the selector UI.
        /// </summary>
        internal void SelectMain()
        {
            currentSegment = Segment.Main;
        }

        /// <summary>
        /// Registers the action to execute after the selected segment changes.
        /// </summary>
        /// <param name="action">The action to execute after a segment change.</param>
        internal void SetSegmentAction(UnityAction action)
        {
            if (segmentAction != null)
            {
                segmentSelector.selectorEvent.RemoveListener(segmentAction);
            }

            segmentAction = index =>
            {
                currentSegment = GetSegments()[index];
                action?.Invoke();
            };

            segmentSelector.selectorEvent.AddListener(segmentAction);
        }

        /// <summary>
        /// Registers the action to execute after the selected line cap changes.
        /// </summary>
        /// <param name="action">The action receiving the selected line-cap kind.</param>
        internal void SetLineCapAction(UnityAction<LineCap> action)
        {
            if (lineCapAction != null)
            {
                lineCapSelector.selectorEvent.RemoveListener(lineCapAction);
            }

            lineCapAction = index =>
            {
                action?.Invoke(GetEditableLineCaps()[index]);
            };

            lineCapSelector.selectorEvent.AddListener(lineCapAction);
        }

        /// <summary>
        /// Removes the actions registered for the segment and line-cap selectors.
        /// </summary>
        internal void RemoveListeners()
        {
            if (segmentAction != null)
            {
                segmentSelector.selectorEvent.RemoveListener(segmentAction);
                segmentAction = null;
            }

            if (lineCapAction != null)
            {
                lineCapSelector.selectorEvent.RemoveListener(lineCapAction);
                lineCapAction = null;
            }
        }

        /// <summary>
        /// Gets the configuration of the currently selected line cap.
        /// </summary>
        /// <param name="line">The edited line configuration.</param>
        /// <returns>
        /// The start or end cap configuration if a cap segment is selected;
        /// otherwise, null.
        /// </returns>
        internal LineCapConf GetSelectedCapConf(LineConf line)
        {
            return currentSegment switch
            {
                Segment.StartCap => line.LineCapStart,
                Segment.EndCap => line.LineCapEnd,
                _ => null
            };
        }

        /// <summary>
        /// Gets the selector index of the given line cap.
        /// </summary>
        /// <param name="lineCap">The line cap whose index should be returned.</param>
        /// <returns>The selector index of the given line cap.</returns>
        internal int GetLineCapIndex(LineCap lineCap)
        {
            return GetEditableLineCaps().IndexOf(lineCap);
        }

        /// <summary>
        /// Updates the selected line cap displayed by the selector.
        /// </summary>
        /// <param name="index">The selector index to display.</param>
        internal void RefreshLineCapSelector(int index)
        {
            lineCapSelector.index = index;
            lineCapSelector.UpdateUI();
        }

        /// <summary>
        /// Shows the segment selection.
        /// </summary>
        internal void EnableSegment()
        {
            segmentText.SetActive(true);
            segmentSelection.SetActive(true);
        }

        /// <summary>
        /// Hides the segment selection.
        /// </summary>
        internal void DisableSegment()
        {
            segmentText.SetActive(false);
            segmentSelection.SetActive(false);
        }

        /// <summary>
        /// Shows the line-cap selection.
        /// </summary>
        internal void EnableLineCap()
        {
            lineCapText.SetActive(true);
            lineCapSelection.SetActive(true);
        }

        /// <summary>
        /// Hides the line-cap selection.
        /// </summary>
        internal void DisableLineCap()
        {
            lineCapText.SetActive(false);
            lineCapSelection.SetActive(false);
        }
    }
}
