using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
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
        /// True if the action waits for user input of a
        /// sub color picker menu.
        /// </summary>
        private bool waitForHelperMenu = false;

        /// <summary>
        /// True if the color picking from a sub menu is finished.
        /// </summary>
        private bool finishChosingColor = false;

        /// <summary>
        /// True if the picker picks a color for the second color.
        /// </summary>
        private bool pickForSecondColor = false;

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.ColorPicker"/>.
        /// It picks the chosen color of a drawable type object.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
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
                /// either into <see cref="ValueHolder.CurrentPrimaryColor"/> or
                /// <see cref="ValueHolder.CurrentSecondaryColor"/>.
                /// Subsequently, a memento is created, and the action process is completed.
                if (((SEEInput.MouseUp(MouseButton.Left) || SEEInput.MouseUp(MouseButton.Right))
                    && isInAction && !waitForHelperMenu)
                    || finishChosingColor)
                {
                    if (!ColorPickerMenu.Instance.GetSwitchStatus())
                    {
                        ValueHolder.CurrentPrimaryColor = pickedColor;
                        ColorPickerMenu.Instance.AssignPrimaryColor(pickedColor);
                    }
                    else
                    {
                        pickForSecondColor = true;
                        ColorPickerMenu.Instance.AssignSecondaryColor(pickedColor);
                        ValueHolder.CurrentSecondaryColor = pickedColor;
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
            if (waitForHelperMenu && ColorPickerMindMapMenu.Instance.TryGetColor(out Color color))
            {
                pickedColor = color;
                waitForHelperMenu = false;
                finishChosingColor = true;
            }

            /// This block is waiting for user input to select a line element
            /// of <see cref="ColorPickerLineMenu"/>.
            /// It is placed outside the <see cref="Raycasting.IsMouseOverGUI"/> block so
            /// that the input can be immediately detected.
            if (waitForHelperMenu && ColorPickerLineMenu.TryGetColor(out Color color1))
            {
                pickedColor = color1;
                waitForHelperMenu = false;
                finishChosingColor = true;
            }
            return false;
        }

        /// <summary>
        /// Picks the primary color of the chosen drawable type object or sticky note.
        /// The left mouse button is used for selection.
        /// </summary>
        private void PickingPrimaryColor()
        {
            if (Selector.SelectQueryHasOrIsDrawableSurface(out RaycastHit raycastHit)
                && !isInAction)
            {
                isInAction = true;
                GameObject hitObject = raycastHit.collider.gameObject;

                /// Check for the case that a child object of a drawable type object was clicked.
                if (!Tags.DrawableTypes.Contains(hitObject.tag)
                    && !hitObject.CompareTag(Tags.Drawable)
                    && hitObject.transform.parent != null)
                {
                    hitObject = hitObject.transform.parent.gameObject;
                }

                switch (hitObject.tag)
                {
                    case Tags.Line:
                        LineConf line = LineConf.GetLine(hitObject);
                        pickedColor = line.PrimaryColor;
                        if (line.ColorKind != GameDrawer.ColorKind.Monochrome && line.FillOutStatus)
                        {
                            ColorPickerLineMenu.Enable(hitObject);
                            waitForHelperMenu = true;
                        }
                        break;
                    case Tags.DText:
                        pickedColor = hitObject.GetComponent<TextMeshPro>().color;
                        break;
                    case Tags.Image:
                        ImageConf image = ImageConf.GetImageConf(hitObject);
                        pickedColor = image.ImageColor;
                        break;
                    case Tags.MindMapNode:
                        ColorPickerMindMapMenu.Instance.Enable(hitObject, true);
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
            if (!isInAction
                && Selector.SelectQueryHasOrIsDrawableSurface(out RaycastHit raycastHit, false))
            {
                isInAction = true;
                GameObject hitObject = raycastHit.collider.gameObject;

                /// Check for the case that a child object of a drawable type object was clicked.
                if (!Tags.DrawableTypes.Contains(hitObject.tag)
                    && !hitObject.CompareTag(Tags.Drawable)
                    && hitObject.transform.parent != null)
                {
                    hitObject = hitObject.transform.parent.gameObject;
                }

                switch (hitObject.tag)
                {
                    case Tags.Line:
                        LineConf line = LineConf.GetLine(hitObject);
                        pickedColor = line.SecondaryColor;
                        if (line.ColorKind == GameDrawer.ColorKind.Monochrome)
                        {
                            pickedColor = line.PrimaryColor;
                        }
                        if (line.ColorKind != GameDrawer.ColorKind.Monochrome && line.FillOutStatus)
                        {
                            ColorPickerLineMenu.Enable(hitObject);
                            waitForHelperMenu = true;
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
                        ColorPickerMindMapMenu.Instance.Enable(hitObject, false);
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
        /// Saves the current primary and secondary colors when this action starts.
        /// These values are required to undo a completed color pick later.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            oldChosenPrimaryColor = ValueHolder.CurrentPrimaryColor;
            oldChosenSecondColor = ValueHolder.CurrentSecondaryColor;
        }

        /// <summary>
        /// This struct can store all the information needed to
        /// revert or repeat a <see cref="ColorPickerAction"/>.
        /// </summary>
        readonly struct Memento
        {
            /// <summary>
            /// The old chosen <see cref="ValueHolder.CurrentPrimaryColor"/>
            /// </summary>
            public readonly Color OldChosenPrimaryColor;
            /// <summary>
            /// The old chosen <see cref="ValueHolder.CurrentSecondaryColor"/>
            /// </summary>
            public readonly Color OldChosenSecondColor;
            /// <summary>
            /// The picked color
            /// </summary>
            public readonly Color PickedColor;
            /// <summary>
            /// A boolean representing that the selected color has been picked
            /// for the <see cref="ValueHolder.CurrentSecondaryColor"/>
            /// </summary>
            public readonly bool PickForSecondColor;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="oldChosenPrimaryColor">The old chosen
            ///     <see cref="ValueHolder.CurrentPrimaryColor"/>.</param>
            /// <param name="oldChosenSecondColor">The old chosen
            ///     <see cref="ValueHolder.CurrentSecondaryColor"/>.</param>
            /// <param name="pickedColor">The picked color.</param>
            /// <param name="pickForSecondColor">Color was picked for
            ///     <see cref="ValueHolder.CurrentSecondaryColor"/>.</param>
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
            ValueHolder.CurrentPrimaryColor = memento.OldChosenPrimaryColor;
            ColorPickerMenu.Instance.AssignPrimaryColor(ValueHolder.CurrentPrimaryColor);
            ValueHolder.CurrentSecondaryColor = memento.OldChosenSecondColor;
            ColorPickerMenu.Instance.AssignSecondaryColor(ValueHolder.CurrentSecondaryColor);
        }

        /// <summary>
        /// Repeats this action, i.e., saves the picked color again in the <see cref="ValueHolder"/>
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (!memento.PickForSecondColor)
            {
                ValueHolder.CurrentPrimaryColor = memento.PickedColor;
                ColorPickerMenu.Instance.AssignPrimaryColor(ValueHolder.CurrentPrimaryColor);
            }
            else
            {
                ValueHolder.CurrentSecondaryColor = memento.PickedColor;
                ColorPickerMenu.Instance.AssignSecondaryColor(ValueHolder.CurrentSecondaryColor);
            }
        }

        /// <summary>
        /// A new instance of <see cref="ColorPickerAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="ColorPickerAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new ColorPickerAction();
        }

        /// <summary>
        /// A new instance of <see cref="ColorPickerAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>New instance of <see cref="ColorPickerAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ColorPicker"/>.</returns>
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
        /// <returns>An empty set.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }
    }
}
