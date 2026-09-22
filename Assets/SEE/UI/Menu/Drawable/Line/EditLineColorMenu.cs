using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using System;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.UI.Menu.Drawable.Line
{
    /// <summary>
    /// Manages color and fill-out editing for the main line and its line caps.
    /// </summary>
    internal sealed class EditLineColorMenu
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
        /// Manages line-cap selection and applies cap-specific visual changes.
        /// </summary>
        private readonly LineCapMenu lineCapMenu;

        /// <summary>
        /// Assigns the selected color kind to the shared line-menu state.
        /// </summary>
        private readonly Action<ColorKind> assignColorKind;

        /// <summary>
        /// Returns the color kind currently stored in the shared line-menu state.
        /// </summary>
        private readonly Func<ColorKind> getSelectedColorKind;

        /// <summary>
        /// Ensures that a secondary color is visible and usable.
        /// </summary>
        private readonly Func<Color, Color> ensureValidSecondaryColor;

        /// <summary>
        /// Returns whether the editing UI is currently refreshed programmatically.
        /// </summary>
        private readonly Func<bool> isRefreshingUI;

        /// <summary>
        /// The additional color-picker action used while editing.
        /// </summary>
        private UnityAction<Color> colorAction;

        /// <summary>
        /// The additional color-kind selector action used while editing.
        /// </summary>
        private UnityAction<int> colorKindAction;

        /// <summary>
        /// The additionally registered action for clearing an externally stored fill-out color.
        /// </summary>
        private UnityAction clearFillOutColorAction;

        /// <summary>
        /// Whether the main line segment is currently selected.
        /// </summary>
        private bool IsMainSegment => lineCapMenu.IsMainSelected;

        /// <summary>
        /// Initializes the color-editing part of the line menu.
        /// </summary>
        /// <param name="lineMenu">The game object containing the complete line menu.</param>
        /// <param name="controls">The shared UI controls of the line menu.</param>
        /// <param name="lineCapMenu">The component managing line-cap editing.</param>
        /// <param name="assignColorKind">
        /// Assigns a color kind to the shared line-menu state.
        /// </param>
        /// <param name="getSelectedColorKind">
        /// Returns the currently selected color kind.
        /// </param>
        /// <param name="ensureValidSecondaryColor">
        /// Ensures that a secondary color is visible and usable.
        /// </param>
        /// <param name="isRefreshingUI">
        /// Returns whether the editing UI is currently refreshed programmatically.
        /// </param>
        internal EditLineColorMenu(
            GameObject lineMenu,
            LineMenuControls controls,
            LineCapMenu lineCapMenu,
            Action<ColorKind> assignColorKind,
            Func<ColorKind> getSelectedColorKind,
            Func<Color, Color> ensureValidSecondaryColor,
            Func<bool> isRefreshingUI)
        {
            this.lineMenu = lineMenu;
            this.controls = controls;
            this.lineCapMenu = lineCapMenu;
            this.assignColorKind = assignColorKind;
            this.getSelectedColorKind = getSelectedColorKind;
            this.ensureValidSecondaryColor = ensureValidSecondaryColor;
            this.isRefreshingUI = isRefreshingUI;
        }

        /// <summary>
        /// Sets up the color-kind selector for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SetUpColorKindSelector(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            assignColorKind(lineHolder.ColorKind);

            controls.ColorKindSelector.index =
                GameLineAppearance.GetColorKinds(true).IndexOf(getSelectedColorKind());
            controls.ColorKindSelector.UpdateUI();

            if (colorKindAction != null)
            {
                controls.ColorKindSelector.selectorEvent.RemoveListener(colorKindAction);
            }

            colorKindAction = index =>
            {
                if (isRefreshingUI())
                {
                    return;
                }

                ILineVisualConf visualConf = IsMainSegment
                    ? lineHolder
                    : GetSelectedCapConf(lineHolder);

                if (visualConf == null)
                {
                    return;
                }

                ColorKind requestedKind = GameLineAppearance.GetColorKinds(true)[index];

                ColorKind newKind = GetValidColorKind(
                    requestedKind,
                    visualConf.ColorKind,
                    visualConf.LineKind);

                if (getSelectedColorKind() != newKind)
                {
                    assignColorKind(newKind);
                }

                controls.ColorKindSelector.label.text = newKind.ToString();
                controls.ColorKindSelector.index =
                    GameLineAppearance.GetColorKinds(true).IndexOf(newKind);

                visualConf.ColorKind = newKind;

                if (visualConf.ColorKind == ColorKind.Monochrome)
                {
                    SelectPrimaryColor(
                        selectedLine,
                        lineHolder,
                        surface,
                        surfaceParentName);
                }
                else
                {
                    visualConf.SecondaryColor =
                        ensureValidSecondaryColor(visualConf.SecondaryColor);
                }

                if (IsMainSegment)
                {
                    GameLineAppearance.ChangeColorKind(
                        selectedLine,
                        lineHolder.ColorKind,
                        lineHolder);

                    new ChangeColorKindNetAction(
                        surface.name,
                        surfaceParentName,
                        LineConf.GetLineWithoutRenderPos(selectedLine),
                        lineHolder.ColorKind).Execute();
                }
                else
                {
                    lineCapMenu.ApplySelectedCapStyle(
                        selectedLine,
                        lineHolder,
                        surface);
                }
            };

            controls.ColorKindSelector.selectorEvent.AddListener(colorKindAction);
        }

        /// <summary>
        /// Resolves a requested color kind for the given line kind.
        /// Two-dashed coloring is not supported for solid lines.
        /// </summary>
        /// <param name="requestedKind">The color kind requested by the selector.</param>
        /// <param name="currentKind">The currently selected color kind.</param>
        /// <param name="lineKind">The line kind to which the color kind will be applied.</param>
        /// <returns>The valid color kind to apply.</returns>
        internal static ColorKind GetValidColorKind(
            ColorKind requestedKind,
            ColorKind currentKind,
            LineKind lineKind)
        {
            if (lineKind != LineKind.Solid
                || requestedKind != ColorKind.TwoDashed)
            {
                return requestedKind;
            }

            return currentKind == ColorKind.Monochrome
                ? ColorKind.Gradient
                : ColorKind.Monochrome;
        }

        /// <summary>
        /// Sets up the primary-color button for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SetUpPrimaryColorButton(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.PrimaryColorButtonManager.clickEvent.RemoveAllListeners();
            controls.PrimaryColorButtonManager.clickEvent.AddListener(
                () => SelectPrimaryColor(selectedLine, lineHolder, surface, surfaceParentName));

            controls.PrimaryColorButtonManager.buttonVar.interactable = false;
        }

        /// <summary>
        /// Sets up the secondary-color button for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SetUpSecondaryColorButton(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.SecondaryColorButtonManager.clickEvent.RemoveAllListeners();
            controls.SecondaryColorButtonManager.clickEvent.AddListener(
                () => SelectSecondaryColor(selectedLine, lineHolder, surface, surfaceParentName));

            controls.SecondaryColorButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Sets up the color picker for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SetUpColorPicker(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            SelectPrimaryColor(
                selectedLine,
                lineHolder,
                surface,
                surfaceParentName);
        }

        /// <summary>
        /// Sets up the button selecting the color-kind editing area.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SetUpColorKindTypeButton(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.ColorKindButtonManager.clickEvent.RemoveAllListeners();
            controls.ColorKindButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorTypeButtons);

            controls.ColorKindButtonManager.clickEvent.AddListener(() =>
            {
                HideFillOut();
                ShowColorKind();

                ILineVisualConf visualConf = GetSelectedVisualConf(lineHolder);

                if (visualConf == null)
                {
                    return;
                }

                if (visualConf.ColorKind == ColorKind.Monochrome
                    || !controls.PrimaryColorButtonManager.buttonVar.interactable)
                {
                    SelectPrimaryColor(
                        selectedLine,
                        lineHolder,
                        surface,
                        surfaceParentName);
                }
                else
                {
                    SelectSecondaryColor(
                        selectedLine,
                        lineHolder,
                        surface,
                        surfaceParentName);
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
        internal void SetUpFillOutTypeButton(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.FillOutButtonManager.clickEvent.RemoveAllListeners();
            controls.FillOutButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorTypeButtons);

            controls.FillOutButtonManager.clickEvent.AddListener(() =>
            {
                HideColorKind();
                ShowFillOut();

                if (IsMainSegment)
                {
                    if (lineHolder.FillOutStatus
                        && GameLineFillOut.GetOwnFillOutObject(selectedLine) == null)
                    {
                        if (GameLineFillOut.FillOut(selectedLine, lineHolder.FillOutColor))
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
                        GameLineFillOut.ChangeFillOutColor(selectedLine, color);
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
                        lineCapMenu.ApplySelectedCapStyle(
                            selectedLine,
                            lineHolder,
                            surface);
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
        internal void SetUpFillOutSwitch(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.FillOutManager.OnEvents.RemoveAllListeners();
            controls.FillOutManager.OffEvents.RemoveAllListeners();

            controls.FillOutManager.OnEvents.AddListener(() =>
            {
                if (isRefreshingUI())
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

                    if (GameLineFillOut.FillOut(selectedLine, lineHolder.FillOutColor))
                    {
                        new DrawingFillOutNetAction(
                            surface.name,
                            surfaceParentName,
                            selectedLine.name,
                            lineHolder.FillOutColor).Execute();

                        if (BlinkEffect.CanFillOutBeAdded(selectedLine))
                        {
                            BlinkEffect.AddFillOutToEffect(selectedLine);
                        }
                    }

                    AssignColorArea(color =>
                    {
                        GameLineFillOut.ChangeFillOutColor(selectedLine, color);
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

                    capConf.FillOutStatus = true;
                    lineCapMenu.UpdateFillOutChangedByUser(capConf);

                    if (capConf.FillOutColor == Color.clear)
                    {
                        capConf.FillOutColor = capConf.PrimaryColor;
                    }

                    lineCapMenu.ApplySelectedCapStyle(
                        selectedLine,
                        lineHolder,
                        surface);
                }
            });

            controls.FillOutManager.OffEvents.AddListener(() =>
            {
                if (isRefreshingUI())
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

                    GameObject mainFillOut = GameLineFillOut.GetOwnFillOutObject(selectedLine);
                    if (mainFillOut != null)
                    {
                        UnityEngine.Object.DestroyImmediate(mainFillOut);
                    }

                    new DeleteFillOutNetAction(
                        surface.name,
                        surfaceParentName,
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
                    lineCapMenu.ApplySelectedCapStyle(
                        selectedLine,
                        lineHolder,
                        surface);
                }
            });

            controls.FillOutManager.isOn = IsMainSegment
                ? lineHolder.FillOutStatus
                : GetSelectedCapConf(lineHolder)?.FillOutStatus ?? false;

            RefreshFillOut();
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

                if (GameLineFillOut.FillOut(DrawShapesAction.currentShape, fillOut))
                {
                    GameObject surface =
                        GameFinder.GetDrawableSurface(DrawShapesAction.currentShape);

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
        /// Selects the primary color of the currently edited line segment and binds
        /// the shared color picker to it.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SelectPrimaryColor(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            SetPrimaryColorButtonState();

            if (IsMainSegment)
            {
                AssignColorArea(color =>
                {
                    GameLineAppearance.ChangePrimaryColor(selectedLine, color);
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

                if (capConf == null || capConf.CapKind == LineCap.None)
                {
                    return;
                }

                AssignColorArea(color =>
                {
                    capConf.PrimaryColor = color;

                    lineCapMenu.ApplySelectedCapStyle(
                        selectedLine,
                        lineHolder,
                        surface);
                }, capConf.PrimaryColor);
            }
        }

        /// <summary>
        /// Selects the secondary color of the currently edited line segment and binds
        /// the shared color picker to it.
        /// Monochrome configurations always use their primary color.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SelectSecondaryColor(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            ILineVisualConf visualConf = GetSelectedVisualConf(lineHolder);

            if (visualConf == null)
            {
                return;
            }

            if (visualConf.ColorKind == ColorKind.Monochrome)
            {
                SelectPrimaryColor(
                    selectedLine,
                    lineHolder,
                    surface,
                    surfaceParentName);
                return;
            }

            visualConf.SecondaryColor =
                ensureValidSecondaryColor(visualConf.SecondaryColor);

            SetSecondaryColorButtonState();

            if (IsMainSegment)
            {
                AssignColorArea(color =>
                {
                    GameLineAppearance.ChangeSecondaryColor(selectedLine, color);
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

                if (capConf == null || capConf.CapKind == LineCap.None)
                {
                    return;
                }

                AssignColorArea(color =>
                {
                    capConf.SecondaryColor = color;

                    lineCapMenu.ApplySelectedCapStyle(
                        selectedLine,
                        lineHolder,
                        surface);
                }, capConf.SecondaryColor);
            }
        }

        /// <summary>
        /// Returns the visual configuration of the currently selected line segment.
        /// </summary>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <returns>
        /// The main line configuration or the selected line-cap configuration.
        /// </returns>
        private ILineVisualConf GetSelectedVisualConf(LineConf lineHolder)
        {
            return IsMainSegment
                ? lineHolder
                : GetSelectedCapConf(lineHolder);
        }

        /// <summary>
        /// Updates the color buttons so that the primary color is selected.
        /// </summary>
        private void SetPrimaryColorButtonState()
        {
            controls.PrimaryColorButtonManager.buttonVar.interactable = false;
            controls.SecondaryColorButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Updates the color buttons so that the secondary color is selected.
        /// </summary>
        private void SetSecondaryColorButtonState()
        {
            controls.PrimaryColorButtonManager.buttonVar.interactable = true;
            controls.SecondaryColorButtonManager.buttonVar.interactable = false;
        }

        /// <summary>
        /// Assigns an action and a color to the HSV color picker while editing.
        /// The previously assigned editing action is removed first.
        /// </summary>
        /// <param name="newColorAction">The color action that should be assigned.</param>
        /// <param name="color">The color that should be assigned.</param>
        internal void AssignColorArea(
            UnityAction<Color> newColorAction,
            Color color)
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
        /// Removes the currently registered color-picker listener before a
        /// programmatic refresh of the editing UI.
        /// </summary>
        internal void BeginRefresh()
        {
            if (colorAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(colorAction);
            }
        }

        /// <summary>
        /// Refreshes the color controls for the given visual configuration.
        /// The primary color becomes the active color whenever a segment is refreshed.
        /// </summary>
        /// <param name="visualConf">The visual configuration whose colors should be displayed.</param>
        internal void RefreshColor(ILineVisualConf visualConf)
        {
            assignColorKind(visualConf.ColorKind);
            RefreshColorKindSelectorUI();

            SetPrimaryColorButtonState();
            controls.ColorPicker.AssignColor(visualConf.PrimaryColor);
        }

        /// <summary>
        /// Assigns the displayed fill-out switch state.
        /// </summary>
        /// <param name="fillOutStatus">Whether fill-out should be displayed as enabled.</param>
        internal void AssignFillOutStatus(bool fillOutStatus)
        {
            controls.FillOutManager.isOn = fillOutStatus;
        }

        /// <summary>
        /// Shows all color and fill-out editing controls.
        /// </summary>
        internal void ShowControls()
        {
            controls.ColorTypeSelectorObject.SetActive(true);
            controls.ColorPickerObject.SetActive(true);

            ResetColorTypeSelectionToDefault();
            HideFillOut();
            ShowColorKind();

            controls.ColorAreaSelectorObject.SetActive(
                getSelectedColorKind() != ColorKind.Monochrome);
        }

        /// <summary>
        /// Hides all color and fill-out editing controls.
        /// </summary>
        internal void HideControls()
        {
            controls.ColorAreaSelectorObject.SetActive(false);
            controls.ColorKindSelectionObject.SetActive(false);
            controls.ColorTypeSelectorObject.SetActive(false);
            controls.ColorPickerObject.SetActive(false);
            controls.FillOutObject.SetActive(false);
        }

        /// <summary>
        /// Restores color-kind editing as the default color-editing mode.
        /// </summary>
        internal void ResetColorTypeSelectionToDefault()
        {
            controls.ColorKindButtonManager.buttonVar.interactable = false;
            controls.FillOutButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Refreshes the fill-out switch.
        /// </summary>
        internal void RefreshFillOut()
        {
            controls.FillOutObject.SetActive(
                !controls.FillOutObject.activeInHierarchy);
            controls.FillOutObject.SetActive(
                !controls.FillOutObject.activeInHierarchy);
        }

        /// <summary>
        /// Removes all listeners registered by the color-editing component.
        /// </summary>
        internal void RemoveListeners()
        {
            if (colorKindAction != null)
            {
                controls.ColorKindSelector.selectorEvent.RemoveListener(colorKindAction);
                colorKindAction = null;
            }

            controls.PrimaryColorButtonManager.clickEvent.RemoveAllListeners();
            controls.SecondaryColorButtonManager.clickEvent.RemoveAllListeners();

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
        /// Refreshes the color-kind selector UI.
        /// </summary>
        private void RefreshColorKindSelectorUI()
        {
            controls.ColorKindSelector.index =
                GameLineAppearance.GetColorKinds(true).IndexOf(getSelectedColorKind());
            controls.ColorKindSelector.UpdateUI();
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
    }
}
