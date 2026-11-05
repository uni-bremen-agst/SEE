using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Editing;
using SEE.Game.Drawable.Line;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Notification;
using System.Collections;
using UnityEngine;

namespace SEE.UI.Menu.Drawable.Line
{
    /// <summary>
    /// Manages editing of object-level properties of a line.
    /// This includes the order in layer and the loop state.
    /// </summary>
    internal sealed class EditLineObjectMenu
    {
        /// <summary>
        /// The shared UI controls of the line menu.
        /// </summary>
        private readonly LineMenuControls controls;

        /// <summary>
        /// Initializes the object-property editing part of the line menu.
        /// </summary>
        /// <param name="controls">The shared UI controls of the line menu.</param>
        internal EditLineObjectMenu(LineMenuControls controls)
        {
            this.controls = controls;
        }

        /// <summary>
        /// Sets up the order-in-layer slider for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        internal void SetUpOrderInLayerSlider(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.LayerSliderController.AssignMaxOrder(
                surface.GetComponent<DrawableHolder>().OrderInLayer);

            controls.LayerSliderController.AssignValue(lineHolder.OrderInLayer);

            controls.LayerSliderController.OnValueChanged.AddListener(layerOrder =>
            {
                GameLayerChanger.SetOrderInLayer(selectedLine, layerOrder);
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
        /// The loop switch remains available for all main line segments.
        /// Activating a redundant loop on explicitly closed geometry is
        /// rejected, and the switch is restored to the actual loop state.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">
        /// The parent ID of the drawable surface.
        /// </param>
        internal void SetUpLoopSwitch(
            GameObject selectedLine,
            LineConf lineHolder,
            GameObject surface,
            string surfaceParentName)
        {
            controls.LoopManager.OnEvents.RemoveAllListeners();
            controls.LoopManager.OffEvents.RemoveAllListeners();

            controls.LoopManager.OnEvents.AddListener(() =>
            {
                GameLineEdit.ChangeLoop(selectedLine, true);

                LineConf appliedLine = LineConf.GetLine(selectedLine);

                if (appliedLine == null || !appliedLine.Loop)
                {
                    lineHolder.Loop = false;

                    controls.LoopManager.isOn = false;
                    controls.LoopManager.UpdateUI();

                    controls.LoopManager.StartCoroutine(
                        RestoreRejectedLoopSwitch(selectedLine));

                    if (appliedLine != null
                        && GameLineGeometry.HasExplicitClosingPoint(
                            GameLineGeometry.GetOriginalLinePositions(selectedLine)))
                    {
                        ShowNotification.Warn(
                            "Loop unavailable",
                            "The line is already explicitly closed. "
                            + "Move its start or end point before enabling Loop.");
                    }

                    return;
                }

                lineHolder.Loop = true;

                new EditLineLoopNetAction(
                    surface.name,
                    surfaceParentName,
                    selectedLine.name,
                    true).Execute();
            });

            controls.LoopManager.OffEvents.AddListener(() =>
            {
                GameLineEdit.ChangeLoop(selectedLine, false);

                LineConf appliedLine = LineConf.GetLine(selectedLine);

                if (appliedLine == null || appliedLine.Loop)
                {
                    controls.LoopManager.isOn = true;
                    controls.LoopManager.UpdateUI();
                    return;
                }

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
        /// Restores the loop switch after a rejected activation.
        /// The refresh is deferred by one frame so that the switch
        /// component can finish processing the original click.
        /// The displayed state is taken from the actual line geometry.
        /// </summary>
        /// <param name="selectedLine">
        /// The line whose loop state should be displayed.
        /// </param>
        /// <returns>
        /// An enumerator that waits for the current input processing
        /// to finish before refreshing the switch.
        /// </returns>
        private IEnumerator RestoreRejectedLoopSwitch(GameObject selectedLine)
        {
            yield return null;

            LineConf currentLine = LineConf.GetLine(selectedLine);

            if (currentLine != null)
            {
                controls.LoopManager.isOn = currentLine.Loop;
                controls.LoopManager.UpdateUI();
            }
        }

        /// <summary>
        /// Updates the visibility of the object-level controls for
        /// the currently selected line segment.
        /// The loop switch remains visible for the main line even if
        /// the geometry already contains an explicit closing point.
        /// </summary>
        /// <param name="isMainSegment">
        /// Whether the main line segment is currently selected.
        /// </param>
        internal void ShowControls(bool isMainSegment)
        {
            if (isMainSegment)
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
        }

        /// <summary>
        /// Hides all object-level editing controls.
        /// </summary>
        internal void HideControls()
        {
            controls.LayerObject.SetActive(false);
            controls.LoopObject.SetActive(false);
        }

        /// <summary>
        /// Removes all listeners registered by the object-property editing menu.
        /// </summary>
        internal void RemoveListeners()
        {
            controls.LayerSliderController.OnValueChanged.RemoveAllListeners();

            controls.LoopManager.OffEvents.RemoveAllListeners();
            controls.LoopManager.OnEvents.RemoveAllListeners();
        }

        /// <summary>
        /// Refreshes the loop switch without hiding it for explicitly
        /// closed lines. The validity of enabling a loop is checked
        /// when the user activates the switch.
        /// </summary>
        private void RefreshLoop()
        {
            controls.LoopObject.SetActive(false);
            controls.LoopObject.SetActive(true);
        }
    }
}
