using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
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
        /// Updates the visibility of the object-level controls for the currently
        /// selected line segment.
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
        /// Refreshes the loop switch.
        /// </summary>
        private void RefreshLoop()
        {
            controls.LoopObject.SetActive(false);
            controls.LoopObject.SetActive(true);
        }
    }
}
