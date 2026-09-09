using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Drawable;
using System;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Manages the editing-specific behavior of the line menu.
    /// Changes are applied either to the main line or to the currently selected
    /// start or end cap.
    /// </summary>
    internal sealed class EditLineMenu
    {
        /// <summary>
        /// The game object containing the complete line menu.
        /// </summary>
        private readonly GameObject lineMenu;

        /// <summary>
        /// The shared UI controls of the line menu.
        /// </summary>
        private readonly LineMenuControls controls;

        /// <summary>
        /// Manages segment and line-cap selection and editing.
        /// </summary>
        private readonly LineCapMenu lineCapMenu;

        /// <summary>
        /// Manages editing of line kind, tiling, and thickness.
        /// </summary>
        private readonly EditLineStyleMenu styleMenu;

        /// <summary>
        /// Manages editing of object-level line properties.
        /// </summary>
        private readonly EditLineObjectMenu objectMenu;

        /// <summary>
        /// Manages color and fill-out editing.
        /// </summary>
        private readonly EditLineColorMenu colorMenu;

        /// <summary>
        /// Whether the editing UI is currently updated programmatically.
        /// During this time UI callbacks must not modify the edited line.
        /// </summary>
        internal bool IsRefreshingUI { get; private set; }

        /// <summary>
        /// Whether the main line segment is currently selected.
        /// </summary>
        private bool IsMainSegment => lineCapMenu.IsMainSelected;

        /// <summary>
        /// Initializes the editing-specific part of the line menu.
        /// </summary>
        /// <param name="lineMenu">The game object containing the complete line menu.</param>
        /// <param name="controls">The shared UI controls of the line menu.</param>
        /// <param name="lineCapMenu">The component managing line-cap editing.</param>
        /// <param name="assignLineKind">
        /// Assigns a line kind and tiling to the shared line-menu state.
        /// </param>
        /// <param name="assignColorKind">
        /// Assigns a color kind to the shared line-menu state.
        /// </param>
        /// <param name="getSelectedLineKind">
        /// Returns the currently selected line kind.
        /// </param>
        /// <param name="getSelectedColorKind">
        /// Returns the currently selected color kind.
        /// </param>
        /// <param name="ensureValidSecondaryColor">
        /// Ensures that a secondary color is visible and usable.
        /// </param>
        internal EditLineMenu(
            GameObject lineMenu,
            LineMenuControls controls,
            LineCapMenu lineCapMenu,
            Action<LineKind, float> assignLineKind,
            Action<ColorKind> assignColorKind,
            Func<LineKind> getSelectedLineKind,
            Func<ColorKind> getSelectedColorKind,
            Func<Color, Color> ensureValidSecondaryColor)
        {
            this.lineMenu = lineMenu;
            this.controls = controls;
            this.lineCapMenu = lineCapMenu;

            styleMenu = new EditLineStyleMenu(
                controls,
                lineCapMenu,
                assignLineKind,
                getSelectedLineKind,
                () => IsRefreshingUI);

            objectMenu = new EditLineObjectMenu(controls);

            colorMenu = new EditLineColorMenu(
                lineMenu,
                controls,
                lineCapMenu,
                assignColorKind,
                getSelectedColorKind,
                ensureValidSecondaryColor,
                () => IsRefreshingUI);
        }

        /// <summary>
        /// Configures all shared line-menu controls for editing the given line.
        /// </summary>
        /// <param name="selectedLine">The line object being edited.</param>
        /// <param name="lineHolder">The configuration containing the current edited values.</param>
        /// <param name="isFreehandLine">Whether the line was created through freehand drawing.</param>
        /// <param name="returnCall">An optional callback returning to the parent menu.</param>
        internal void Enable(
            GameObject selectedLine,
            LineConf lineHolder,
            bool isFreehandLine,
            UnityAction returnCall = null)
        {
            lineCapMenu.BeginEditing(lineHolder);

            if (returnCall != null)
            {
                SetUpReturnButton(returnCall);
            }

            LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
            GameObject surface = GameFinder.GetDrawableSurface(selectedLine);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            styleMenu.SetUpLineKindSelector(selectedLine, renderer, lineHolder, surface, surfaceParentName);

            colorMenu.SetUpColorKindSelector(selectedLine, lineHolder, surface, surfaceParentName);

            if (!isFreehandLine)
            {
                UnityAction refreshEditingUI =
                    () => RefreshUIForCurrentSegment(selectedLine, lineHolder, surface, surfaceParentName);

                lineCapMenu.SetUpSegmentEditing(
                    lineHolder,
                    DisableLineCap,
                    UpdateLineOptions,
                    refreshEditingUI,
                    colorMenu.ResetColorTypeSelectionToDefault);

                lineCapMenu.SetUpLineCapEditing(
                    selectedLine,
                    lineHolder,
                    surface,
                    surfaceParentName,
                    UpdateLineOptions,
                    refreshEditingUI);
            }
            else
            {
                lineCapMenu.SelectMain();
                lineCapMenu.DisableSegment();
                lineCapMenu.DisableLineCap();
            }

            styleMenu.SetUpTilingSlider(selectedLine, lineHolder, surface, surfaceParentName);

            colorMenu.SetUpPrimaryColorButton(selectedLine, lineHolder, surface, surfaceParentName);
            colorMenu.SetUpSecondaryColorButton(selectedLine, lineHolder, surface, surfaceParentName);

            styleMenu.SetUpThicknessSlider(selectedLine, renderer, lineHolder, surface, surfaceParentName);

            objectMenu.SetUpOrderInLayerSlider(selectedLine, lineHolder, surface, surfaceParentName);
            objectMenu.SetUpLoopSwitch(selectedLine, lineHolder, surface, surfaceParentName);

            colorMenu.SetUpColorPicker(selectedLine, lineHolder, surface, surfaceParentName);
            colorMenu.SetUpColorKindTypeButton(selectedLine, lineHolder, surface, surfaceParentName);
            colorMenu.SetUpFillOutTypeButton(selectedLine, lineHolder, surface, surfaceParentName);
            colorMenu.SetUpFillOutSwitch(selectedLine, lineHolder, surface, surfaceParentName);
        }

        /// <summary>
        /// Removes all editing-specific listeners registered at the shared controls.
        /// </summary>
        internal void RemoveListeners()
        {
            styleMenu.RemoveListeners();
            objectMenu.RemoveListeners();
            colorMenu.RemoveListeners();
        }

        /// <summary>
        /// Assigns a fill-out status and color while editing.
        /// </summary>
        /// <param name="fillOut">The fill-out color or null if filling is disabled.</param>
        /// <param name="setFillOutAction">
        /// The action executed when the fill-out color changes.
        /// </param>
        /// <param name="clearFillOutAction">
        /// The action registered for clearing an externally stored fill-out color.
        /// </param>
        internal void AssignFillOut(
            Color? fillOut,
            UnityAction<Color> setFillOutAction,
            UnityAction clearFillOutAction)
        {
            colorMenu.AssignFillOut(
                fillOut,
                setFillOutAction,
                clearFillOutAction);
        }

        /// <summary>
        /// Assigns an action and a color to the HSV color picker while editing.
        /// </summary>
        /// <param name="newColorAction">The color action that should be assigned.</param>
        /// <param name="color">The color that should be assigned.</param>
        internal void AssignColorArea(
            UnityAction<Color> newColorAction,
            Color color)
        {
            colorMenu.AssignColorArea(newColorAction, color);
        }

        /// <summary>
        /// Returns whether the given line was created by freehand drawing.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>True if the line was created by freehand drawing.</returns>
        internal static bool IsFreehandLine(GameObject line)
        {
            return line.TryGetComponent(out LineValueHolder holder) && holder.FreehandLine;
        }

        /// <summary>
        /// Sets up the return button for editing from another menu.
        /// </summary>
        /// <param name="returnCall">The callback returning to the parent menu.</param>
        private void SetUpReturnButton(UnityAction returnCall)
        {
            controls.ReturnButtonObject.SetActive(true);

            controls.ReturnButtonManager.clickEvent.RemoveAllListeners();
            controls.ReturnButtonManager.clickEvent.AddListener(returnCall);

            controls.LayerSlider.interactable = false;
        }

        /// <summary>
        /// Gets the configuration of the currently selected line-cap segment.
        /// </summary>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <returns>
        /// The selected start or end cap configuration, or null for the main line.
        /// </returns>
        private LineCapConf GetSelectedCapConf(LineConf lineHolder)
        {
            return lineCapMenu.GetSelectedCapConf(lineHolder);
        }

        /// <summary>
        /// Updates the visibility of the editable line options for a line cap.
        /// </summary>
        /// <param name="lineCap">
        /// The line cap whose kind determines whether the controls are available.
        /// </param>
        private void UpdateLineOptions(LineCap lineCap)
        {
            if (lineCap != LineCap.None)
            {
                EnableLineOptions();
            }
            else
            {
                DisableLineOptions();
            }
        }

        /// <summary>
        /// Refreshes all editing controls for the currently selected segment.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void RefreshUIForCurrentSegment(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            IsRefreshingUI = true;

            try
            {
                colorMenu.BeginRefresh();

                if (IsMainSegment)
                {
                    styleMenu.RefreshLineKind(lineHolder);

                    colorMenu.RefreshColor(lineHolder);

                    styleMenu.RefreshThickness(lineHolder);

                    colorMenu.AssignFillOutStatus(lineHolder.FillOutStatus);
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    if (capConf.CapKind == LineCap.None)
                    {
                        colorMenu.AssignFillOutStatus(false);
                    }
                    else
                    {
                        styleMenu.RefreshLineKind(capConf);

                        colorMenu.RefreshColor(capConf);

                        styleMenu.RefreshThickness(capConf);

                        colorMenu.AssignFillOutStatus(capConf.FillOutStatus);
                    }
                }

                colorMenu.RefreshFillOut();
            }
            finally
            {
                IsRefreshingUI = false;
            }

            colorMenu.SetUpColorPicker(
                selectedLine,
                lineHolder,
                surface,
                surfaceParentName);

            MenuHelper.CalculateHeight(lineMenu, true);
        }

        /// <summary>
        /// Hides the line-cap selector and restores the common line options.
        /// </summary>
        private void DisableLineCap()
        {
            lineCapMenu.DisableLineCap();
            EnableLineOptions();
        }

        /// <summary>
        /// Shows all controls required for editing a valid line segment.
        /// </summary>
        internal void EnableLineOptions()
        {
            styleMenu.ShowControls();
            objectMenu.ShowControls(IsMainSegment);
            colorMenu.ShowControls();
        }

        /// <summary>
        /// Hides all common line controls while the selected cap is disabled.
        /// </summary>
        private void DisableLineOptions()
        {
            styleMenu.HideControls();
            objectMenu.HideControls();
            colorMenu.HideControls();
        }
    }
}
