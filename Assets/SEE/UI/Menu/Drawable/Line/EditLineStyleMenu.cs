using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using System;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.UI.Menu.Drawable.Line
{
    /// <summary>
    /// Manages editing of the line style, including line kind, tiling,
    /// and thickness for the main line and its caps.
    /// </summary>
    internal sealed class EditLineStyleMenu
    {
        /// <summary>
        /// The shared UI controls of the line menu.
        /// </summary>
        private readonly LineMenuControls controls;

        /// <summary>
        /// Manages segment and line-cap selection and editing.
        /// </summary>
        private readonly LineCapMenu lineCapMenu;

        /// <summary>
        /// Assigns the selected line kind and tiling to the shared line-menu state.
        /// </summary>
        private readonly Action<LineKind, float> assignLineKind;

        /// <summary>
        /// Returns the line kind currently stored in the shared line-menu state.
        /// </summary>
        private readonly Func<LineKind> getSelectedLineKind;

        /// <summary>
        /// Returns whether the editing UI is currently refreshed programmatically.
        /// </summary>
        private readonly Func<bool> isRefreshingUI;

        /// <summary>
        /// The additional tiling-slider action used while editing.
        /// </summary>
        private UnityAction<float> tilingAction;

        /// <summary>
        /// The additional line-kind selector action used while editing.
        /// </summary>
        private UnityAction<int> lineKindAction;

        /// <summary>
        /// Whether the main line segment is currently selected.
        /// </summary>
        private bool IsMainSegment => lineCapMenu.IsMainSelected;

        /// <summary>
        /// Initializes the line-style editing component.
        /// </summary>
        /// <param name="controls">The shared UI controls of the line menu.</param>
        /// <param name="lineCapMenu">The component managing line-cap editing.</param>
        /// <param name="assignLineKind">
        /// Assigns a line kind and tiling to the shared line-menu state.
        /// </param>
        /// <param name="getSelectedLineKind">
        /// Returns the currently selected line kind.
        /// </param>
        /// <param name="isRefreshingUI">
        /// Returns whether the editing UI is currently refreshed programmatically.
        /// </param>
        internal EditLineStyleMenu(
            LineMenuControls controls,
            LineCapMenu lineCapMenu,
            Action<LineKind, float> assignLineKind,
            Func<LineKind> getSelectedLineKind,
            Func<bool> isRefreshingUI)
        {
            this.controls = controls;
            this.lineCapMenu = lineCapMenu;
            this.assignLineKind = assignLineKind;
            this.getSelectedLineKind = getSelectedLineKind;
            this.isRefreshingUI = isRefreshingUI;
        }

        /// <summary>
        /// Sets up the line-kind selector for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The renderer of the selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SetUpLineKindSelector(
            GameObject selectedLine,
            LineRenderer renderer,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            assignLineKind(selectedLine.GetComponent<LineValueHolder>().LineKind, renderer.textureScale.x);

            controls.LineKindSelector.index = GetLineKinds().IndexOf(getSelectedLineKind());
            controls.LineKindSelector.UpdateUI();

            if (lineKindAction != null)
            {
                controls.LineKindSelector.selectorEvent.RemoveListener(lineKindAction);
            }

            lineKindAction = index =>
            {
                if (isRefreshingUI())
                {
                    return;
                }

                LineKind newKind = GetLineKinds()[index];

                if (newKind == LineKind.Dashed)
                {
                    return;
                }

                if (IsMainSegment)
                {
                    lineHolder.LineKind = newKind;

                    if (lineHolder.LineKind == LineKind.Solid
                        && lineHolder.ColorKind == ColorKind.TwoDashed)
                    {
                        lineHolder.ColorKind = ColorKind.Monochrome;

                        ChangeColorKind(selectedLine, lineHolder.ColorKind, lineHolder);

                        new ChangeColorKindNetAction(
                            surface.name,
                            surfaceParentName,
                            LineConf.GetLineWithoutRenderPos(selectedLine),
                            lineHolder.ColorKind).Execute();
                    }

                    ChangeLineKind(selectedLine, lineHolder.LineKind, lineHolder.Tiling);

                    new ChangeLineKindNetAction(
                        surface.name,
                        surfaceParentName,
                        selectedLine.name,
                        lineHolder.LineKind,
                        lineHolder.Tiling).Execute();
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.LineKind = newKind;

                    if (capConf.LineKind == LineKind.Solid
                        && capConf.ColorKind == ColorKind.TwoDashed)
                    {
                        capConf.ColorKind = ColorKind.Monochrome;
                    }

                    lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            };

            controls.LineKindSelector.selectorEvent.AddListener(lineKindAction);
        }

        /// <summary>
        /// Sets up the tiling slider for editing dashed lines.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SetUpTilingSlider(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.TilingSlider.onValueChanged.AddListener(tilingAction = tiling =>
            {
                if (isRefreshingUI())
                {
                    return;
                }

                if (IsMainSegment)
                {
                    lineHolder.LineKind = LineKind.Dashed;
                    lineHolder.Tiling = tiling;

                    ChangeLineKind(selectedLine, LineKind.Dashed, tiling);

                    new ChangeLineKindNetAction(
                        surface.name,
                        surfaceParentName,
                        selectedLine.name,
                        LineKind.Dashed,
                        tiling).Execute();
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.LineKind = LineKind.Dashed;
                    capConf.Tiling = tiling;

                    lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            });
        }

        /// <summary>
        /// Sets up the thickness slider for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The renderer of the selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SetUpThicknessSlider(
            GameObject selectedLine,
            LineRenderer renderer,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.ThicknessSlider.AssignValue(renderer.startWidth);

            controls.ThicknessSlider.OnValueChanged.AddListener(thickness =>
            {
                if (isRefreshingUI())
                {
                    return;
                }

                if (thickness <= 0.0f)
                {
                    return;
                }

                if (IsMainSegment)
                {
                    GameEdit.ChangeThickness(selectedLine, thickness);
                    lineHolder.Thickness = thickness;

                    new EditLineThicknessNetAction(
                        surface.name,
                        surfaceParentName,
                        selectedLine.name,
                        thickness).Execute();
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.Thickness = thickness;
                    lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            });
        }

        /// <summary>
        /// Refreshes the line-kind selector for the given visual configuration.
        /// </summary>
        /// <param name="visualConf">The visual configuration whose line kind should be displayed.</param>
        internal void RefreshLineKind(ILineVisualConf visualConf)
        {
            if (visualConf == null)
            {
                return;
            }

            assignLineKind(visualConf.LineKind, visualConf.Tiling);
            RefreshLineKindSelectorUI();
        }

        /// <summary>
        /// Refreshes the thickness control for the given visual configuration.
        /// </summary>
        /// <param name="visualConf">The visual configuration whose thickness should be displayed.</param>
        internal void RefreshThickness(ILineVisualConf visualConf)
        {
            if (visualConf == null)
            {
                return;
            }

            controls.ThicknessSlider.AssignValue(visualConf.Thickness);
        }

        /// <summary>
        /// Shows all controls belonging to line-style editing.
        /// </summary>
        internal void ShowControls()
        {
            if (getSelectedLineKind() != LineKind.Dashed)
            {
                controls.TilingSlider.ResetToMin();
            }

            controls.LineKindSelectionObject.SetActive(true);
            controls.LineKindTextObject.SetActive(true);
            controls.ThicknessObject.SetActive(true);
            controls.TilingObject.SetActive(getSelectedLineKind() == LineKind.Dashed);
        }

        /// <summary>
        /// Hides all controls belonging to line-style editing.
        /// </summary>
        internal void HideControls()
        {
            controls.LineKindSelectionObject.SetActive(false);
            controls.LineKindTextObject.SetActive(false);
            controls.ThicknessObject.SetActive(false);
            controls.TilingObject.SetActive(false);
        }

        /// <summary>
        /// Removes all listeners registered by the line-style editing component.
        /// </summary>
        internal void RemoveListeners()
        {
            if (lineKindAction != null)
            {
                controls.LineKindSelector.selectorEvent.RemoveListener(lineKindAction);
                lineKindAction = null;
            }

            if (tilingAction != null)
            {
                if (getSelectedLineKind() != LineKind.Dashed)
                {
                    controls.TilingSlider.ResetToMin();
                }

                controls.TilingSlider.onValueChanged.RemoveListener(tilingAction);
                tilingAction = null;
            }

            controls.ThicknessSlider.OnValueChanged.RemoveAllListeners();
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
        /// Refreshes the line-kind selector UI.
        /// </summary>
        private void RefreshLineKindSelectorUI()
        {
            controls.LineKindSelector.index = GetLineKinds().IndexOf(getSelectedLineKind());
            controls.LineKindSelector.UpdateUI();
        }
    }
}
