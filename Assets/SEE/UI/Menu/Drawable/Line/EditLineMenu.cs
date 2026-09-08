using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
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
        /// Assigns the selected line kind and tiling to the shared line-menu state.
        /// </summary>
        private readonly Action<LineKind, float> assignLineKind;

        /// <summary>
        /// Assigns the selected color kind to the shared line-menu state.
        /// </summary>
        private readonly Action<ColorKind> assignColorKind;

        /// <summary>
        /// Returns the line kind currently stored in the shared line-menu state.
        /// </summary>
        private readonly Func<LineKind> getSelectedLineKind;

        /// <summary>
        /// Returns the color kind currently stored in the shared line-menu state.
        /// </summary>
        private readonly Func<ColorKind> getSelectedColorKind;

        /// <summary>
        /// Ensures that a secondary color is visible and usable.
        /// </summary>
        private readonly Func<Color, Color> ensureValidSecondaryColor;

        /// <summary>
        /// The additional color-picker action used while editing.
        /// </summary>
        private UnityAction<Color> colorAction;

        /// <summary>
        /// The additional tiling-slider action used while editing.
        /// </summary>
        private UnityAction<float> tilingAction;

        /// <summary>
        /// The additional line-kind selector action used while editing.
        /// </summary>
        private UnityAction<int> lineKindAction;

        /// <summary>
        /// The additional color-kind selector action used while editing.
        /// </summary>
        private UnityAction<int> colorKindAction;

        /// <summary>
        /// The additionally registered action for clearing an externally stored fill-out color.
        /// </summary>
        private UnityAction clearFillOutColorAction;

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
            this.assignLineKind = assignLineKind;
            this.assignColorKind = assignColorKind;
            this.getSelectedLineKind = getSelectedLineKind;
            this.getSelectedColorKind = getSelectedColorKind;
            this.ensureValidSecondaryColor = ensureValidSecondaryColor;
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

            SetUpLineKindSelector(selectedLine, renderer, lineHolder, surface, surfaceParentName);
            SetUpColorKindSelector(selectedLine, lineHolder, surface, surfaceParentName);

            if (!isFreehandLine)
            {
                UnityAction refreshEditingUI =
                    () => RefreshUIForCurrentSegment(selectedLine, lineHolder, surface, surfaceParentName);

                lineCapMenu.SetUpSegmentEditing(
                    lineHolder,
                    DisableLineCap,
                    UpdateLineOptions,
                    refreshEditingUI,
                    ResetColorTypeSelectionToDefault);

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

            controls.TilingSlider.onValueChanged.AddListener(tilingAction = tiling =>
            {
                if (IsRefreshingUI)
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

            SetUpPrimaryColorButton(selectedLine, lineHolder, surface, surfaceParentName);
            SetUpSecondaryColorButton(selectedLine, lineHolder, surface, surfaceParentName);
            SetUpThicknessSlider(selectedLine, renderer, lineHolder, surface, surfaceParentName);
            SetUpOrderInLayerSlider(selectedLine, lineHolder, surface, surfaceParentName);
            SetUpLoopSwitch(selectedLine, lineHolder, surface, surfaceParentName);
            SetUpColorPicker(selectedLine, lineHolder, surface, surfaceParentName);
            SetUpColorKindTypeButton(selectedLine, lineHolder, surface, surfaceParentName);
            SetUpFillOutTypeButton(selectedLine, lineHolder, surface, surfaceParentName);
            SetUpFillOutSwitch(selectedLine, lineHolder, surface, surfaceParentName);
        }

        /// <summary>
        /// Removes all editing-specific listeners registered at the shared controls.
        /// </summary>
        internal void RemoveListeners()
        {
            if (lineKindAction != null)
            {
                controls.LineKindSelector.selectorEvent.RemoveListener(lineKindAction);
                lineKindAction = null;
            }

            if (colorKindAction != null)
            {
                controls.ColorKindSelector.selectorEvent.RemoveListener(colorKindAction);
                colorKindAction = null;
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

            controls.PrimaryColorButtonManager.clickEvent.RemoveAllListeners();
            controls.SecondaryColorButtonManager.clickEvent.RemoveAllListeners();

            controls.ThicknessSlider.OnValueChanged.RemoveAllListeners();
            controls.LayerSliderController.OnValueChanged.RemoveAllListeners();

            controls.LoopManager.OffEvents.RemoveAllListeners();
            controls.LoopManager.OnEvents.RemoveAllListeners();

            controls.FillOutManager.OffEvents.RemoveAllListeners();
            controls.FillOutManager.OnEvents.RemoveAllListeners();

            controls.ColorKindButtonManager.clickEvent.RemoveAllListeners();
            controls.FillOutButtonManager.clickEvent.RemoveAllListeners();

            if (colorAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(colorAction);
                colorAction = null;
            }

            clearFillOutColorAction = null;
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
            clearFillOutColorAction = clearFillOutAction;

            if (controls.FillOutButtonManager.buttonVar.interactable)
            {
                return;
            }

            if (fillOut != null && setFillOutAction != null)
            {
                controls.FillOutManager.isOn = true;

                if (FillOut(DrawShapesAction.currentShape, fillOut))
                {
                    GameObject surface = GameFinder.GetDrawableSurface(DrawShapesAction.currentShape);

                    new DrawingFillOutNetAction(
                        surface.name,
                        GameFinder.GetDrawableSurfaceParentName(surface),
                        DrawShapesAction.currentShape.name,
                        LineConf.GetLine(DrawShapesAction.currentShape).FillOutColor).Execute();

                    if (BlinkEffect.CanFillOutBeAdded(DrawShapesAction.currentShape))
                    {
                        BlinkEffect.AddFillOutToEffect(DrawShapesAction.currentShape);
                    }
                }

                if (colorAction != setFillOutAction)
                {
                    AssignColorArea(setFillOutAction, fillOut.Value);
                }
            }
            else
            {
                controls.FillOutManager.isOn = false;
                controls.FillOutManager.OffEvents.Invoke();
                RefreshFillOut();
            }
        }

        /// <summary>
        /// Assigns an action and a color to the HSV color picker while editing.
        /// The previously assigned editing action is removed first.
        /// </summary>
        /// <param name="newColorAction">The color action that should be assigned.</param>
        /// <param name="color">The color that should be assigned.</param>
        internal void AssignColorArea(UnityAction<Color> newColorAction, Color color)
        {
            if (colorAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(colorAction);
            }

            colorAction = newColorAction;

            controls.ColorPicker.AssignColor(color);
            controls.ColorPicker.onValueChanged.AddListener(newColorAction);
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
        /// Sets up the line-kind selector for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The renderer of the selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpLineKindSelector(
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
                if (IsRefreshingUI)
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
        /// Sets up the color-kind selector for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpColorKindSelector(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            assignColorKind(lineHolder.ColorKind);

            controls.ColorKindSelector.index = GetColorKinds(true).IndexOf(getSelectedColorKind());
            controls.ColorKindSelector.UpdateUI();

            if (colorKindAction != null)
            {
                controls.ColorKindSelector.selectorEvent.RemoveListener(colorKindAction);
            }

            colorKindAction = index =>
            {
                if (IsRefreshingUI)
                {
                    return;
                }

                ColorKind newKind = GetColorKinds(true)[index];

                if (IsMainSegment)
                {
                    lineHolder.ColorKind = newKind;

                    if (lineHolder.ColorKind != ColorKind.Monochrome)
                    {
                        lineHolder.SecondaryColor = ensureValidSecondaryColor(lineHolder.SecondaryColor);
                    }

                    ChangeColorKind(selectedLine, lineHolder.ColorKind, lineHolder);

                    new ChangeColorKindNetAction(
                        surface.name,
                        surfaceParentName,
                        LineConf.GetLineWithoutRenderPos(selectedLine),
                        lineHolder.ColorKind).Execute();
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.ColorKind = newKind;

                    if (capConf.ColorKind != ColorKind.Monochrome)
                    {
                        capConf.SecondaryColor = ensureValidSecondaryColor(capConf.SecondaryColor);
                    }

                    lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            };

            controls.ColorKindSelector.selectorEvent.AddListener(colorKindAction);
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
        /// Sets up the primary-color button for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpPrimaryColorButton(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.PrimaryColorButtonManager.clickEvent.RemoveAllListeners();
            controls.PrimaryColorButtonManager.clickEvent.AddListener(MutuallyExclusiveColorButtons);

            controls.PrimaryColorButtonManager.clickEvent.AddListener(() =>
            {
                if (IsMainSegment)
                {
                    AssignColorArea(color =>
                    {
                        GameEdit.ChangePrimaryColor(selectedLine, color);
                        lineHolder.PrimaryColor = color;

                        new EditLinePrimaryColorNetAction(
                            surface.name,
                            surfaceParentName,
                            selectedLine.name,
                            color).Execute();
                    }, lineHolder.PrimaryColor);
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    AssignColorArea(color =>
                    {
                        capConf.PrimaryColor = color;
                        lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                    }, capConf.PrimaryColor);
                }
            });

            controls.PrimaryColorButtonManager.buttonVar.interactable = false;
        }

        /// <summary>
        /// Sets up the secondary-color button for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpSecondaryColorButton(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.SecondaryColorButtonManager.clickEvent.RemoveAllListeners();
            controls.SecondaryColorButtonManager.clickEvent.AddListener(MutuallyExclusiveColorButtons);

            controls.SecondaryColorButtonManager.clickEvent.AddListener(() =>
            {
                if (IsMainSegment)
                {
                    lineHolder.SecondaryColor = ensureValidSecondaryColor(lineHolder.SecondaryColor);

                    AssignColorArea(color =>
                    {
                        GameEdit.ChangeSecondaryColor(selectedLine, color);
                        lineHolder.SecondaryColor = color;

                        new EditLineSecondaryColorNetAction(
                            surface.name,
                            surfaceParentName,
                            selectedLine.name,
                            color).Execute();
                    }, lineHolder.SecondaryColor);
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.SecondaryColor = ensureValidSecondaryColor(capConf.SecondaryColor);

                    AssignColorArea(color =>
                    {
                        capConf.SecondaryColor = color;
                        lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                    }, capConf.SecondaryColor);
                }
            });

            controls.SecondaryColorButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Sets up the thickness slider for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The renderer of the selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpThicknessSlider(
            GameObject selectedLine,
            LineRenderer renderer,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.ThicknessSlider.AssignValue(renderer.startWidth);

            controls.ThicknessSlider.OnValueChanged.AddListener(thickness =>
            {
                if (IsRefreshingUI)
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
        /// Sets up the order-in-layer slider for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpOrderInLayerSlider(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.LayerSliderController.AssignMaxOrder(surface.GetComponent<DrawableHolder>().OrderInLayer);
            controls.LayerSliderController.AssignValue(lineHolder.OrderInLayer);

            controls.LayerSliderController.OnValueChanged.AddListener(layerOrder =>
            {
                GameEdit.ChangeLayer(selectedLine, layerOrder);
                lineHolder.OrderInLayer = layerOrder;

                new EditLayerNetAction(
                    surface.name,
                    surfaceParentName,
                    selectedLine.name,
                    layerOrder).Execute();
            });
        }

        /// <summary>
        /// Sets up the loop switch for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpLoopSwitch(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.LoopManager.OnEvents.RemoveAllListeners();
            controls.LoopManager.OffEvents.RemoveAllListeners();

            controls.LoopManager.OnEvents.AddListener(() =>
            {
                GameEdit.ChangeLoop(selectedLine, true);
                lineHolder.Loop = true;

                new EditLineLoopNetAction(
                    surface.name,
                    surfaceParentName,
                    selectedLine.name,
                    true).Execute();
            });

            controls.LoopManager.OffEvents.AddListener(() =>
            {
                GameEdit.ChangeLoop(selectedLine, false);
                lineHolder.Loop = false;

                new EditLineLoopNetAction(
                    surface.name,
                    surfaceParentName,
                    selectedLine.name,
                    false).Execute();
            });

            controls.LoopManager.isOn = lineHolder.Loop;
            RefreshLoop();
        }

        /// <summary>
        /// Sets up the color picker for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpColorPicker(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            if (colorAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(colorAction);
            }

            if (IsMainSegment)
            {
                LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();

                switch (lineHolder.ColorKind)
                {
                    case ColorKind.Monochrome:
                        controls.ColorPicker.AssignColor(renderer.material.color);
                        break;

                    case ColorKind.Gradient:
                        controls.ColorPicker.AssignColor(renderer.startColor);
                        break;

                    case ColorKind.TwoDashed:
                        controls.ColorPicker.AssignColor(renderer.material.color);
                        break;
                }

                colorAction = color =>
                {
                    GameEdit.ChangePrimaryColor(selectedLine, color);
                    lineHolder.PrimaryColor = color;

                    new EditLinePrimaryColorNetAction(
                        surface.name,
                        surfaceParentName,
                        selectedLine.name,
                        color).Execute();
                };
            }
            else
            {
                LineCapConf capConf = GetSelectedCapConf(lineHolder);
                if (capConf == null || capConf.CapKind == LineCap.None)
                {
                    return;
                }

                controls.ColorPicker.AssignColor(capConf.PrimaryColor);

                colorAction = color =>
                {
                    LineCapConf currentCapConf = GetSelectedCapConf(lineHolder);
                    if (currentCapConf == null || currentCapConf.CapKind == LineCap.None)
                    {
                        return;
                    }

                    currentCapConf.PrimaryColor = color;
                    lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                };
            }

            controls.ColorPicker.onValueChanged.AddListener(colorAction);
        }

        /// <summary>
        /// Sets up the button selecting the color-kind editing area.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpColorKindTypeButton(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.ColorKindButtonManager.clickEvent.RemoveAllListeners();
            controls.ColorKindButtonManager.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);

            controls.ColorKindButtonManager.clickEvent.AddListener(() =>
            {
                HideFillOut();
                ShowColorKind();

                if (IsMainSegment)
                {
                    if (!controls.PrimaryColorButtonManager.buttonVar.interactable)
                    {
                        AssignColorArea(color =>
                        {
                            GameEdit.ChangePrimaryColor(selectedLine, color);
                            lineHolder.PrimaryColor = color;

                            new EditLinePrimaryColorNetAction(
                                surface.name,
                                surfaceParentName,
                                selectedLine.name,
                                color).Execute();
                        }, lineHolder.PrimaryColor);
                    }
                    else
                    {
                        lineHolder.SecondaryColor = ensureValidSecondaryColor(lineHolder.SecondaryColor);

                        AssignColorArea(color =>
                        {
                            GameEdit.ChangeSecondaryColor(selectedLine, color);
                            lineHolder.SecondaryColor = color;

                            new EditLineSecondaryColorNetAction(
                                surface.name,
                                surfaceParentName,
                                selectedLine.name,
                                color).Execute();
                        }, lineHolder.SecondaryColor);
                    }
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    if (!controls.PrimaryColorButtonManager.buttonVar.interactable)
                    {
                        AssignColorArea(color =>
                        {
                            capConf.PrimaryColor = color;
                            lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                        }, capConf.PrimaryColor);
                    }
                    else
                    {
                        capConf.SecondaryColor = ensureValidSecondaryColor(capConf.SecondaryColor);

                        AssignColorArea(color =>
                        {
                            capConf.SecondaryColor = color;
                            lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                        }, capConf.SecondaryColor);
                    }
                }

                MenuHelper.CalculateHeight(lineMenu, true);
            });

            controls.ColorKindButtonManager.buttonVar.interactable = false;
        }

        /// <summary>
        /// Sets up the button selecting the fill-out editing area.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpFillOutTypeButton(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.FillOutButtonManager.clickEvent.RemoveAllListeners();
            controls.FillOutButtonManager.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);

            controls.FillOutButtonManager.clickEvent.AddListener(() =>
            {
                HideColorKind();
                ShowFillOut();

                if (IsMainSegment)
                {
                    if (lineHolder.FillOutStatus && GetOwnFillOutObject(selectedLine) == null)
                    {
                        if (FillOut(selectedLine, lineHolder.FillOutColor))
                        {
                            new DrawingFillOutNetAction(
                                surface.name,
                                surfaceParentName,
                                selectedLine.name,
                                lineHolder.FillOutColor).Execute();
                        }
                    }

                    AssignColorArea(color =>
                    {
                        GameEdit.ChangeFillOutColor(selectedLine, color);
                        lineHolder.FillOutColor = color;

                        new EditLineFillOutColorNetAction(
                            surface.name,
                            surfaceParentName,
                            selectedLine.name,
                            color).Execute();
                    }, lineHolder.FillOutColor);
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    AssignColorArea(color =>
                    {
                        capConf.FillOutColor = color;
                        lineCapMenu.UpdateFillOutChangedByUser(capConf);
                        lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                    }, capConf.FillOutColor);
                }

                MenuHelper.CalculateHeight(lineMenu, true);
            });

            controls.FillOutButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Sets up the fill-out switch for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpFillOutSwitch(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            controls.FillOutManager.OnEvents.RemoveAllListeners();
            controls.FillOutManager.OffEvents.RemoveAllListeners();

            controls.FillOutManager.OnEvents.AddListener(() =>
            {
                if (IsRefreshingUI)
                {
                    return;
                }

                if (IsMainSegment)
                {
                    lineHolder.FillOutStatus = true;

                    if (lineHolder.FillOutColor == Color.clear)
                    {
                        lineHolder.FillOutColor = lineHolder.PrimaryColor;
                    }

                    if (FillOut(selectedLine, lineHolder.FillOutColor))
                    {
                        new DrawingFillOutNetAction(surface.name, surfaceParentName,
                            selectedLine.name, lineHolder.FillOutColor).Execute();

                        if (BlinkEffect.CanFillOutBeAdded(selectedLine))
                        {
                            BlinkEffect.AddFillOutToEffect(selectedLine);
                        }
                    }

                    AssignColorArea(color =>
                    {
                        GameEdit.ChangeFillOutColor(selectedLine, color);
                        lineHolder.FillOutColor = color;

                        new EditLineFillOutColorNetAction(surface.name, surfaceParentName,
                            selectedLine.name, color).Execute();
                    }, lineHolder.FillOutColor);
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.FillOutStatus = true;
                    lineCapMenu.UpdateFillOutChangedByUser(capConf);

                    if (capConf.FillOutColor == Color.clear)
                    {
                        capConf.FillOutColor = capConf.PrimaryColor;
                    }

                    lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            });

            controls.FillOutManager.OffEvents.AddListener(() =>
            {
                if (IsRefreshingUI)
                {
                    return;
                }

                if (IsMainSegment)
                {
                    lineHolder.FillOutStatus = false;

                    if (colorAction != null)
                    {
                        controls.ColorPicker.onValueChanged.RemoveListener(colorAction);
                        colorAction = null;
                    }

                    clearFillOutColorAction?.Invoke();

                    BlinkEffect.RemoveFillOutFromEffect(selectedLine);

                    GameObject mainFillOut = GetOwnFillOutObject(selectedLine);
                    if (mainFillOut != null)
                    {
                        UnityEngine.Object.DestroyImmediate(mainFillOut);
                    }

                    new DeleteFillOutNetAction(surface.name, surfaceParentName,
                        selectedLine.name).Execute();
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.FillOutStatus = false;
                    lineCapMenu.UpdateFillOutChangedByUser(capConf);
                    lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            });

            controls.FillOutManager.isOn = IsMainSegment
                ? lineHolder.FillOutStatus
                : GetSelectedCapConf(lineHolder)?.FillOutStatus ?? false;

            RefreshFillOut();
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
                if (colorAction != null)
                {
                    controls.ColorPicker.onValueChanged.RemoveListener(colorAction);
                }

                if (IsMainSegment)
                {
                    assignLineKind(lineHolder.LineKind, lineHolder.Tiling);
                    RefreshLineKindSelectorUI();

                    assignColorKind(lineHolder.ColorKind);
                    RefreshColorKindSelectorUI();

                    controls.ColorPicker.AssignColor(lineHolder.PrimaryColor);
                    controls.ThicknessSlider.AssignValue(lineHolder.Thickness);
                    controls.FillOutManager.isOn = lineHolder.FillOutStatus;
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
                        controls.FillOutManager.isOn = false;
                    }
                    else
                    {
                        assignLineKind(capConf.LineKind, capConf.Tiling);
                        RefreshLineKindSelectorUI();

                        assignColorKind(capConf.ColorKind);
                        RefreshColorKindSelectorUI();

                        controls.ColorPicker.AssignColor(capConf.PrimaryColor);
                        controls.ThicknessSlider.AssignValue(capConf.Thickness);
                        controls.FillOutManager.isOn = capConf.FillOutStatus;
                    }
                }

                RefreshFillOut();
            }
            finally
            {
                IsRefreshingUI = false;
            }

            SetUpColorPicker(selectedLine, lineHolder, surface, surfaceParentName);
            MenuHelper.CalculateHeight(lineMenu, true);
        }

        /// <summary>
        /// Refreshes the color-kind selector UI.
        /// </summary>
        private void RefreshColorKindSelectorUI()
        {
            controls.ColorKindSelector.index = GetColorKinds(true).IndexOf(getSelectedColorKind());
            controls.ColorKindSelector.UpdateUI();
        }

        /// <summary>
        /// Refreshes the line-kind selector UI.
        /// </summary>
        private void RefreshLineKindSelectorUI()
        {
            controls.LineKindSelector.index = GetLineKinds().IndexOf(getSelectedLineKind());
            controls.LineKindSelector.UpdateUI();
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
            if (getSelectedLineKind() != LineKind.Dashed)
            {
                controls.TilingSlider.ResetToMin();
            }

            controls.LineKindSelectionObject.SetActive(true);
            controls.LineKindTextObject.SetActive(true);
            controls.ThicknessObject.SetActive(true);

            if (IsMainSegment)
            {
                controls.LayerObject.SetActive(true);
                controls.LayerSlider.interactable = true;
                controls.LoopObject.SetActive(true);
            }
            else
            {
                controls.LayerObject.SetActive(false);
                controls.LoopObject.SetActive(false);
            }

            controls.ColorTypeSelectorObject.SetActive(true);
            controls.ColorPickerObject.SetActive(true);
            controls.TilingObject.SetActive(getSelectedLineKind() == LineKind.Dashed);

            ResetColorTypeSelectionToDefault();
            HideFillOut();
            ShowColorKind();

            controls.ColorAreaSelectorObject.SetActive(
                getSelectedColorKind() != ColorKind.Monochrome);
        }

        /// <summary>
        /// Hides all common line controls while the selected cap is disabled.
        /// </summary>
        private void DisableLineOptions()
        {
            controls.LineKindSelectionObject.SetActive(false);
            controls.LineKindTextObject.SetActive(false);
            controls.ThicknessObject.SetActive(false);
            controls.ColorAreaSelectorObject.SetActive(false);
            controls.ColorKindSelectionObject.SetActive(false);
            controls.LayerObject.SetActive(false);
            controls.LoopObject.SetActive(false);
            controls.ColorTypeSelectorObject.SetActive(false);
            controls.ColorPickerObject.SetActive(false);
            controls.TilingObject.SetActive(false);
            controls.FillOutObject.SetActive(false);
        }

        /// <summary>
        /// Makes the primary and secondary color buttons mutually exclusive.
        /// </summary>
        private void MutuallyExclusiveColorButtons()
        {
            controls.PrimaryColorButtonManager.buttonVar.interactable =
                !controls.PrimaryColorButtonManager.buttonVar.IsInteractable();

            controls.SecondaryColorButtonManager.buttonVar.interactable =
                !controls.SecondaryColorButtonManager.buttonVar.IsInteractable();
        }

        /// <summary>
        /// Makes the color-kind and fill-out buttons mutually exclusive.
        /// </summary>
        private void MutuallyExclusiveColorTypeButtons()
        {
            controls.ColorKindButtonManager.buttonVar.interactable =
                !controls.ColorKindButtonManager.buttonVar.IsInteractable();

            controls.FillOutButtonManager.buttonVar.interactable =
                !controls.FillOutButtonManager.buttonVar.IsInteractable();
        }

        /// <summary>
        /// Restores color-kind editing as the default color editing mode.
        /// </summary>
        private void ResetColorTypeSelectionToDefault()
        {
            controls.ColorKindButtonManager.buttonVar.interactable = false;
            controls.FillOutButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Shows the color-kind controls.
        /// </summary>
        private void ShowColorKind()
        {
            controls.ColorKindSelectionObject.SetActive(true);

            if (getSelectedColorKind() != ColorKind.Monochrome)
            {
                controls.ColorAreaSelectorObject.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the color-kind controls.
        /// </summary>
        private void HideColorKind()
        {
            controls.ColorKindSelectionObject.SetActive(false);
            controls.ColorAreaSelectorObject.SetActive(false);
        }

        /// <summary>
        /// Shows the fill-out controls.
        /// </summary>
        private void ShowFillOut()
        {
            controls.FillOutObject.SetActive(true);
        }

        /// <summary>
        /// Hides the fill-out controls.
        /// </summary>
        private void HideFillOut()
        {
            controls.FillOutObject.SetActive(false);
        }

        /// <summary>
        /// Refreshes the loop switch.
        /// </summary>
        private void RefreshLoop()
        {
            controls.LoopObject.SetActive(false);
            controls.LoopObject.SetActive(true);
        }

        /// <summary>
        /// Refreshes the fill-out switch.
        /// </summary>
        private void RefreshFillOut()
        {
            controls.FillOutObject.SetActive(!controls.FillOutObject.activeInHierarchy);
            controls.FillOutObject.SetActive(!controls.FillOutObject.activeInHierarchy);
        }
    }
}
