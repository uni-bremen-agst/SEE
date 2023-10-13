using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static SEE.Game.GameDrawer;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game.Drawable.Configurations;
using Text = SEE.Game.Drawable.Configurations.Text;
using UnityEngine.Events;
using SEE.Game.UI.PropertyDialog.Drawable;
using SEE.Game.UI.Notification;
using Toggle = UnityEngine.UI.Toggle;
using TMPro;

namespace SEE.Controls.Actions.Drawable
{
    public class EditAction : AbstractPlayerAction
    {
        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.SelectObject;

        /// <summary>
        /// The progress states of the <see cref="EditAction"/>
        /// </summary>
        private enum ProgressState
        {
            SelectObject,
            OpenEditMenu,
            Edit,
            Finish
        }

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="EditAction"/>
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="EditAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// Is the selected drawable type object that should be edit.
            /// </summary>
            public GameObject selectedObj;
            /// <summary>
            /// The old values of the drawable type object.
            /// </summary>
            public readonly DrawableType oldValueHolder;
            /// <summary>
            /// The new edited values of the drawable type object.
            /// </summary>
            public readonly DrawableType newValueHolder;
            /// <summary>
            /// The drawable on that the drawable type object is displayed.
            /// </summary>
            public readonly GameObject drawable;
            /// <summary>
            /// The id of the drawable type object.
            /// </summary>
            public readonly string id;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="obj">Is the selected drawable type object that should be edit</param>
            /// <param name="oldValueHolder">The old values of the drawable type object.</param>
            /// <param name="newValueHolder">The new edited values of the drawable type object.</param>
            /// <param name="drawable">The drawable on that the drawable type object is displayed.</param>
            /// <param name="id">The id of the drawable type object.</param>
            public Memento(GameObject obj, DrawableType oldValueHolder,
                DrawableType newValueHolder, GameObject drawable, string id)
            {
                this.selectedObj = obj;
                this.oldValueHolder = oldValueHolder;
                this.newValueHolder = newValueHolder;
                this.drawable = drawable;
                this.id = id;
            }
        }
        /// <summary>
        /// The selected drawable type object that should be edit.
        /// </summary>
        private GameObject selectedObj;

        /// <summary>
        /// The selected object of the last run.
        /// </summary>
        private static GameObject oldSelectedObj;

        /// <summary>
        /// Bool that represents that the left mouse button was released after finish.
        /// It is necessary to prevent the previously selected object from being accidentally selected again. 
        /// After the action has successfully completed, it starts again, allowing for the selection of a new object. 
        /// This option enables the immediate selection of another object while holding down the mouse button.
        /// </summary>
        private static bool mouseWasReleased = true;

        /// <summary>
        /// The old values of the selected drawable type.
        /// </summary>
        private DrawableType oldValueHolder;

        /// <summary>
        /// The new edited values of the selected drawable type.
        /// </summary>
        private DrawableType newValueHolder;

        /// <summary>
        /// The HSV color picker for the line menu.
        /// </summary>
        private HSVPicker.ColorPicker picker;

        /// <summary>
        /// The layer slider controller for the line menu.
        /// </summary>
        private LayerSliderController layerSlider;

        /// <summary>
        /// The thickness slider controller for the line menu.
        /// </summary>
        private ThicknessSliderController thicknessSlider;

        /// <summary>
        /// Resets the old selected object, if the action state will leave.
        /// </summary>
        public static void Reset()
        {
            oldSelectedObj = null;
            mouseWasReleased = true;
        }

        /// <summary>
        /// This method checks if there are any changes in the editable values.
        /// </summary>
        /// <param name="oldHolder">The holder for the old values.</param>
        /// <param name="newHolder">The holder for the new values.</param>
        /// <returns></returns>
        private bool CheckEquals(DrawableType oldHolder, DrawableType newHolder)
        {
            if (oldHolder is Line oldLineHolder && newHolder is Line newLineHolder)
            {
                return oldLineHolder.color.Equals(newLineHolder.color) &&
                    oldLineHolder.orderInLayer.Equals(newLineHolder.orderInLayer) &&
                    oldLineHolder.thickness.Equals(newLineHolder.thickness) &&
                    oldLineHolder.loop.Equals(newLineHolder.loop) &&
                    oldLineHolder.lineKind.Equals(newLineHolder.lineKind) &&
                    oldLineHolder.tiling.Equals(newLineHolder.tiling);
            }
            if (oldHolder is Text oldTextHolder && newHolder is Text newTextHolder)
            {
                return oldTextHolder.text.Equals(newTextHolder.text) &&
                    oldTextHolder.fontSize.Equals(newTextHolder.fontSize) &&
                    oldTextHolder.orderInLayer.Equals(newTextHolder.orderInLayer) &&
                    oldTextHolder.fontStyles.Equals(newTextHolder.fontStyles) &&
                    oldTextHolder.fontColor.Equals(newTextHolder.fontColor) &&
                    oldTextHolder.outlineColor.Equals(newTextHolder.outlineColor) &&
                    oldTextHolder.outlineThickness.Equals(newTextHolder.outlineThickness);
            }
            return false;
        }

