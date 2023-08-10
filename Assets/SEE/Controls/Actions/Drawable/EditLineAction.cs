using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Net.Actions.Whiteboard;
using RTG;
using SEE.Controls.Actions;
using SEE.Game;
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
        private const string editPrefabPath = "Prefabs/UI/DrawableEdit";
        private HSVPicker.ColorPicker picker;
        private LayerSliderController layerSlider;
        private ThicknessSliderController thicknessSlider;
        private Memento memento;
        private bool isActive = false;

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
                    GameObject selectedLine = raycastHit.collider.gameObject;
                    GameObject oldLine = GameEditLine.selectedLine;
                    
                    if (oldLine == null || (oldLine != null && !oldLine.name.Equals(selectedLine.name)))
                    {
                        LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                        GameEditLine.oldValueHolder = new(renderer.material.color, renderer.sortingOrder, renderer.startWidth);
                    }
                    if (oldLine == null || (oldLine != null && oldLine.name.Equals(selectedLine.name)))
                    {
                        LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                        GameEditLine.newValueHolder = new(renderer.material.color, renderer.sortingOrder, renderer.startWidth);
                    }

                    isActive = true;
                    BlinkEffect effect = selectedLine.AddOrGetComponent<BlinkEffect>();

                    if (GameEditLine.selectedLine != null && selectedLine.name.Equals(GameEditLine.selectedLine.name))
                    {
                        effect.LoopReverse();
                        if (!effect.GetLoopStatus())
                        {
                            Destroyer.Destroy(GameEditLine.editInstance);
                        }
                        memento = new Memento(GameEditLine.oldLine, GameEditLine.selectedLine, GameEditLine.oldValueHolder, GameEditLine.newValueHolder, 
                            GameEditLine.selectedLine.transform.parent.gameObject, GameEditLine.selectedLine.name);
                        currentState = ReversibleAction.Progress.Completed;
                    } else
                    {
                        if (GameEditLine.selectedLine != null)
                        {
                            GameEditLine.selectedLine.GetComponent<BlinkEffect>().Deactivate();
                            Destroyer.Destroy(GameEditLine.editInstance);
                        }
                        effect.line = selectedLine;
                        effect.Activate();
                        GameEditLine.selectedLine = selectedLine;
                    }

                   if (selectedLine.GetComponent<BlinkEffect>().GetLoopStatus())
                    {
                        GameEditLine.editInstance = PrefabInstantiator.InstantiatePrefab(editPrefabPath,
                            GameObject.Find("UI Canvas").transform, false);
                        LineRenderer renderer = GameEditLine.selectedLine.GetComponent<LineRenderer>();

                        thicknessSlider = GameEditLine.editInstance.GetComponentInChildren<ThicknessSliderController>();
                        thicknessSlider.AssignValue(renderer.startWidth);
                        thicknessSlider.onValueChanged.AddListener(thickness =>
                        {
                            GameEditLine.ChangeThickness(GameEditLine.selectedLine, thickness);
                        });

                        layerSlider = GameEditLine.editInstance.GetComponentInChildren<LayerSliderController>();
                        layerSlider.AssignValue(renderer.sortingOrder);
                        layerSlider.onValueChanged.AddListener(layerOrder => 
                        {
                            GameEditLine.ChangeLayer(GameEditLine.selectedLine, layerOrder);
                        });

                        picker = GameEditLine.editInstance.GetComponent<HSVPicker.ColorPicker>();
                        picker.AssignColor(renderer.material.color);
                        picker.onValueChanged.AddListener(color =>
                        {
                            GameEditLine.ChangeColor(GameEditLine.selectedLine, color);
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
            public readonly GameEditLine.ValueHolder oldValueHolder;
            public readonly GameEditLine.ValueHolder newValueHolder;
            public readonly GameObject drawable;
            public readonly string currentLineName;

            public Memento(GameObject oldLine, GameObject currentLine, GameEditLine.ValueHolder oldValueHolder, GameEditLine.ValueHolder newValueHolder, GameObject drawable, string currentLineName)
            {
                this.oldLine = oldLine;
                this.currentLine = currentLine;
                this.oldValueHolder = oldValueHolder;
                this.newValueHolder = newValueHolder;
                this.drawable = drawable;
                this.currentLineName = currentLineName;
            }
        }

        /*
        public override void Stop()
        {
            Debug.Log("Action stopped " + DateTime.Now);
            /*
            foreach(BlinkEffect effect in effects)
            {
                effect.Deactivate();
                Destroyer.Destroy(effect);
            }
            
        }*/

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.oldLine == null || (memento.oldLine != null && memento.oldLine.name.Equals(memento.currentLine.name)))
            {
                if (memento.currentLine == null && memento.currentLineName != null)
                {
                    memento.currentLine = GameDrawableIDFinder.FindChild(memento.drawable, memento.currentLineName);
                }
                if (memento.currentLine != null)
                {
                    GameEditLine.ChangeThickness(memento.currentLine, memento.oldValueHolder.thickness);
                    GameEditLine.ChangeLayer(memento.currentLine, memento.oldValueHolder.layer);
                    GameEditLine.ChangeColor(memento.currentLine, memento.oldValueHolder.color);
                }
                if (GameEditLine.editInstance != null)
                {
                    Destroyer.Destroy(GameEditLine.editInstance);
                }
            }
            else
            {
                BlinkEffect oldBlinkEffect = memento.oldLine.AddOrGetComponent<BlinkEffect>();
                oldBlinkEffect.LoopReverse();
                BlinkEffect currentBlinkEffect = memento.currentLine.AddOrGetComponent<BlinkEffect>();
                GameEditLine.selectedLine = memento.oldLine;
                currentBlinkEffect.LoopReverse();
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
            if (memento.oldLine == null || (memento.oldLine != null && memento.oldLine.name.Equals(memento.currentLine.name)))
            {
                if (memento.currentLine == null && memento.currentLineName != null)
                {
                    memento.currentLine = GameDrawableIDFinder.FindChild(memento.drawable, memento.currentLineName);
                }
                if (memento.currentLine != null)
                {
                    GameEditLine.ChangeThickness(memento.currentLine, memento.newValueHolder.thickness);
                    GameEditLine.ChangeLayer(memento.currentLine, memento.newValueHolder.layer);
                    GameEditLine.ChangeColor(memento.currentLine, memento.newValueHolder.color);
                }
                if (GameEditLine.editInstance != null)
                {
                    Destroyer.Destroy(GameEditLine.editInstance);
                }
            } else
            {
                if (memento.oldLine != null)
                {
                    BlinkEffect oldBlinkEffect = memento.oldLine.AddOrGetComponent<BlinkEffect>();
                    oldBlinkEffect.LoopReverse();
                }
                BlinkEffect currentBlinkEffect = memento.currentLine.AddOrGetComponent<BlinkEffect>();
                GameEditLine.selectedLine = memento.currentLine;
                currentBlinkEffect.LoopReverse();
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
            } else 
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