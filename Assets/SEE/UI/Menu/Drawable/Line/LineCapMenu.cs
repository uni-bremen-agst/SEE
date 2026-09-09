using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Manages the segment and line-cap selection and editing of the line menu.
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
        /// The game object of the complete line menu.
        /// </summary>
        private readonly GameObject lineMenu;

        /// <summary>
        /// Returns whether the line menu is currently in edit mode.
        /// </summary>
        private readonly Func<bool> isInEditMode;

        /// <summary>
        /// Holds temporary state used while editing line caps.
        /// </summary>
        private readonly LineCapEditState editState = new();

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
        /// <param name="isInEditMode">
        /// Returns whether the line menu is currently in edit mode.
        /// </param>
        internal LineCapMenu(GameObject lineMenu, Func<bool> isInEditMode)
        {
            this.lineMenu = lineMenu;
            this.isInEditMode = isInEditMode;

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
        /// Initializes the temporary line-cap state for a new edit operation.
        /// </summary>
        /// <param name="line">The line configuration being edited.</param>
        internal void BeginEditing(LineConf line)
        {
            editState.Initialize(line);
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
        /// Sets up the segment selector for editing.
        /// </summary>
        /// <param name="line">The edited line configuration.</param>
        /// <param name="disableLineCap">
        /// Hides the line-cap selection and restores the main line options.
        /// </param>
        /// <param name="updateLineOptions">
        /// Updates the common line options for the selected cap kind.
        /// </param>
        /// <param name="refreshEditingUI">
        /// Refreshes the common editing controls for the selected segment.
        /// </param>
        /// <param name="resetColorTypeSelection">
        /// Resets the color-type selection to its default state.
        /// </param>
        internal void SetUpSegmentEditing(LineConf line,
            UnityAction disableLineCap,
            UnityAction<LineCap> updateLineOptions,
            UnityAction refreshEditingUI,
            UnityAction resetColorTypeSelection)
        {
            SetSegmentAction(() =>
            {
                if (!IsMainSelected)
                {
                    LineCap currentCap = IsStartCapSelected
                        ? line.LineCapStart.CapKind
                        : line.LineCapEnd.CapKind;

                    int capIndex = GetLineCapIndex(currentCap);

                    EnableLineCap();
                    updateLineOptions?.Invoke(currentCap);
                    RefreshLineCapSelectorDelayedAsync(capIndex).Forget();
                }
                else
                {
                    disableLineCap?.Invoke();
                }

                refreshEditingUI?.Invoke();
                resetColorTypeSelection?.Invoke();
            });
        }

        /// <summary>
        /// Configures editing of the line-cap kind of the currently selected cap segment.
        /// </summary>
        /// <param name="selectedLine">The line being edited.</param>
        /// <param name="line">The configuration of the edited line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="surfaceParentName">The name of the drawable surface parent.</param>
        /// <param name="updateLineOptions">
        /// Updates the visibility of line-specific editing options.
        /// </param>
        /// <param name="refreshEditingUI">
        /// Refreshes the common editing controls for the selected segment.
        /// </param>
        internal void SetUpLineCapEditing(
            GameObject selectedLine,
            LineConf line,
            GameObject surface,
            string surfaceParentName,
            UnityAction<LineCap> updateLineOptions,
            UnityAction refreshEditingUI)
        {
            SetLineCapAction(selectedCap =>
            {
                bool isStartCap = IsStartCapSelected;

                LineCapConf currentCapConf = isStartCap
                    ? line.LineCapStart
                    : line.LineCapEnd;

                LineCap oldCap = currentCapConf.CapKind;
                bool requiresUIRefresh = oldCap != selectedCap;

                if (oldCap != LineCap.None && requiresUIRefresh)
                {
                    editState.RememberPreviousCapConf(currentCapConf, isStartCap);
                }

                LineCapConf newCapConf = currentCapConf.Clone();
                newCapConf.CapKind = selectedCap;

                if (oldCap == LineCap.None && selectedCap != LineCap.None)
                {
                    editState.InitializeCapConf(line, newCapConf, isStartCap);
                }

                if (requiresUIRefresh || selectedCap == LineCap.None)
                {
                    GameDrawer.ApplyCapKindDefaults(line, newCapConf);
                }

                if (selectedCap == LineCap.None)
                {
                    newCapConf.UseOwnVisuals = false;
                }

                if (isStartCap)
                {
                    line.LineCapStart = newCapConf;
                }
                else
                {
                    line.LineCapEnd = newCapConf;
                }

                GameEdit.ChangeLineCaps(
                    selectedLine,
                    line,
                    line.LineCapStart.CapKind,
                    line.LineCapEnd.CapKind);

                new EditLineCapsNetAction(
                    surface.name,
                    surfaceParentName,
                    selectedLine.name,
                    line,
                    line.LineCapStart.CapKind,
                    line.LineCapEnd.CapKind).Execute();

                LineConf refreshedLine = LineConf.GetLine(selectedLine);
                if (refreshedLine != null)
                {
                    line.LineCapStart = refreshedLine.LineCapStart;
                    line.LineCapEnd = refreshedLine.LineCapEnd;
                    line.FillOutStatus = refreshedLine.FillOutStatus;
                    line.FillOutColor = refreshedLine.FillOutColor;
                }

                SynchronizeShapeMenuLineCapsForPreview(selectedLine, line);

                LineCapConf selectedCapConf = isStartCap
                    ? line.LineCapStart
                    : line.LineCapEnd;

                if (editState.RestoreRememberedFillOutIfNotChangedByUser(
                        selectedCapConf, isStartCap))
                {
                    ApplySelectedCapStyle(selectedLine, line, surface);
                }

                updateLineOptions?.Invoke(selectedCap);

                if (requiresUIRefresh)
                {
                    refreshEditingUI?.Invoke();
                }
                else
                {
                    RecalculateMenuHeightDelayedAsync().Forget();
                }
            });
        }

        /// <summary>
        /// Registers the action to execute after the selected segment changes.
        /// </summary>
        /// <param name="action">The action to execute after a segment change.</param>
        private void SetSegmentAction(UnityAction action)
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
        private void SetLineCapAction(UnityAction<LineCap> action)
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
        /// Applies the visual style of the currently selected line cap locally
        /// and synchronizes the change over the network.
        /// </summary>
        /// <param name="selectedLine">The edited line.</param>
        /// <param name="line">The edited line configuration.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        internal void ApplySelectedCapStyle(GameObject selectedLine,
            LineConf line, GameObject surface)
        {
            LineCapConf capConf = GetSelectedCapConf(line);
            if (capConf == null || capConf.CapKind == LineCap.None)
            {
                return;
            }

            capConf.UseOwnVisuals = true;

            bool isStartCap = IsStartCapSelected;

            GameEdit.ChangeLineCapStyle(selectedLine, isStartCap, capConf);

            SynchronizeShapeMenuLineCapsForPreview(selectedLine, line);

            new EditLineCapStyleNetAction(
                surface.name,
                GameFinder.GetDrawableSurfaceParentName(surface),
                selectedLine.name,
                isStartCap,
                capConf).Execute();
        }

        /// <summary>
        /// Updates whether the fill-out state of the selected cap was explicitly
        /// changed during the current edit operation.
        /// </summary>
        /// <param name="capConf">The edited line-cap configuration.</param>
        internal void UpdateFillOutChangedByUser(LineCapConf capConf)
        {
            if (IsMainSelected)
            {
                return;
            }

            editState.UpdateFillOutChangedByUser(capConf, IsStartCapSelected);
        }

        /// <summary>
        /// Synchronizes the shape menu line-cap selection with the edited line only
        /// while the edited line is the active drawing preview.
        /// </summary>
        /// <param name="selectedLine">The edited line.</param>
        /// <param name="line">The edited line configuration.</param>
        private static void SynchronizeShapeMenuLineCapsForPreview(
            GameObject selectedLine, LineConf line)
        {
            if (!DrawShapesAction.IsCurrentPreviewShape(selectedLine) || line == null)
            {
                return;
            }

            ShapeMenu.SetLineCaps(
                line.LineCapStart,
                line.LineCapEnd);
        }

        /// <summary>
        /// Gets the selector index of the given line cap.
        /// </summary>
        /// <param name="lineCap">The line cap whose index should be returned.</param>
        /// <returns>The selector index of the given line cap.</returns>
        private static int GetLineCapIndex(LineCap lineCap)
        {
            return GetEditableLineCaps().IndexOf(lineCap);
        }

        /// <summary>
        /// Refreshes the line-cap selector in the next frame.
        /// </summary>
        /// <param name="index">The selector index to display.</param>
        private async UniTaskVoid RefreshLineCapSelectorDelayedAsync(int index)
        {
            await UniTask.Yield();

            if (lineCapSelector == null
                || lineCapSelector.gameObject == null
                || !isInEditMode())
            {
                return;
            }

            lineCapSelector.index = index;
            lineCapSelector.UpdateUI();
        }

        /// <summary>
        /// Recalculates the line-menu height in the next frame after UI changes
        /// have been applied.
        /// </summary>
        private async UniTaskVoid RecalculateMenuHeightDelayedAsync()
        {
            await UniTask.Yield();

            if (lineMenu == null || !lineMenu.activeInHierarchy)
            {
                return;
            }

            MenuHelper.CalculateHeight(lineMenu, true);
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