        /// <summary>
        /// Deactivates the blink effect if, it is still active and hides the text and line menu.
        /// If the action was not completed in full, the changes are reset.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            if (selectedObj != null && selectedObj.GetComponent<BlinkEffect>() != null)
            {
                selectedObj.GetComponent<BlinkEffect>().Deactivate();
            }
            if (progressState != ProgressState.Finish && selectedObj != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(selectedObj);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (oldValueHolder is Line oldLineHolder)
                {
                    GameEdit.ChangeLine(selectedObj, oldLineHolder);
                    new EditLineNetAction(drawable.name, drawableParent, oldLineHolder).Execute();
                }
                if (oldValueHolder is Text oldTextHolder)
                {
                    GameEdit.ChangeText(selectedObj, oldTextHolder);
                    new EditTextNetAction(drawable.name, drawableParent, oldTextHolder).Execute();
                }
            }
            TextMenu.disableTextMenu();
            LineMenu.disableLineMenu();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Edit"/>.
        /// It allows editing of the drawable type objects.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// This block allows the selection of a drawable type object for editing, taking into account the object edited in the last run. 
                    /// It prevents the same object from being accidentally selected again when the left mouse button is not released. 
                    /// Therefore, after the last action has been successfully completed, the left mouse button must be released to select the same object again. 
                    /// Additionally, a ValueResetter component is added to the UI Canvas to reset the two static variables after exiting this action type.
                    case ProgressState.SelectObject:
                        if (Input.GetMouseButtonUp(0))
                        {
                            mouseWasReleased = true;
                        }
                        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj == null &&
                            Raycasting.RaycastAnything(out RaycastHit raycastHit) && 
                            (oldSelectedObj == null || oldSelectedObj != raycastHit.collider.gameObject || (oldSelectedObj == raycastHit.collider.gameObject && mouseWasReleased)) &&
                            Tags.DrawableTypes.Contains(raycastHit.collider.gameObject.tag))
                        {
                            selectedObj = raycastHit.collider.gameObject;
                            oldSelectedObj = selectedObj;
                            oldValueHolder = new DrawableType().Get(selectedObj);
                            newValueHolder = new DrawableType().Get(selectedObj);
                            BlinkEffect effect = selectedObj.AddOrGetComponent<BlinkEffect>();
                            effect.SetAllowedActionStateType(GetActionStateType());

                            if (GameObject.Find("UI Canvas").GetComponent<ValueResetter>() == null)
                            {
                                GameObject.Find("UI Canvas").AddComponent<ValueResetter>().SetAllowedState(GetActionStateType());
                            }
                            progressState = ProgressState.OpenEditMenu;
                        }
                        
                        break;
                    /// In this block the right menu for the chosen drawable type will be opened.
                    case ProgressState.OpenEditMenu:
                        if (selectedObj.CompareTag(Tags.Line))
                        {
                            enableLineMenu(selectedObj);
                        }
                        if (selectedObj.CompareTag(Tags.DText))
                        {
                            enableTextMenu(selectedObj);
                        }
                        if (Input.GetMouseButtonUp(0))
                        {
                            progressState = ProgressState.Edit;
                        }
                        
                        break;
                    /// This block provides the completion of the action. As soon as the left mouse button is pressed, the completion is initiated.
                    case ProgressState.Edit:
                        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj.GetComponent<BlinkEffect>() != null)
                        {
                            selectedObj.GetComponent<BlinkEffect>().Deactivate();
                            progressState = ProgressState.Finish;
                        }
                        break;
                    /// This block completes or resets this action.
                    /// If no changes were made, it resets.
                    /// If there are changes the action will be successfull completed.
                    case ProgressState.Finish:
                        mouseWasReleased = false;
                        if (!CheckEquals(oldValueHolder, newValueHolder))
                        {
                            memento = new Memento(selectedObj, oldValueHolder, newValueHolder,
                                     GameDrawableFinder.FindDrawable(selectedObj), selectedObj.name);
                            currentState = ReversibleAction.Progress.Completed;
                            return true;
                        } else
                        {
                            selectedObj = null;
                            progressState = ProgressState.SelectObject;
                            LineMenu.disableLineMenu();
                            TextMenu.disableTextMenu();
                        }
                        break;
                    default:
                        return false;
                }
            }
            return false;     
        }

        /// <summary>
        /// This method provides the text menu for editing, adding the necessary AddListeners to the respective components.
        /// </summary>
        /// <param name="selectedText">The selected text object for editing.</param>
        private void enableTextMenu(GameObject selectedText)
        {
            if (newValueHolder is Text textHolder)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(selectedText);
                string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);

                UnityAction<Color> pickerAction = color =>
                {
                    GameEdit.ChangeFontColor(selectedText, color);
                    textHolder.fontColor = color;
                    new EditTextNetAction(drawable.name, drawableParentName, Text.GetText(selectedText)).Execute();
                };

                TextMenu.enableTextMenu(pickerAction, textHolder.fontColor, true, true);
                TextMenu.GetFontColorButton().onClick.AddListener(() =>
                {
                    TextMenu.AssignColorArea(pickerAction, textHolder.fontColor);
                });

                TextMenu.GetOutlineColorButton().onClick.AddListener(() =>
                {
                    if (textHolder.outlineColor == Color.clear)
                    {
                        textHolder.outlineColor = Random.ColorHSV();
                    }
                    if (textHolder.outlineColor.a == 0)
                    {
                        textHolder.outlineColor = new Color(textHolder.outlineColor.r, textHolder.outlineColor.g, textHolder.outlineColor.b, 255);
                    }
                    TextMenu.AssignColorArea(color =>
                    {
                        GameEdit.ChangeOutlineColor(selectedText, color);
                        textHolder.outlineColor = color;
                        new EditTextNetAction(drawable.name, drawableParentName, Text.GetText(selectedText)).Execute();
                    }, textHolder.outlineColor);
                });

                TextMenu.AssignOutlineThickness(thickness =>
                {
                    GameEdit.ChangeOutlineThickness(selectedText, thickness);
                    textHolder.outlineThickness = thickness;
                    new EditTextNetAction(drawable.name, drawableParentName, Text.GetText(selectedText)).Execute();
                }, textHolder.outlineThickness);


                TextMenu.AssignFontSize(size =>
                {
                    GameEdit.ChangeFontSize(selectedText, size);
                    textHolder.fontSize = size;
                    new EditTextNetAction(drawable.name, drawableParentName, Text.GetText(selectedText)).Execute();
                }, textHolder.fontSize);

                TextMenu.AssignFontStyles(style =>
                {
                    GameEdit.ChangeFontStyles(selectedText, style);
                    textHolder.fontStyles = style;
                    new EditTextNetAction(drawable.name, drawableParentName, Text.GetText(selectedText)).Execute();
                }, textHolder.fontStyles);

                TextMenu.AssignEditTextButton(() =>
                {
                    WriteEditTextDialog writeTextDialog = new();
                    writeTextDialog.SetStringInit(textHolder.text);
                    UnityAction<string> stringAction = (textOut =>
                    {
                        if (textOut != null && textOut != "")
                        {
                            TextMeshPro tmp = selectedText.GetComponent<TextMeshPro>();
                            tmp.rectTransform.sizeDelta = GameTexter.CalculateWidthAndHeight(textOut, tmp.font, textHolder.fontSize, textHolder.fontStyles);
                            GameEdit.ChangeText(selectedText, textOut);
                            textHolder.text = textOut;
                            new EditTextNetAction(drawable.name, drawableParentName, Text.GetText(selectedText)).Execute();
                        }
                        else
                        {
                            ShowNotification.Warn("Empty text", "The text to write is empty. Please add one.");
                        }
                    });

                    writeTextDialog.Open(stringAction);
                });

                TextMenu.AssignOrderInLayer(order =>
                {
                    GameEdit.ChangeLayer(selectedText, order);
                    textHolder.orderInLayer = order;
                    new EditTextNetAction(drawable.name, drawableParentName, Text.GetText(selectedText)).Execute();
                }, textHolder.orderInLayer);
            }
        }

        /// <summary>
        /// This method provides the line menu for editing, adding the necessary AddListeners to the respective components.
        /// </summary>
        /// <param name="selectedLine">The selected line object for editing.</param>
        private void enableLineMenu(GameObject selectedLine)
        {
            if (newValueHolder is Line lineHolder)
            {
                LineMenu.enableLineMenu();
                LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                GameObject drawable = GameDrawableFinder.FindDrawable(selectedLine);
                string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);

                LineMenu.AssignLineKind(selectedLine.GetComponent<LineKindHolder>().GetLineKind(), renderer.textureScale.x);

                LineMenu.GetTilingSliderController().onValueChanged.AddListener(LineMenu.tilingAction = tiling =>
                {
                    GameDrawer.ChangeLineKind(selectedLine, LineKind.Dashed, tiling);
                    lineHolder.lineKind = LineKind.Dashed;
                    lineHolder.tiling = tiling;
                    new ChangeLineKindNetAction(drawable.name, drawableParentName, selectedLine.name,
                            LineKind.Dashed, tiling).Execute();
                });

                LineMenu.GetNextLineKindBtn().onClick.RemoveAllListeners();
                LineMenu.GetNextLineKindBtn().onClick.AddListener(() =>
                {
                    LineKind kind = LineMenu.NextLineKind();
                    if (kind != LineKind.Dashed)
                    {
                        GameDrawer.ChangeLineKind(selectedObj, kind, lineHolder.tiling);
                        lineHolder.lineKind = kind;
                        new ChangeLineKindNetAction(drawable.name, drawableParentName, selectedLine.name,
                            kind, lineHolder.tiling).Execute();
                    }
                });
                LineMenu.GetPreviousLineKindBtn().onClick.RemoveAllListeners();
                LineMenu.GetPreviousLineKindBtn().onClick.AddListener(() =>
                {
                    LineKind kind = LineMenu.PreviousLineKind();
                    if (kind != LineKind.Dashed)
                    {
                        GameDrawer.ChangeLineKind(selectedObj, kind, lineHolder.tiling);
                        lineHolder.lineKind = kind;
                        new ChangeLineKindNetAction(drawable.name, drawableParentName, selectedLine.name,
                            kind, lineHolder.tiling).Execute();
                    }
                });

                thicknessSlider = LineMenu.instance.GetComponentInChildren<ThicknessSliderController>();
                thicknessSlider.AssignValue(renderer.startWidth);
                thicknessSlider.onValueChanged.AddListener(thickness =>
                {
                    if (thickness > 0.0f)
                    {
                        GameEdit.ChangeThickness(selectedLine, thickness);
                        lineHolder.thickness = thickness;
                        new EditLineThicknessNetAction(drawable.name, drawableParentName, selectedLine.name, thickness).Execute();
                    }
                });

                layerSlider = LineMenu.instance.GetComponentInChildren<LayerSliderController>();
                layerSlider.AssignValue(renderer.sortingOrder);
                layerSlider.onValueChanged.AddListener(layerOrder =>
                {
                    GameEdit.ChangeLayer(selectedLine, layerOrder);
                    lineHolder.orderInLayer = layerOrder;
                    new EditLayerNetAction(drawable.name, drawableParentName, selectedLine.name, layerOrder).Execute();
                });

                Toggle toggle = LineMenu.instance.GetComponentInChildren<Toggle>();
                toggle.isOn = renderer.loop;
                toggle.onValueChanged.AddListener(loop =>
                {
                    GameEdit.ChangeLoop(selectedLine, loop);
                    lineHolder.loop = loop;
                    new EditLineLoopNetAction(drawable.name, drawableParentName, selectedLine.name, loop).Execute();
                });

                picker = LineMenu.instance.GetComponent<HSVPicker.ColorPicker>();
                picker.AssignColor(renderer.material.color);
                picker.onValueChanged.AddListener(LineMenu.colorAction = color =>
                {
                    GameEdit.ChangeColor(selectedLine, color);
                    lineHolder.color = color;
                    new EditLineColorNetAction(drawable.name, drawableParentName, selectedLine.name, color).Execute();
                });
            }
        }

        /// <summary>
        /// Reverts this action, i.e., restores the old values of the selected object.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.selectedObj == null && memento.id != null)
            {
                memento.selectedObj = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.selectedObj != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(memento.selectedObj);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (memento.oldValueHolder is Line oldLineHolder)
                {
                    GameEdit.ChangeLine(memento.selectedObj, oldLineHolder);
                    new EditLineNetAction(drawable.name, drawableParent, oldLineHolder).Execute();
                }
                if (memento.oldValueHolder is Text oldTextHolder)
                {
                    GameEdit.ChangeText(memento.selectedObj, oldTextHolder);
                    new EditTextNetAction(drawable.name, drawableParent, oldTextHolder).Execute();
                }
            }
        }

        /// <summary>
        /// Repeats this action, i.e., assigns the new changed values to the selected object.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.selectedObj == null && memento.id != null)
            {
                memento.selectedObj = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.selectedObj != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(memento.selectedObj);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (memento.newValueHolder is Line newLineValueHolder)
                {
                    GameEdit.ChangeLine(memento.selectedObj, newLineValueHolder);
                    new EditLineNetAction(drawable.name, drawableParent, newLineValueHolder).Execute();
                }
                if (memento.newValueHolder is Text newTextHolder)
                {
                    GameEdit.ChangeText(memento.selectedObj, newTextHolder);
                    new EditTextNetAction(drawable.name, drawableParent, newTextHolder).Execute();
                }
            }
        }

        /// <summary>
        /// A new instance of <see cref="EditAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new EditAction();
        }

        /// <summary>
        /// A new instance of <see cref="EditAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Edit;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>The object id of the changed object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.selectedObj == null && memento.selectedObj == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.selectedObj.name
                };
            }
        }
    }
}