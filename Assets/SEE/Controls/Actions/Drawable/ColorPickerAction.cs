using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game;
using TMPro;
using HSVPicker;
using UnityEngine.UI;
using SEE.Game.Drawable.Configurations;
using Crosstales;
using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action provides a Color Picker action.
    /// </summary>
    class ColorPickerAction : AbstractPlayerAction
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
        /// The new picked color.
        /// </summary>
        private Color pickedColor;

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Bool to identifiy if the action is running.
        /// </summary>
        private bool isInAction = false;

        /// <summary>
        /// Bool to identifiy if the action waits for user input of the
        /// mind map color picker menu.
        /// </summary>
        private bool waitForHelperMenu = false;

        /// <summary>
        /// Bool to identify that the color picking from mind map node is finish.
        /// </summary>
        private bool finishChosingMMColor = false;

        /// <summary>
        /// Bool to identifiy if the picker picks a color for the second color.
        /// </summary>
        private bool pickForSecondColor = false;

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.ColorPicker"/>.
        /// It pickes the chosen color of a drawable type object.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// Block for picking the (main) color of the object.
                /// If left control is pressed the main color will be picked for the <see cref="ValueHolder.currentSecondaryColor"/>
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !isInAction &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    (GameFinder.hasDrawable(raycastHit.collider.gameObject) ||
                        raycastHit.collider.gameObject.CompareTag(Tags.Drawable) && 
                        GameFinder.GetHighestParent(raycastHit.collider.gameObject).name.StartsWith(ValueHolder.StickyNotePrefix)))
                {
                    isInAction = true;
                    GameObject hittedObject = raycastHit.collider.gameObject;

                    switch (hittedObject.tag)
                    {
                        case Tags.Line:
                            LineConf line = LineConf.GetLine(hittedObject);
                            pickedColor = line.primaryColor;
                            break;
                        case Tags.DText:
                            pickedColor = hittedObject.GetComponent<TextMeshPro>().color;
                            break;
                        case Tags.Image:
                            ImageConf image = ImageConf.GetImageConf(hittedObject);
                            pickedColor = image.imageColor;
                            break;
                        case Tags.MindMapNode:
                            ColorPickerMindMapMenu.Enable(hittedObject, true);
                            waitForHelperMenu = true;
                            break;
                        case Tags.Drawable:
                            DrawableConfig config = DrawableConfigManager.GetDrawableConfig(hittedObject);
                            pickedColor = config.Color;
                            break;
                    }
                }

                /// Block for picking the second color of the object.
                /// /// If left control is pressed the second color will be loaded as main color
                if ((Input.GetMouseButtonDown(1) || Input.GetMouseButton(1)) && !isInAction &&
                    Raycasting.RaycastAnything(out RaycastHit hit) &&
                    (GameFinder.hasDrawable(hit.collider.gameObject) ||
                        hit.collider.gameObject.CompareTag(Tags.Drawable) &&
                        GameFinder.GetHighestParent(hit.collider.gameObject).name.StartsWith(ValueHolder.StickyNotePrefix)))
                {
                    isInAction = true;
                    GameObject hittedObject = hit.collider.gameObject;

                    switch (hittedObject.tag)
                    {
                        case Tags.Line:
                            LineConf line = LineConf.GetLine(hittedObject);
                            pickedColor = line.secondaryColor;
                            if (line.colorKind == GameDrawer.ColorKind.Monochrome)
                            {
                                pickedColor = line.primaryColor;
                            }
                            break;
                        case Tags.DText:
                            pickedColor = hittedObject.GetComponent<TextMeshPro>().outlineColor;
                            break;
                        case Tags.Image:
                            ImageConf image = ImageConf.GetImageConf(hittedObject);
                            pickedColor = image.imageColor;
                            break;
                        case Tags.MindMapNode:
                            ColorPickerMindMapMenu.Enable(hittedObject, false);
                            waitForHelperMenu = true;
                            break;
                        case Tags.Drawable:
                            DrawableConfig config = DrawableConfigManager.GetDrawableConfig(hittedObject);
                            pickedColor = config.Color;
                            break;
                    }
                }

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
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }
            }
            if (waitForHelperMenu && ColorPickerMindMapMenu.TryGetColor(out Color color))
            {
                pickedColor = color;
                waitForHelperMenu = false;
                finishChosingMMColor = true;
            }
            return false;
        }

        /// <summary>
        /// At beginning of this action, it saves the current color values 
        /// (<see cref="ValueHolder.currentPrimaryColor>"/> and <see cref="ValueHolder.currentSecondaryColor"/> of the <see cref="ValueHolder"/>.
        /// It also adds the UICanvas a color picker menu disabler. 
        /// This is required to prevent a display error when displaying the color picker menu. 
        /// (When it is displayed, the switch status is initialized once. It is activated once and then returns to its original position.)
        /// Then it enables the color picker menu.
        /// </summary>
        public override void Awake()
        {
            oldChosenPrimaryColor = ValueHolder.currentPrimaryColor;
            oldChosenSecondColor = ValueHolder.currentSecondaryColor;
            if (GameObject.Find("UI Canvas").GetComponent<ColorPickerMenuDisabler>() == null)
            {
                GameObject.Find("UI Canvas").AddComponent<ColorPickerMenuDisabler>();
                ColorPickerMenu.Enable();
            }
        }

        /// <summary>
        /// Destroys the mind map color picker menu on action stop.
        /// </summary>
        public override void Stop()
        {
            ColorPickerMindMapMenu.Disable();
        }

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="ColorPickerAction"/>.
        /// </summary>
        struct Memento
        {
            /// <summary>
            /// The old chosen <see cref="ValueHolder.currentPrimaryColor"/>
            /// </summary>
            public readonly Color oldChosenPrimaryColor;
            /// <summary>
            /// The old chosen <see cref="ValueHolder.currentSecondaryColor"/>
            /// </summary>
            public readonly Color oldChosenSecondColor;
            /// <summary>
            /// The picked color
            /// </summary>
            public readonly Color pickedColor;
            /// <summary>
            /// A boolean representing that the selected color has been picked for the <see cref="ValueHolder.currentSecondaryColor"/>
            /// </summary>
            public readonly bool pickForSecondColor;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="oldChosenPrimaryColor">The old chosen <see cref="ValueHolder.currentPrimaryColor"/></param>
            /// <param name="oldChosenSecondColor">The old chosen <see cref="ValueHolder.currentSecondaryColor"/></param>
            /// <param name="pickedColor">The picked color</param>
            /// <param name="pickForSecondColor">Color was picked for <see cref="ValueHolder.currentSecondaryColor"/></param>
            /// 
            public Memento(Color oldChosenPrimaryColor, Color oldChosenSecondColor, Color pickedColor, bool pickForSecondColor)
            {
                this.oldChosenPrimaryColor = oldChosenPrimaryColor;
                this.oldChosenSecondColor = oldChosenSecondColor;
                this.pickedColor = pickedColor;
                this.pickForSecondColor = pickForSecondColor;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., restores the original color of the <see cref="ValueHolder"/>
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            ValueHolder.currentPrimaryColor = memento.oldChosenPrimaryColor;
            ColorPickerMenu.AssignPrimaryColor(ValueHolder.currentPrimaryColor);
            ValueHolder.currentSecondaryColor = memento.oldChosenSecondColor;
            ColorPickerMenu.AssignSecondaryColor(ValueHolder.currentSecondaryColor);
        }

        /// <summary>
        /// Repeats this action, i.e., saves the picked color again in the <see cref="ValueHolder"/>
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (!memento.pickForSecondColor)
            {
                ValueHolder.currentPrimaryColor = memento.pickedColor;
                ColorPickerMenu.AssignPrimaryColor(ValueHolder.currentPrimaryColor);
            }
            else
            {
                ValueHolder.currentSecondaryColor = memento.pickedColor;
                ColorPickerMenu.AssignSecondaryColor(ValueHolder.currentSecondaryColor);
            }
        }

        /// <summary>
        /// A new instance of <see cref="ColorPickerAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ColorPickerAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new ColorPickerAction();
        }

        /// <summary>
        /// A new instance of <see cref="ColorPickerAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ColorPickerAction"/></returns>
        public override ReversibleAction NewInstance()
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
