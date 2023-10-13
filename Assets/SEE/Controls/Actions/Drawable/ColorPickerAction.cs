using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game;
using TMPro;
using HSVPicker;
using UnityEngine.UI;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action provides a Color Picker action.
    /// </summary>
    class ColorPickerAction : AbstractPlayerAction
    {
        /// <summary>
        /// The location where the color picker menu prefeb is placed.
        /// </summary>
        private const string colorPickerMenuPrefab = "Prefabs/UI/Drawable/ColorPickerMenu";

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
        /// The instance for the colorPickerMenu
        /// </summary>
        private GameObject colorPickerMenu;

        /// <summary>
        /// The HSV color picker to show the selected primary color.
        /// </summary>
        private ColorPicker pickerForPrimaryColor;

        /// <summary>
        /// The HSV color picker to show the selected second color.
        /// </summary>
        private ColorPicker pickerForSecondColor;

        /// <summary>
        /// Bool to identifiy if the action is running.
        /// </summary>
        private bool isInAction = false;

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.ColorPicker"/>.
        /// It pickes the chosen color of a drawable type object.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                bool pickForSecondColor = false;

                /// Block for picking the (main) color of the object.
                /// If left control is pressed the main color will be picked for the <see cref="ValueHolder.currentSecondColor"/>
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !isInAction &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject) &&
                    (raycastHit.collider.gameObject.CompareTag(Tags.Line) || raycastHit.collider.gameObject.CompareTag(Tags.DText)))
                {
                    isInAction = true;
                    GameObject hittedObject = raycastHit.collider.gameObject;

                    switch (hittedObject.tag)
                    {
                        case Tags.Line:
                            pickedColor = hittedObject.GetColor(); // TODO work with color kind ... 
                            break;
                        case Tags.DText:
                            pickedColor = hittedObject.GetComponent<TextMeshPro>().color;
                            break;
                    }

                    if (!Input.GetKey(KeyCode.LeftControl))
                    {
                        ValueHolder.currentPrimaryColor = pickedColor;
                        pickerForPrimaryColor.AssignColor(pickedColor);
                    }
                    else
                    {
                        pickForSecondColor = true;
                        pickerForSecondColor.AssignColor(pickedColor);
                        ValueHolder.currentSecondColor = pickedColor;
                    }
                    memento = new(oldChosenPrimaryColor, oldChosenSecondColor, pickedColor, pickForSecondColor);
                }

                /// Block for picking the second color of the object.
                /// /// If left control is pressed the second color will be loaded as main color
                if ((Input.GetMouseButtonDown(1) || Input.GetMouseButton(1)) && !isInAction &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit hit) &&
                    GameDrawableFinder.hasDrawable(hit.collider.gameObject) &&
                    (hit.collider.gameObject.CompareTag(Tags.Line) || hit.collider.gameObject.CompareTag(Tags.DText)))
                {
                    isInAction = true;
                    GameObject hittedObject = hit.collider.gameObject;

                    switch (hittedObject.tag)
                    {
                        case Tags.Line:
                            pickedColor = hittedObject.GetColor(); // TODO work with color kind ... 
                            break;
                        case Tags.DText:
                            pickedColor = hittedObject.GetComponent<TextMeshPro>().outlineColor;
                            break;
                    }
                    

                    if (!Input.GetKey(KeyCode.LeftControl))
                    {
                        pickerForPrimaryColor.AssignColor(pickedColor);
                        ValueHolder.currentPrimaryColor = pickedColor;
                    }
                    else
                    {
                        pickForSecondColor = true;
                        pickerForSecondColor.AssignColor(pickedColor);
                        ValueHolder.currentSecondColor = pickedColor;
                    }
                    memento = new(oldChosenPrimaryColor, oldChosenSecondColor, pickedColor, pickForSecondColor);
                }

                if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) && isInAction)
                {
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// At beginning of this action, it saves the current color values 
        /// (<see cref="ValueHolder.currentPrimaryColor>"/> and <see cref="ValueHolder.currentSecondColor"/> of the <see cref="ValueHolder"/>.
        /// Then it loads the color picker menu.
        /// </summary>
        public override void Awake()
        {
            oldChosenPrimaryColor = ValueHolder.currentPrimaryColor;
            oldChosenSecondColor = ValueHolder.currentSecondColor;
            colorPickerMenu = PrefabInstantiator.InstantiatePrefab(colorPickerMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            pickerForPrimaryColor = colorPickerMenu.transform.Find("Primary").GetComponent<HSVPicker.ColorPicker>();
            pickerForPrimaryColor.AssignColor(ValueHolder.currentPrimaryColor);
            pickerForSecondColor = colorPickerMenu.transform.Find("Second").GetComponent<HSVPicker.ColorPicker>();
            pickerForSecondColor.AssignColor(ValueHolder.currentSecondColor);
        }

        /// <summary>
        /// Stops the <see cref="ColorPickerAction"/> and deletes the color picker menu.
        /// </summary>
        public override void Stop()
        {
            Destroyer.Destroy(colorPickerMenu);
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
            /// The old chosen <see cref="ValueHolder.currentSecondColor"/>
            /// </summary>
            public readonly Color oldChosenSecondColor;
            /// <summary>
            /// The picked color
            /// </summary>
            public readonly Color pickedColor;
            /// <summary>
            /// A boolean representing that the selected color has been picked for the <see cref="ValueHolder.currentSecondColor"/>
            /// </summary>
            public readonly bool pickForSecondColor;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="oldChosenPrimaryColor">The old chosen <see cref="ValueHolder.currentPrimaryColor"/></param>
            /// <param name="oldChosenSecondColor">The old chosen <see cref="ValueHolder.currentSecondColor"/></param>
            /// <param name="pickedColor">The picked color</param>
            /// <param name="pickForSecondColor">Color was picked for <see cref="ValueHolder.currentSecondColor"/></param>
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
            ValueHolder.currentSecondColor = memento.oldChosenSecondColor;
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
            }
            else
            {
                ValueHolder.currentSecondColor = memento.pickedColor;
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
