using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides a color picker action for <see cref="DrawableType"/> objects.
    /// </summary>
    class ColorPickerAction : DrawableAction
    {
        /// <summary>
        /// The old chosen primary color of the <see cref="ValueHolder"/>
        /// </summary>
        private Color oldChosenPrimaryColor;

        /// <summary>
        /// The old chosen second color of the <see cref="ValueHolder"/>
        /// </summary>
        private Color oldChosenSecondColor;

        /// <summary>
        /// The newly picked color.
        /// </summary>
        private Color pickedColor;

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Trie if the action is running.
        /// </summary>
        private bool isInAction = false;

        /// <summary>
        /// True if the action waits for user input of the
        /// mind map color picker menu.
        /// </summary>
        private bool waitForHelperMenu = false;

        /// <summary>
        /// True if the color picking from a mind map node is finished.
        /// </summary>
        private bool finishChosingMMColor = false;

        /// <summary>
        /// True if the picker picks a color for the second color.
        /// </summary>
        private bool pickForSecondColor = false;

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.ColorPicker"/>.
        /// It picks the chosen color of a drawable type object.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// Block for picking the primary color of the chosen object.
                /// Will be executed via left mouse click.
                PickingPrimaryColor();

                /// Block for picking the secondary color of the chosen object.
                /// Will be executed via right mouse click.
                PickingSecondaryColor();

                /// Ends the action.
                /// Loads the selected color, depending on the option chosen in the ColorPickerMenu,
                /// either into <see cref="ValueHolder.currentPrimaryColor"/> or
                /// <see cref="ValueHolder.currentSecondaryColor"/>.
                /// Subsequently, a memento is created, and the action process is completed.
                if (((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) && isInAction && !waitForHelperMenu) ||
                    finishChosingMMColor)
                {
                    if (!ColorPickerMenu.GetSwitchStatus())
                    {
                        ValueHolder.currentPrimaryColor = pickedColor;
                        ColorPickerMenu.AssignPrimaryColor(pickedColor);
                    }
                    else
                    {
                        pickForSecondColor = true;
                        ColorPickerMenu.AssignSecondaryColor(pickedColor);
                        ValueHolder.currentSecondaryColor = pickedColor;
                    }
                    memento = new(oldChosenPrimaryColor, oldChosenSecondColor, pickedColor, pickForSecondColor);
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                }
            }

            /// This block is waiting for user input to select a Mind Map element
            /// of <see cref="ColorPickerMindMapMenu"/>.
            /// It is placed outside the <see cref="Raycasting.IsMouseOverGUI"/> block so
            /// that the input can be immediately detected.
            if (waitForHelperMenu && ColorPickerMindMapMenu.TryGetColor(out Color color))
            {
                pickedColor = color;
                waitForHelperMenu = false;
                finishChosingMMColor = true;
            }
            return false;
        }

        /// <summary>
        /// Picks the primary color of the chosen drawable type object or sticky note.
        /// The left mouse button is used for selection.
        /// </summary>
        private void PickingPrimaryColor()
        {
            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !isInAction
                && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && (GameFinder.HasDrawable(raycastHit.collider.gameObject) ||
                    raycastHit.collider.gameObject.CompareTag(Tags.Drawable) &&
                    GameFinder.GetHighestParent(raycastHit.collider.gameObject).name.
                        StartsWith(ValueHolder.StickyNotePrefix)))
            {
                isInAction = true;
                GameObject hitObject = raycastHit.collider.gameObject;

                switch (hitObject.tag)
                {
                    case Tags.Line:
                        LineConf line = LineConf.GetLine(hitObject);
                        pickedColor = line.PrimaryColor;
                        break;
                    case Tags.DText:
                        pickedColor = hitObject.GetComponent<TextMeshPro>().color;
                        break;
                    case Tags.Image:
                        ImageConf image = ImageConf.GetImageConf(hitObject);
                        pickedColor = image.ImageColor;
                        break;
                    case Tags.MindMapNode:
                        ColorPickerMindMapMenu.Enable(hitObject, true);
                        waitForHelperMenu = true;
                        break;
                    case Tags.Drawable:
                        DrawableConfig config = DrawableConfigManager.GetDrawableConfig(hitObject);
                        pickedColor = config.Color;
                        break;
                }
            }
        }

        /// <summary>
        /// Picks the secondary color of the chosen drawable type object or sticky note.
        /// The right mouse button is used for selection.
        /// </summary>
        private void PickingSecondaryColor()
        {
            if ((Input.GetMouseButtonDown(1) || Input.GetMouseButton(1)) && !isInAction
                && Raycasting.RaycastAnything(out RaycastHit hit)
                && (GameFinder.HasDrawable(hit.collider.gameObject) ||
                    hit.collider.gameObject.CompareTag(Tags.Drawable) &&
                    GameFinder.GetHighestParent(hit.collider.gameObject).name.
                        StartsWith(ValueHolder.StickyNotePrefix)))
            {
                isInAction = true;
                GameObject hitObject = hit.collider.gameObject;

                switch (hitObject.tag)
                {
                    case Tags.Line:
                        LineConf line = LineConf.GetLine(hitObject);
                        pickedColor = line.SecondaryColor;
                        if (line.ColorKind == GameDrawer.ColorKind.Monochrome)
                        {
                            pickedColor = line.PrimaryColor;
                        }
                        break;
                    case Tags.DText:
                        pickedColor = hitObject.GetComponent<TextMeshPro>().outlineColor;
                        break;
                    case Tags.Image:
                        ImageConf image = ImageConf.GetImageConf(hitObject);
                        pickedColor = image.ImageColor;
                        break;
                    case Tags.MindMapNode:
                        ColorPickerMindMapMenu.Enable(hitObject, false);
                        waitForHelperMenu = true;
                        break;
                    case Tags.Drawable:
                        DrawableConfig config = DrawableConfigManager.GetDrawableConfig(hitObject);
                        pickedColor = config.Color;
                        break;
                }
            }
        }

        /// <summary>
        /// At the beginning of this action, it saves the current color values
        /// (<see cref="ValueHolder.currentPrimaryColor>"/> and <see cref="ValueHolder.currentSecondaryColor"/>)
        /// of the <see cref="ValueHolder"/>.
        /// It also adds to the UICanvas a <see cref="ColorPickerMenuDisabler"/> component.
        /// This is required to prevent a display error when displaying the color picker menu.
        /// (When it is displayed, the switch status is initialized once.
        ///     It is activated once and then returns to its original position.)
        /// Then it enables the color picker menu.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            oldChosenPrimaryColor = ValueHolder.currentPrimaryColor;
            oldChosenSecondColor = ValueHolder.currentSecondaryColor;
            Canvas.AddOrGetComponent<ColorPickerMenuDisabler>();
            ColorPickerMenu.Enable();
        }

        /// <summary>
        /// Destroys the mind map color picker menu on action stop.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            ColorPickerMindMapMenu.Disable();
        }

        /// <summary>
        /// This struct can store all the information needed to
        /// revert or repeat a <see cref="ColorPickerAction"/>.
        /// </summary>
        readonly struct Memento
        {
            /// <summary>
            /// The old chosen <see cref="ValueHolder.currentPrimaryColor"/>
            /// </summary>
            public readonly Color OldChosenPrimaryColor;
            /// <summary>
            /// The old chosen <see cref="ValueHolder.currentSecondaryColor"/>
            /// </summary>
            public readonly Color OldChosenSecondColor;
            /// <summary>
            /// The picked color
            /// </summary>
            public readonly Color PickedColor;
            /// <summary>
            /// A boolean representing that the selected color has been picked
            /// for the <see cref="ValueHolder.currentSecondaryColor"/>
            /// </summary>
            public readonly bool PickForSecondColor;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="oldChosenPrimaryColor">The old chosen
            ///     <see cref="ValueHolder.currentPrimaryColor"/></param>
            /// <param name="oldChosenSecondColor">The old chosen
            ///     <see cref="ValueHolder.currentSecondaryColor"/></param>
            /// <param name="pickedColor">The picked color</param>
            /// <param name="pickForSecondColor">Color was picked for
            ///     <see cref="ValueHolder.currentSecondaryColor"/></param>
            ///
            public Memento(Color oldChosenPrimaryColor, Color oldChosenSecondColor, Color pickedColor,
                bool pickForSecondColor)
            {
                OldChosenPrimaryColor = oldChosenPrimaryColor;
                OldChosenSecondColor = oldChosenSecondColor;
                PickedColor = pickedColor;
                PickForSecondColor = pickForSecondColor;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., restores the original color of the <see cref="ValueHolder"/>
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            ValueHolder.currentPrimaryColor = memento.OldChosenPrimaryColor;
            ColorPickerMenu.AssignPrimaryColor(ValueHolder.currentPrimaryColor);
            ValueHolder.currentSecondaryColor = memento.OldChosenSecondColor;
            ColorPickerMenu.AssignSecondaryColor(ValueHolder.currentSecondaryColor);
        }

        /// <summary>
        /// Repeats this action, i.e., saves the picked color again in the <see cref="ValueHolder"/>
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (!memento.PickForSecondColor)
            {
                ValueHolder.currentPrimaryColor = memento.PickedColor;
                ColorPickerMenu.AssignPrimaryColor(ValueHolder.currentPrimaryColor);
            }
            else
            {
                ValueHolder.currentSecondaryColor = memento.PickedColor;
                ColorPickerMenu.AssignSecondaryColor(ValueHolder.currentSecondaryColor);
            }
        }

        /// <summary>
        /// A new instance of <see cref="ColorPickerAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ColorPickerAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new ColorPickerAction();
        }

        /// <summary>
        /// A new instance of <see cref="ColorPickerAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ColorPickerAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ColorPicker"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.ColorPicker;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object,
        /// an empty set is always returned.
        /// </summary>
        /// <returns>a empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }
    }
}
