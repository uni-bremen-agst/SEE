using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable.Line;
using UnityEngine;

namespace SEE.UI.Menu.Drawable.Shapes
{
    /// <summary>
    /// Coordinates switching between the shape configuration menu
    /// and the line configuration menu.
    /// </summary>
    internal sealed class ShapeMenuSwitcher
    {
        /// <summary>
        /// The controls belonging to the shape menu and its configuration switch.
        /// </summary>
        private readonly ShapeMenuControls controls;

        /// <summary>
        /// Creates a new switcher for the given shape-menu controls.
        /// </summary>
        /// <param name="controls">
        /// The controls used for switching between the shape and line menus.
        /// </param>
        internal ShapeMenuSwitcher(ShapeMenuControls controls)
        {
            this.controls = controls;
        }

        /// <summary>
        /// Initializes the buttons used for switching between the
        /// shape and line configuration menus.
        /// By default, the shape menu is selected.
        /// </summary>
        internal void Initialize()
        {
            controls.ShapeButtonManager.clickEvent.AddListener(OpenShapeMenu);
            controls.ConfigButtonManager.clickEvent.AddListener(OpenLineMenu);

            controls.ShapeButton.interactable = false;
            controls.ShapeButtonManager.enabled = false;
        }

        /// <summary>
        /// Enables the shape-menu switch and restores the menu that is
        /// currently selected.
        /// </summary>
        internal void Enable()
        {
            controls.SwitchObject.SetActive(true);

            if (!controls.ShapeButton.interactable)
            {
                LineMenu.Instance.Disable();
                controls.MenuObject.SetActive(true);
                BindShapeMenu();
            }
            else
            {
                controls.MenuObject.SetActive(false);
                LineMenu.Instance.EnableForDrawing();
                BindLineMenu();
            }
        }

        /// <summary>
        /// Disables the shape menu, the line menu and their switch.
        /// </summary>
        internal void Disable()
        {
            controls.MenuObject.SetActive(false);
            LineMenu.Instance.Disable();
            controls.SwitchObject.SetActive(false);
        }

        /// <summary>
        /// Opens the line configuration menu and closes the shape menu.
        /// The line menu is configured for drawing or editing depending
        /// on whether a shape is currently being drawn.
        /// </summary>
        internal void OpenLineMenu()
        {
            controls.ConfigButton.interactable = false;
            controls.ConfigButtonManager.enabled = false;

            controls.ShapeButtonManager.enabled = true;
            controls.ShapeButton.interactable = true;

            if (DrawShapesAction.currentShape == null)
            {
                LineMenu.Instance.EnableForDrawing();
            }
            else
            {
                LineMenu.Instance.EnableForEditing(
                    DrawShapesAction.currentShape,
                    LineConf.Get(DrawShapesAction.currentShape));
            }

            MenuHelper.CalculateHeight(
                LineMenu.Instance.GameObject);

            BindLineMenu();

            controls.MenuObject.SetActive(false);
        }

        /// <summary>
        /// Opens the shape configuration menu and closes the line menu.
        /// </summary>
        private void OpenShapeMenu()
        {
            controls.ShapeButton.interactable = false;
            controls.ShapeButtonManager.enabled = false;

            controls.ConfigButton.interactable = true;
            controls.ConfigButtonManager.enabled = true;

            LineMenu.Instance.Disable();

            BindShapeMenu();

            controls.MenuObject.SetActive(true);
        }

        /// <summary>
        /// Binds the line configuration menu to the shape-menu switch.
        /// </summary>
        private void BindLineMenu()
        {
            LineMenu.Instance.GameObject.transform.SetParent(
                controls.SwitchContent);

            GameObject dragger =
                GameFinder.FindAttachedOrLocalDescendant(
                    LineMenu.Instance.GameObject,
                    "Dragger");

            dragger
                .GetComponent<WindowDragger>()
                .enabled = false;
        }

        /// <summary>
        /// Binds the shape configuration menu to the shape-menu switch.
        /// </summary>
        private void BindShapeMenu()
        {
            controls.MenuObject.transform.SetParent(
                controls.SwitchContent);

            controls.MenuDragger.enabled = false;
        }
    }
}
