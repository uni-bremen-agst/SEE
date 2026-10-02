using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable.Line;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

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

            GameObject currentPreviewShape = ShapeMenu.GetCurrentPreviewShape();

            if (currentPreviewShape == null)
            {
                LineMenu.Instance.EnableForDrawing();
            }
            else
            {
                LineConf currentPreviewConf = LineConf.GetLine(currentPreviewShape);

                if (currentPreviewConf == null)
                {
                    LineMenu.Instance.EnableForDrawing();
                }
                else
                {
                    LineMenu.Instance.EnableForEditing(
                        currentPreviewShape,
                        currentPreviewConf,
                        lineCapsChanged: SynchronizePreviewLineCaps);
                }
            }

            BindLineMenu();

            controls.MenuObject.SetActive(false);
        }

        /// <summary>
        /// Synchronizes edited preview line caps with the shape-menu state.
        /// </summary>
        /// <param name="line">The updated preview line configuration.</param>
        private static void SynchronizePreviewLineCaps(LineConf line)
        {
            if (line == null)
            {
                return;
            }

            ShapeMenu.SetLineCaps(
                PreserveReferenceSelection(
                    ShapeMenu.GetLineStartCapConf(),
                    line.LineCapStart),
                PreserveReferenceSelection(
                    ShapeMenu.GetLineEndCapConf(),
                    line.LineCapEnd));
        }

        /// <summary>
        /// Preserves a logical <see cref="LineCap.Reference"/> selection while its
        /// rendered representation is exposed by the line editor as an arrow.
        /// </summary>
        /// <param name="selectedCap">The cap currently selected in the shape menu.</param>
        /// <param name="editedCap">The cap configuration reported by the line editor.</param>
        /// <returns>The cap configuration that should be stored in the shape menu.</returns>
        private static LineCapConf PreserveReferenceSelection(
            LineCapConf selectedCap,
            LineCapConf editedCap)
        {
            if (selectedCap?.CapKind == LineCap.Reference
                && editedCap?.CapKind == LineCap.Arrow)
            {
                return selectedCap;
            }

            return editedCap;
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
