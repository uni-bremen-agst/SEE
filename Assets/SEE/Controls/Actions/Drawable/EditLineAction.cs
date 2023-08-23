using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game.UI.Drawable;
using Assets.SEE.Net.Actions.Drawable;
using Assets.SEE.Net.Actions.Whiteboard;
using RTG;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Game.UI.ConfigMenu;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Assets.SEE.Controls.Actions.Drawable
{
    public class EditLineAction : AbstractPlayerAction
    {
        private HSVPicker.ColorPicker picker;
        private LayerSliderController layerSlider;
        private ThicknessSliderController thicknessSlider;
        private Memento memento;
        private bool isActive = false;

        private static GameObject selectedLine;

        private static ValueHolder oldValueHolder;

        private static ValueHolder newValueHolder;

        public class ValueHolder
        {
            public Color color;
            public int layer;
            public float thickness;

            public ValueHolder(Color color, int layer, float thickness)
            {
                this.color = color;
                this.layer = layer;
                this.thickness = thickness;
            }

            public bool CheckEquals(ValueHolder holder)
            {
                return color.Equals(holder.color) && layer.Equals(holder.layer) && thickness.Equals(holder.thickness);
            }
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
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && isActive == false &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    raycastHit.collider.gameObject.CompareTag(Tags.Line))
                {
                    GameObject currentSelectedLine = raycastHit.collider.gameObject;
                    GameObject oldLine = selectedLine;

                    if (oldLine != null && !oldValueHolder.CheckEquals(newValueHolder))
                    {
                        memento = new Memento(oldLine, oldLine, oldValueHolder, newValueHolder,
                                     GameDrawableFinder.FindDrawableParent(oldLine), oldLine.name);
                        currentState = ReversibleAction.Progress.Completed;
                    }

                    LineRenderer renderer = currentSelectedLine.GetComponent<LineRenderer>();
                    oldValueHolder = new(renderer.material.color, renderer.sortingOrder, renderer.startWidth);
                    newValueHolder = new(renderer.material.color, renderer.sortingOrder, renderer.startWidth);

                    isActive = true;
                    BlinkEffect effect = currentSelectedLine.AddOrGetComponent<BlinkEffect>();
                    effect.SetAllowedActionStateType(GetActionStateType());

                    if (oldLine != null)
                    {
                        if (currentSelectedLine.name.Equals(oldLine.name))
                        {
                            effect.LoopReverse();
                            selectedLine = null;
                            if (!effect.GetLoopStatus())
                            {
                                DrawableConfigurator.disableDrawableMenu();
                            }
                        }
                        else
                        {
                            if (oldLine.GetComponent<BlinkEffect>() != null)
                            {
                                oldLine.GetComponent<BlinkEffect>().Deactivate();
                                DrawableConfigurator.disableDrawableMenu();
                            }
                        }
                    }
                    if (oldLine == null || !currentSelectedLine.name.Equals(oldLine.name))
                    {
                        effect.Activate(currentSelectedLine);
                        selectedLine = currentSelectedLine;
                    }

                    if (currentSelectedLine.GetComponent<BlinkEffect>() != null && currentSelectedLine.GetComponent<BlinkEffect>().GetLoopStatus())
                    {
                        DrawableConfigurator.enableDrawableMenu();

                        GameObject drawable = GameDrawableFinder.FindDrawableParent(currentSelectedLine);
                        string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);

                        thicknessSlider = DrawableConfigurator.drawableMenu.GetComponentInChildren<ThicknessSliderController>();
                        thicknessSlider.AssignValue(renderer.startWidth);
                        thicknessSlider.onValueChanged.AddListener(thickness =>
                        {
                            if (thickness > 0.0f)
                            {
                                GameEditLine.ChangeThickness(currentSelectedLine, thickness);
                                newValueHolder.thickness = thickness;
                                new EditLineThicknessNetAction(drawable.name, drawableParentName, currentSelectedLine.name, thickness).Execute();
                            }
                        });

                        layerSlider = DrawableConfigurator.drawableMenu.GetComponentInChildren<LayerSliderController>();
                        layerSlider.AssignValue(renderer.sortingOrder);
                        layerSlider.onValueChanged.AddListener(layerOrder =>
                        {
                            GameEditLine.ChangeLayer(currentSelectedLine, layerOrder);
                            newValueHolder.layer = layerOrder;
                            new EditLineLayerNetAction(drawable.name, drawableParentName, currentSelectedLine.name, layerOrder).Execute();
                        });

                        picker = DrawableConfigurator.drawableMenu.GetComponent<HSVPicker.ColorPicker>();
                        picker.AssignColor(renderer.material.color);
                        picker.onValueChanged.AddListener(DrawableConfigurator.colorAction = color =>
                        {
                            GameEditLine.ChangeColor(currentSelectedLine, color);
                            newValueHolder.color = color;
                            new EditLineColorNetAction(drawable.name, drawableParentName, currentSelectedLine.name, color).Execute();
                        });
                    }
                    result = true;
                }
                if (Input.GetMouseButtonUp(0) && isActive)
                {
                    isActive = false;
                }
                return Input.GetMouseButtonUp(0);
            }
            return result;
        }

        private struct Memento
        {
            public readonly GameObject oldLine;
            public GameObject currentLine;
            public readonly ValueHolder oldValueHolder;
            public readonly ValueHolder newValueHolder;
            public readonly GameObject drawable;
            public readonly string currentLineName;

            public Memento(GameObject oldLine, GameObject currentLine, ValueHolder oldValueHolder,
                ValueHolder newValueHolder, GameObject drawable, string currentLineName)
            {
                this.oldLine = oldLine;
                this.currentLine = currentLine;
                this.oldValueHolder = oldValueHolder;
                this.newValueHolder = newValueHolder;
                this.drawable = drawable;
                this.currentLineName = currentLineName;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.currentLine == null && memento.currentLineName != null)
            {
                memento.currentLine = GameDrawableFinder.FindChild(memento.drawable, memento.currentLineName);
            }
            if (memento.oldLine == null || (memento.oldLine != null && memento.oldLine.name.Equals(memento.currentLine.name)))
            {
                if (memento.currentLine != null)
                {
                    GameEditLine.ChangeThickness(memento.currentLine, memento.oldValueHolder.thickness);
                    GameEditLine.ChangeLayer(memento.currentLine, memento.oldValueHolder.layer);
                    GameEditLine.ChangeColor(memento.currentLine, memento.oldValueHolder.color);

                    GameObject drawable = GameDrawableFinder.FindDrawableParent(memento.currentLine);
                    string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);

                    new EditLineThicknessNetAction(drawable.name, drawableParent, memento.currentLine.name, memento.oldValueHolder.thickness).Execute();
                    new EditLineLayerNetAction(drawable.name, drawableParent, memento.currentLine.name, memento.oldValueHolder.layer).Execute();
                    new EditLineColorNetAction(drawable.name, drawableParent, memento.currentLine.name, memento.oldValueHolder.color).Execute();
                }
            }
            DrawableConfigurator.disableDrawableMenu();
            if (memento.oldLine != null && memento.oldLine.TryGetComponent<BlinkEffect>(out BlinkEffect oldEffect))
            {
                oldEffect.Deactivate();
            }
            if (memento.currentLine != null && memento.currentLine.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
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
            if (memento.currentLine == null && memento.currentLineName != null)
            {
                memento.currentLine = GameDrawableFinder.FindChild(memento.drawable, memento.currentLineName);
            }
            if (memento.oldLine == null || (memento.oldLine != null && memento.oldLine.name.Equals(memento.currentLine.name)))
            {
                if (memento.currentLine != null)
                {
                    GameEditLine.ChangeThickness(memento.currentLine, memento.newValueHolder.thickness);
                    GameEditLine.ChangeLayer(memento.currentLine, memento.newValueHolder.layer);
                    GameEditLine.ChangeColor(memento.currentLine, memento.newValueHolder.color);

                    GameObject drawable = GameDrawableFinder.FindDrawableParent(memento.currentLine);
                    string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);

                    new EditLineThicknessNetAction(drawable.name, drawableParent, memento.currentLine.name, memento.newValueHolder.thickness).Execute();
                    new EditLineLayerNetAction(drawable.name, drawableParent, memento.currentLine.name, memento.newValueHolder.layer).Execute();
                    new EditLineColorNetAction(drawable.name, drawableParent, memento.currentLine.name, memento.newValueHolder.color).Execute();
                }
            }
            DrawableConfigurator.disableDrawableMenu();
            if (memento.oldLine != null && memento.oldLine.TryGetComponent<BlinkEffect>(out BlinkEffect oldEffect))
            {
                oldEffect.Deactivate();
            }
            if (memento.currentLine != null && memento.currentLine.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
            }
        }

        /// <summary>
        /// A new instance of <see cref="EditLineAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditLineAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new EditLineAction();
        }

        /// <summary>
        /// A new instance of <see cref="EditLineAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditLineAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.EditLine;
        }

        public override HashSet<string> GetChangedObjects()
        {
            if (memento.oldLine == null && memento.currentLine == null)
            {
                return new HashSet<string>();
            }
            else
            if (memento.oldLine == null && memento.currentLine != null)
            {
                return new HashSet<string>
                {
                    memento.currentLine.name
                };
            }
            else
            {
                return new HashSet<string>
                {
                    memento.oldLine.name,
                    memento.currentLine.name
                };
            }
        }
    }
}