using SEE.Game.Drawable;
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
    /// Configures the shared line-menu controls for drawing new lines.
    /// The values selected in this menu are stored in <see cref="ValueHolder"/>
    /// and are therefore reused by subsequently created lines.
    /// </summary>
    internal sealed class DrawLineMenu
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
        /// Assigns the selected line kind to the shared line-menu state.
        /// </summary>
        private readonly Action<LineKind> assignLineKind;

        /// <summary>
        /// Assigns the selected color kind to the shared line-menu state.
        /// </summary>
        private readonly Action<ColorKind> assignColorKind;

        /// <summary>
        /// Ensures that a secondary color is visible and usable.
        /// </summary>
        private readonly Func<Color, Color> ensureValidSecondaryColor;

        /// <summary>
        /// The additional action registered at the color picker while drawing.
        /// </summary>
        private UnityAction<Color> colorAction;

        /// <summary>
        /// The additional action registered at the tiling slider while drawing.
        /// </summary>
        private UnityAction<float> tilingAction;

        /// <summary>
        /// The additional action registered at the line-kind selector while drawing.
        /// </summary>
        private UnityAction<int> lineKindAction;

        /// <summary>
        /// The additional action registered at the color-kind selector while drawing.
        /// </summary>
        private UnityAction<int> colorKindAction;

        /// <summary>
        /// Initializes the drawing-specific part of the line menu.
        /// </summary>
        /// <param name="lineMenu">The game object containing the complete line menu.</param>
        /// <param name="controls">The shared UI controls of the line menu.</param>
        /// <param name="assignLineKind">
        /// Assigns the selected line kind to the shared line-menu state.
        /// </param>
        /// <param name="assignColorKind">
        /// Assigns the selected color kind to the shared line-menu state.
        /// </param>
        /// <param name="ensureValidSecondaryColor">
        /// Ensures that a secondary color is visible and usable.
        /// </param>
        internal DrawLineMenu(GameObject lineMenu,
            LineMenuControls controls,
            Action<LineKind> assignLineKind,
            Action<ColorKind> assignColorKind,
            Func<Color, Color> ensureValidSecondaryColor)
        {
            this.lineMenu = lineMenu;
            this.controls = controls;
            this.assignLineKind = assignLineKind;
            this.assignColorKind = assignColorKind;
            this.ensureValidSecondaryColor = ensureValidSecondaryColor;
        }

        /// <summary>
        /// Configures all shared line-menu controls for drawing.
        /// </summary>
        internal void Enable()
        {
            controls.TilingSlider.onValueChanged.AddListener(
                tilingAction = tiling =>
                {
                    ValueHolder.CurrentTiling = tiling;
                });

            SetUpLineKindSelector();
            SetUpColorKindSelector();
            SetUpPrimaryColorButton();
            SetUpSecondaryColorButton();
            SetUpThicknessSlider();
            SetUpColorKindTypeButton();
            SetUpFillOutTypeButton();
            SetUpFillOutSwitch();

            controls.ColorPicker.AssignColor(ValueHolder.CurrentPrimaryColor);
            controls.ColorPicker.onValueChanged.AddListener(
                colorAction = color =>
                {
                    ValueHolder.CurrentPrimaryColor = color;
                });

            MenuHelper.CalculateHeight(lineMenu, true);
        }

        /// <summary>
        /// Removes the drawing-specific listeners that are explicitly tracked by this component.
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
                controls.TilingSlider.onValueChanged.RemoveListener(tilingAction);
                tilingAction = null;
            }

            if (colorAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(colorAction);
                colorAction = null;
            }
        }

        /// <summary>
        /// Returns the fill-out color selected for drawing.
        /// </summary>
        /// <returns>
        /// The selected fill-out color if filling is enabled; otherwise, null.
        /// </returns>
        internal Color? GetFillOutColor()
        {
            return controls.FillOutManager.isOn
                ? ValueHolder.CurrentTertiaryColor
                : null;
        }

        /// <summary>
        /// Configures the line-kind selector for drawing.
        /// </summary>
        private void SetUpLineKindSelector()
        {
            assignLineKind(ValueHolder.CurrentLineKind);

            controls.LineKindSelector.index =
                GetLineKinds().IndexOf(ValueHolder.CurrentLineKind);
            controls.LineKindSelector.UpdateUI();

            if (lineKindAction != null)
            {
                controls.LineKindSelector.selectorEvent.RemoveListener(lineKindAction);
            }

            lineKindAction = index =>
            {
                ValueHolder.CurrentLineKind = GetLineKinds()[index];

                if (ValueHolder.CurrentLineKind == LineKind.Solid
                    && ValueHolder.CurrentColorKind == ColorKind.TwoDashed)
                {
                    ValueHolder.CurrentColorKind = ColorKind.Monochrome;
                }
            };

            controls.LineKindSelector.selectorEvent.AddListener(lineKindAction);
        }

        /// <summary>
        /// Configures the color-kind selector for drawing.
        /// </summary>
        private void SetUpColorKindSelector()
        {
            assignColorKind(ValueHolder.CurrentColorKind);

            controls.ColorKindSelector.index =
                GetColorKinds(true).IndexOf(ValueHolder.CurrentColorKind);
            controls.ColorKindSelector.UpdateUI();

            if (colorKindAction != null)
            {
                controls.ColorKindSelector.selectorEvent.RemoveListener(colorKindAction);
            }

            colorKindAction = index =>
            {
                ValueHolder.CurrentColorKind = GetColorKinds(true)[index];

                if (ValueHolder.CurrentColorKind != ColorKind.Monochrome
                    && ValueHolder.CurrentSecondaryColor == Color.clear)
                {
                    ValueHolder.CurrentSecondaryColor =
                        ValueHolder.CurrentPrimaryColor;
                }
            };

            controls.ColorKindSelector.selectorEvent.AddListener(colorKindAction);
        }

        /// <summary>
        /// Configures the primary-color button for drawing.
        /// </summary>
        private void SetUpPrimaryColorButton()
        {
            controls.PrimaryColorButtonManager.clickEvent.RemoveAllListeners();
            controls.PrimaryColorButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorButtons);

            controls.PrimaryColorButtonManager.clickEvent.AddListener(() =>
            {
                AssignColorArea(
                    color => ValueHolder.CurrentPrimaryColor = color,
                    ValueHolder.CurrentPrimaryColor);
            });

            controls.PrimaryColorButtonManager.buttonVar.interactable = false;
        }

        /// <summary>
        /// Configures the secondary-color button for drawing.
        /// </summary>
        private void SetUpSecondaryColorButton()
        {
            controls.SecondaryColorButtonManager.clickEvent.RemoveAllListeners();
            controls.SecondaryColorButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorButtons);

            controls.SecondaryColorButtonManager.clickEvent.AddListener(() =>
            {
                ValueHolder.CurrentSecondaryColor =
                    ensureValidSecondaryColor(ValueHolder.CurrentSecondaryColor);

                AssignColorArea(
                    color => ValueHolder.CurrentSecondaryColor = color,
                    ValueHolder.CurrentSecondaryColor);
            });

            controls.SecondaryColorButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Configures the thickness slider for drawing.
        /// </summary>
        private void SetUpThicknessSlider()
        {
            controls.ThicknessSlider.AssignValue(ValueHolder.CurrentThickness);

            controls.ThicknessSlider.OnValueChanged.AddListener(thickness =>
            {
                ValueHolder.CurrentThickness = thickness;
            });
        }

        /// <summary>
        /// Configures the color-kind area button for drawing.
        /// </summary>
        private void SetUpColorKindTypeButton()
        {
            controls.ColorKindButtonManager.clickEvent.RemoveAllListeners();
            controls.ColorKindButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorTypeButtons);

            controls.ColorKindButtonManager.clickEvent.AddListener(() =>
            {
                HideFillOut();
                ShowColorKind();
                MenuHelper.CalculateHeight(lineMenu, true);

                if (!controls.PrimaryColorButtonManager.buttonVar.interactable)
                {
                    AssignColorArea(
                        color => ValueHolder.CurrentPrimaryColor = color,
                        ValueHolder.CurrentPrimaryColor);
                }
                else
                {
                    ValueHolder.CurrentSecondaryColor =
                        ensureValidSecondaryColor(ValueHolder.CurrentSecondaryColor);

                    AssignColorArea(
                        color => ValueHolder.CurrentSecondaryColor = color,
                        ValueHolder.CurrentSecondaryColor);
                }
            });

            controls.ColorKindButtonManager.buttonVar.interactable = false;
        }

        /// <summary>
        /// Configures the fill-out area button for drawing.
        /// </summary>
        private void SetUpFillOutTypeButton()
        {
            controls.FillOutButtonManager.clickEvent.RemoveAllListeners();
            controls.FillOutButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorTypeButtons);

            controls.FillOutButtonManager.clickEvent.AddListener(() =>
            {
                HideColorKind();
                ShowFillOut();
                MenuHelper.CalculateHeight(lineMenu, true);

                if (ValueHolder.CurrentTertiaryColor == Color.clear)
                {
                    ValueHolder.CurrentTertiaryColor =
                        ValueHolder.CurrentPrimaryColor;
                }

                AssignColorArea(
                    color => ValueHolder.CurrentTertiaryColor = color,
                    ValueHolder.CurrentTertiaryColor);
            });

            controls.FillOutButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Configures the fill-out switch for drawing.
        /// </summary>
        private void SetUpFillOutSwitch()
        {
            controls.FillOutManager.OnEvents.RemoveAllListeners();
            controls.FillOutManager.OnEvents.AddListener(() =>
            {
                ValueHolder.CurrentFillOutStatus = true;
            });

            controls.FillOutManager.OffEvents.RemoveAllListeners();
            controls.FillOutManager.OffEvents.AddListener(() =>
            {
                ValueHolder.CurrentFillOutStatus = false;
            });

            controls.FillOutManager.isOn = ValueHolder.CurrentFillOutStatus;
            RefreshFillOut();
        }

        /// <summary>
        /// Assigns an action and color to the shared color picker.
        /// </summary>
        /// <param name="newColorAction">The action executed when the color changes.</param>
        /// <param name="color">The color to display in the picker.</param>
        private void AssignColorArea(UnityAction<Color> newColorAction, Color color)
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
        /// Shows the color-kind controls for drawing.
        /// </summary>
        private void ShowColorKind()
        {
            controls.ColorKindSelectionObject.SetActive(true);

            if (ValueHolder.CurrentColorKind != ColorKind.Monochrome)
            {
                controls.ColorAreaSelectorObject.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the color-kind controls for drawing.
        /// </summary>
        private void HideColorKind()
        {
            controls.ColorKindSelectionObject.SetActive(false);
            controls.ColorAreaSelectorObject.SetActive(false);
        }

        /// <summary>
        /// Shows the fill-out controls for drawing.
        /// </summary>
        private void ShowFillOut()
        {
            controls.FillOutObject.SetActive(true);
        }

        /// <summary>
        /// Hides the fill-out controls for drawing.
        /// </summary>
        private void HideFillOut()
        {
            controls.FillOutObject.SetActive(false);
        }

        /// <summary>
        /// Refreshes the fill-out switch after its state changed.
        /// </summary>
        private void RefreshFillOut()
        {
            controls.FillOutObject.SetActive(
                !controls.FillOutObject.activeInHierarchy);
            controls.FillOutObject.SetActive(
                !controls.FillOutObject.activeInHierarchy);
        }
    }
}
