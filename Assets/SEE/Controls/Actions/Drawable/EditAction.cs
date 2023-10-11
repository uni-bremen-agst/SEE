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
        private HSVPicker.ColorPicker picker;
        private LayerSliderController layerSlider;
        private ThicknessSliderController thicknessSlider;
        private Memento memento;
        private bool isActive = false;
        private bool finish = false;

        private GameObject selectedObj;

        private DrawableType oldValueHolder;

        private DrawableType newValueHolder;

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
        /// 
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;
            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && isActive == false && !finish &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) && // Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    Tags.DrawableTypes.Contains(raycastHit.collider.gameObject.tag))
                {
                    GameObject currentSelectedObj = raycastHit.collider.gameObject;
                    GameObject oldObj = selectedObj;
                    isActive = true;

                    if (oldObj != null && !CheckEquals(oldValueHolder, newValueHolder))
                    {
                        finish = true;
                        oldObj.GetComponent<BlinkEffect>().Deactivate();
                       // TextMenu.disableTextMenu();
                       // LineMenu.disableLineMenu();
                        memento = new Memento(oldObj, oldValueHolder, newValueHolder,
                                     GameDrawableFinder.FindDrawable(oldObj), oldObj.name);
                        currentState = ReversibleAction.Progress.InProgress;
                      //  return false;
                    }

                    oldValueHolder = new DrawableType().Get(currentSelectedObj);
                    newValueHolder = new DrawableType().Get(currentSelectedObj);


                    BlinkEffect effect = currentSelectedObj.AddOrGetComponent<BlinkEffect>();
                    effect.SetAllowedActionStateType(GetActionStateType());

                    if (oldObj != null)
                    {
                        if (currentSelectedObj.name.Equals(oldObj.name))
                        {
                            effect.LoopReverse();
                            selectedObj = null;
                            if (!effect.GetLoopStatus())
                            {
                                TextMenu.disableTextMenu();
                                LineMenu.disableLineMenu();
                            }
                        }
                        else
                        {
                            if (oldObj.GetComponent<BlinkEffect>() != null)
                            {
                                oldObj.GetComponent<BlinkEffect>().Deactivate();
                                TextMenu.disableTextMenu();
                                LineMenu.disableLineMenu();
                            }
                        }
                    }
                    if (oldObj == null || !currentSelectedObj.name.Equals(oldObj.name))
                    {
                        effect.Activate(currentSelectedObj);
                        selectedObj = currentSelectedObj;
                    }

                    if (currentSelectedObj.GetComponent<BlinkEffect>() != null && currentSelectedObj.GetComponent<BlinkEffect>().GetLoopStatus())
                    {
                        //TODO add MMNode Tags if that are differents.
                        if (currentSelectedObj.CompareTag(Tags.Line))
                        {
                            enableLineMenu(currentSelectedObj);
                        }
                        if (currentSelectedObj.CompareTag(Tags.DText))
                        {
                            enableTextMenu(currentSelectedObj);
                        }
                    }
                }
                if (Input.GetMouseButtonUp(0))
                {
                    if (isActive)
                    {
                        isActive = false;
                    }
                    if (finish)
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                }
                return false;
            }
            return result;
        }

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


                TextMenu.AssignFontSize(size => {
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
                    UnityAction<string> stringAction = (textOut => {
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

        private struct Memento
        {
            public GameObject obj;
            public readonly DrawableType oldValueHolder;
            public readonly DrawableType newValueHolder;
            public readonly GameObject drawable;
            public readonly string id;

            public Memento(GameObject obj, DrawableType oldValueHolder,
                DrawableType newValueHolder, GameObject drawable, string id)
            {
                this.obj = obj;
                this.oldValueHolder = oldValueHolder;
                this.newValueHolder = newValueHolder;
                this.drawable = drawable;
                this.id = id;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.obj == null && memento.id != null)
            {
                memento.obj = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.obj != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(memento.obj);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (memento.oldValueHolder is Line oldLineHolder)
                {
                    GameEdit.ChangeLine(memento.obj, oldLineHolder);
                    new EditLineNetAction(drawable.name, drawableParent, oldLineHolder).Execute();
                }
                if (memento.oldValueHolder is Text oldTextHolder)
                {
                    GameEdit.ChangeText(memento.obj, oldTextHolder);
                    new EditTextNetAction(drawable.name, drawableParent, oldTextHolder).Execute();
                }
            }
        }

        /// <summary>
        /// Redraws the drawn line (setting up <see cref="line"/> and adds <see cref="renderer"/> 
        /// before that).
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.obj == null && memento.id != null)
            {
                memento.obj = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.obj != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(memento.obj);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (memento.newValueHolder is Line newLineValueHolder)
                {
                    GameEdit.ChangeLine(memento.obj, newLineValueHolder);
                    new EditLineNetAction(drawable.name, drawableParent, newLineValueHolder).Execute();
                }
                if (memento.newValueHolder is Text newTextHolder)
                {
                    GameEdit.ChangeText(memento.obj, newTextHolder);
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

        public override HashSet<string> GetChangedObjects()
        {
            if (memento.obj == null && memento.obj == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.obj.name
                };
            }
        }
    }
}