using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable.Line
{
    /// <summary>
    /// Manages fill-out editing for the main line and its line caps.
    /// </summary>
    internal sealed class EditLineFillOutMenu
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
        /// Assigns an action and color to the shared color picker.
        /// </summary>
        private readonly Action<UnityAction<Color>, Color> assignColorArea;

        /// <summary>
        /// Clears the currently assigned action from the shared color picker.
        /// </summary>
        private readonly Action clearColorArea;

        /// <summary>
        /// Returns whether the given action is currently assigned to the shared
        /// color picker.
        /// </summary>
        private readonly Func<UnityAction<Color>, bool> isColorAreaAssigned;

        /// <summary>
        /// Returns whether the editing UI is currently refreshed programmatically.
        /// </summary>
        private readonly Func<bool> isRefreshingUI;

        /// <summary>
        /// The additionally registered action for clearing an externally stored
        /// fill-out color.
        /// </summary>
        private UnityAction clearFillOutColorAction;

        /// <summary>
        /// Whether the main line segment is currently selected.
        /// </summary>
        private bool IsMainSegment => lineCapMenu.IsMainSelected;

        /// <summary>
        /// Initializes the fill-out editing part of the line menu.
        /// </summary>
        /// <param name="lineMenu">
        /// The game object containing the complete line menu.
        /// </param>
        /// <param name="controls">The shared UI controls of the line menu.</param>
        /// <param name="lineCapMenu">
        /// The component managing line-cap editing.
        /// </param>
        /// <param name="assignColorArea">
        /// Assigns an action and color to the shared color picker.
        /// </param>
        /// <param name="clearColorArea">
        /// Clears the currently assigned action from the shared color picker.
        /// </param>
        /// <param name="isColorAreaAssigned">
        /// Returns whether an action is currently assigned to the shared color picker.
        /// </param>
        /// <param name="isRefreshingUI">
        /// Returns whether the editing UI is currently refreshed programmatically.
        /// </param>
        internal EditLineFillOutMenu(
            GameObject lineMenu,
            LineMenuControls controls,
            LineCapMenu lineCapMenu,
            Action<UnityAction<Color>, Color> assignColorArea,
            Action clearColorArea,
            Func<UnityAction<Color>, bool> isColorAreaAssigned,
            Func<bool> isRefreshingUI)
        {
            this.lineMenu = lineMenu;
            this.controls = controls;
            this.lineCapMenu = lineCapMenu;
            this.assignColorArea = assignColorArea;
            this.clearColorArea = clearColorArea;
            this.isColorAreaAssigned = isColorAreaAssigned;
            this.isRefreshingUI = isRefreshingUI;
        }

        /// <summary>
        /// Sets up the button selecting the fill-out editing area.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">
        /// The parent ID of the drawable surface.
        /// </param>
        /// <param name="hideColorControls">
        /// Hides the regular color-editing controls.
        /// </param>
        internal void SetUpFillOutTypeButton(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName,
            Action hideColorControls)
        {
            controls.FillOutButtonManager.clickEvent.RemoveAllListeners();

            controls.FillOutButtonManager.clickEvent.AddListener(() =>
            {
                SelectFillOutType();

                hideColorControls();
                ShowControls();

                if (IsMainSegment)
                {
                    if (lineHolder.FillOutStatus
                        && GameLineFillOut.GetOwnFillOutObject(selectedLine) == null)
                    {
                        if (GameLineFillOut.FillOut(
                                selectedLine,
                                lineHolder.FillOutColor))
                        {
                            new DrawingFillOutNetAction(
                                surface.name,
                                surfaceParentName,
                                selectedLine.name,
                                lineHolder.FillOutColor).Execute();
                        }
                    }

                    assignColorArea(color =>
                    {
                        GameLineFillOut.ChangeFillOutColor(
                            selectedLine,
                            color);

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
                    LineCapConf capConf =
                        GetSelectedCapConf(lineHolder);

                    if (capConf == null)
                    {
                        return;
                    }

                    assignColorArea(color =>
                    {
                        capConf.FillOutColor = color;

                        lineCapMenu.UpdateFillOutChangedByUser(
                            capConf);

                        lineCapMenu.ApplySelectedCapStyle(
                            selectedLine,
                            lineHolder,
                            surface);
                    }, capConf.FillOutColor);
                }

                MenuHelper.CalculateHeight(
                    lineMenu,
                    true);
            });

            controls.FillOutButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Sets up the fill-out switch for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">
        /// The parent ID of the drawable surface.
        /// </param>
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
                        lineHolder.FillOutColor =
                            lineHolder.PrimaryColor;
                    }

                    if (GameLineFillOut.FillOut(
                            selectedLine,
                            lineHolder.FillOutColor))
                    {
                        new DrawingFillOutNetAction(
                            surface.name,
                            surfaceParentName,
                            selectedLine.name,
                            lineHolder.FillOutColor).Execute();

                        if (BlinkEffect.CanFillOutBeAdded(selectedLine))
                        {
                            BlinkEffect.AddFillOutToEffect(
                                selectedLine);
                        }
                    }

                    assignColorArea(color =>
                    {
                        GameLineFillOut.ChangeFillOutColor(
                            selectedLine,
                            color);

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
                    LineCapConf capConf =
                        GetSelectedCapConf(lineHolder);

                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.FillOutStatus = true;

                    lineCapMenu.UpdateFillOutChangedByUser(
                        capConf);

                    if (capConf.FillOutColor == Color.clear)
                    {
                        capConf.FillOutColor =
                            capConf.PrimaryColor;
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

                    clearColorArea();

                    clearFillOutColorAction?.Invoke();

                    BlinkEffect.RemoveFillOutFromEffect(
                        selectedLine);

                    GameObject mainFillOut =
                        GameLineFillOut.GetOwnFillOutObject(
                            selectedLine);

                    if (mainFillOut != null)
                    {
                        UnityEngine.Object.DestroyImmediate(
                            mainFillOut);
                    }

                    new DeleteFillOutNetAction(
                        surface.name,
                        surfaceParentName,
                        selectedLine.name).Execute();
                }
                else
                {
                    LineCapConf capConf =
                        GetSelectedCapConf(lineHolder);

                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.FillOutStatus = false;

                    lineCapMenu.UpdateFillOutChangedByUser(
                        capConf);

                    lineCapMenu.ApplySelectedCapStyle(
                        selectedLine,
                        lineHolder,
                        surface);
                }
            });

            controls.FillOutManager.isOn =
                IsMainSegment
                    ? lineHolder.FillOutStatus
                    : GetSelectedCapConf(lineHolder)?
                        .FillOutStatus ?? false;

            controls.FillOutManager.UpdateUI();
        }

        /// <summary>
        /// Assigns the fill-out state and callbacks of the current preview
        /// to the editing UI.
        /// </summary>
        /// <param name="fillOut">
        /// The fill-out color or null if filling is disabled.
        /// </param>
        /// <param name="setFillOutAction">
        /// The action executed when the fill-out color changes.
        /// </param>
        /// <param name="clearFillOutAction">
        /// The action registered for clearing the externally stored fill-out color.
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

            bool fillOutEnabled =
                fillOut != null
                && setFillOutAction != null;

            if (controls.FillOutManager.isOn != fillOutEnabled)
            {
                controls.FillOutManager.isOn = fillOutEnabled;
                controls.FillOutManager.UpdateUI();
            }

            if (fillOutEnabled)
            {
                if (!isColorAreaAssigned(setFillOutAction))
                {
                    assignColorArea(
                        setFillOutAction,
                        fillOut.Value);
                }
            }
            else
            {
                clearColorArea();
            }
        }

        /// <summary>
        /// Assigns the displayed fill-out switch state.
        /// </summary>
        /// <param name="fillOutStatus">
        /// Whether fill-out should be displayed as enabled.
        /// </param>
        internal void AssignFillOutStatus(bool fillOutStatus)
        {
            controls.FillOutManager.isOn = fillOutStatus;
            controls.FillOutManager.UpdateUI();
        }

        /// <summary>
        /// Shows the fill-out editing controls.
        /// </summary>
        internal void ShowControls()
        {
            controls.FillOutObject.SetActive(true);
        }

        /// <summary>
        /// Hides the fill-out editing controls.
        /// </summary>
        internal void HideControls()
        {
            controls.FillOutObject.SetActive(false);
        }

        /// <summary>
        /// Removes all listeners registered by the fill-out editing component.
        /// </summary>
        internal void RemoveListeners()
        {
            controls.FillOutManager.OffEvents.RemoveAllListeners();
            controls.FillOutManager.OnEvents.RemoveAllListeners();
            controls.FillOutButtonManager.clickEvent.RemoveAllListeners();

            clearFillOutColorAction = null;
        }

        /// <summary>
        /// Selects fill-out editing as the active color-editing area.
        /// </summary>
        private void SelectFillOutType()
        {
            controls.ColorKindButtonManager.buttonVar.interactable = true;
            controls.FillOutButtonManager.buttonVar.interactable = false;
        }

        /// <summary>
        /// Gets the configuration of the currently selected line-cap segment.
        /// </summary>
        /// <param name="lineHolder">
        /// The edited line configuration.
        /// </param>
        /// <returns>
        /// The selected start or end cap configuration, or null for the main line.
        /// </returns>
        private LineCapConf GetSelectedCapConf(LineConf lineHolder)
        {
            return lineCapMenu.GetSelectedCapConf(lineHolder);
        }
    }
}
