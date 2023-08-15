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
    public class MoveRotatorAction : AbstractPlayerAction
    {
        private Memento memento;
        private bool isActive = false;
        GameObject selectedLine;
        GameObject oldLine;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;
            selectedLine = GameMoveRotator.selectedLine;
            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && isActive == false &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    raycastHit.collider.gameObject.CompareTag(Tags.Line))
                {
                    selectedLine = raycastHit.collider.gameObject;
                    oldLine = GameMoveRotator.selectedLine;

                    /*
                    if (oldLine != null && !GameEditLine.oldValueHolder.CheckEquals(GameEditLine.newValueHolder))
                    {
                       memento = new Memento(oldLine, oldLine, GameEditLine.oldValueHolder, GameEditLine.newValueHolder,
                                    oldLine.transform.parent.gameObject, oldLine.name);
                       currentState = ReversibleAction.Progress.Completed;
                    }
                    *
                    LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                    GameEditLine.oldValueHolder = new(renderer.material.color, renderer.sortingOrder, renderer.startWidth);
                    GameEditLine.newValueHolder = new(renderer.material.color, renderer.sortingOrder, renderer.startWidth);
                    */
                    isActive = true;
                    BlinkEffect effect = selectedLine.AddOrGetComponent<BlinkEffect>();
                    effect.SetAllowedActionStateType(GetActionStateType());

                    if (oldLine != null)
                    {
                        if (selectedLine.name.Equals(oldLine.name))
                        {
                            effect.LoopReverse();
                            GameMoveRotator.selectedLine = null;
                            /*
                            if (!effect.GetLoopStatus())
                            {
                                Destroyer.Destroy(GameEditLine.editMenuInstance);
                            }*/
                        }
                        else
                        {
                            if (oldLine.GetComponent<BlinkEffect>() != null)
                            {
                                oldLine.GetComponent<BlinkEffect>().Deactivate();
                                // Destroyer.Destroy(GameEditLine.editMenuInstance);
                            }
                        }
                    }
                    if (oldLine == null || !selectedLine.name.Equals(oldLine.name))
                    {
                        effect.Activate(selectedLine);
                        GameMoveRotator.selectedLine = selectedLine;
                    }
                }

                if (selectedLine != null && selectedLine.GetComponent<BlinkEffect>() != null && selectedLine.GetComponent<BlinkEffect>().GetLoopStatus())
                {
                    if (Raycasting.RaycastAnything(out RaycastHit hit))
                    {
                        Debug.Log("Hit game object: " + hit.collider.gameObject);
                        if (hit.collider.gameObject.CompareTag(Tags.Drawable))
                        {
                            Debug.Log(DateTime.Now + " - Hitposition: " + hit.point);
                            LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                            Vector3[] positions = new Vector3[renderer.positionCount];
                            renderer.GetPositions(positions);
                            if ((Input.GetMouseButton(0) || Input.GetMouseButtonDown(0)) && isActive == false)
                            {
                                isActive = true;
                                Debug.Log("Positionsize: " + positions.Length + " - Render size: " + renderer.positionCount);
                                for (int i = 0; i < renderer.positionCount; i++)
                                {
                                    
                                    Vector3 position = positions[i];
                                    float x = position.x;
                                    float y = position.y;

                                    float hitX = hit.point.x;
                                    float hitY = hit.point.y;

                                    Vector3 moveOffset = new(hitX - x, hitY - y, 0);
                                    
                                    positions[i] = position + moveOffset;
                                }
                                renderer.SetPositions(positions);
                                Mesh mesh = new();
                                MeshCollider meshCollider = selectedLine.GetComponent<MeshCollider>();
                                Destroyer.Destroy(meshCollider);
                                MeshCollider newMeshCollider = selectedLine.AddComponent<MeshCollider>(); 
                                renderer.BakeMesh(mesh, false);
                                newMeshCollider.sharedMesh = mesh;
                            }
                        }
                    }
                            //Debug.Log("Rotate");
                            //Vector3 newRotation = new Vector3(0, 0, 0);
                            //selectedLine.transform.localEulerAngles = newRotation;

                        /*
                        GameEditLine.editMenuInstance = PrefabInstantiator.InstantiatePrefab(editPrefabPath,
                            GameObject.Find("UI Canvas").transform, false);
                        GameObject drawable = selectedLine.transform.parent.gameObject;
                        GameObject drawableParent = drawable.transform.parent.gameObject;

                        thicknessSlider = GameEditLine.editMenuInstance.GetComponentInChildren<ThicknessSliderController>();
                        thicknessSlider.AssignValue(renderer.startWidth);
                        thicknessSlider.onValueChanged.AddListener(thickness =>
                        {
                            GameEditLine.ChangeThickness(selectedLine, thickness);
                            GameEditLine.newValueHolder.thickness = thickness;
                            new EditLineThicknessNetAction(drawable.name, drawableParent.name, selectedLine.name, thickness).Execute();
                        });

                        layerSlider = GameEditLine.editMenuInstance.GetComponentInChildren<LayerSliderController>();
                        layerSlider.AssignValue(renderer.sortingOrder);
                        layerSlider.onValueChanged.AddListener(layerOrder =>
                        {
                            GameEditLine.ChangeLayer(selectedLine, layerOrder);
                            GameEditLine.newValueHolder.layer = layerOrder;
                            new EditLineLayerNetAction(drawable.name, drawableParent.name, selectedLine.name, layerOrder).Execute();
                        });

                        picker = GameEditLine.editMenuInstance.GetComponent<HSVPicker.ColorPicker>();
                        picker.AssignColor(renderer.material.color);
                        picker.onValueChanged.AddListener(color =>
                        {
                            GameEditLine.ChangeColor(selectedLine, color);
                            GameEditLine.newValueHolder.color = color;
                            new EditLineColorNetAction(drawable.name, drawableParent.name, selectedLine.name, color).Execute();
                        });*/
                    }


                    result = true;
                
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

            public Memento(GameObject oldLine, GameObject currentLine, GameEditLine.ValueHolder oldValueHolder,
                GameEditLine.ValueHolder newValueHolder, GameObject drawable, string currentLineName)
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
                memento.currentLine = GameDrawableIDFinder.FindChild(memento.drawable, memento.currentLineName);
            }
            if (memento.oldLine == null || (memento.oldLine != null && memento.oldLine.name.Equals(memento.currentLine.name)))
            {
                if (memento.currentLine != null)
                {
                    GameEditLine.ChangeThickness(memento.currentLine, memento.oldValueHolder.thickness);
                    GameEditLine.ChangeLayer(memento.currentLine, memento.oldValueHolder.layer);
                    GameEditLine.ChangeColor(memento.currentLine, memento.oldValueHolder.color);

                    GameObject drawable = memento.currentLine.transform.parent.gameObject;
                    GameObject drawableParent = drawable.transform.parent.gameObject;

                    new EditLineThicknessNetAction(drawable.name, drawableParent.name, memento.currentLine.name, memento.oldValueHolder.thickness).Execute();
                    new EditLineLayerNetAction(drawable.name, drawableParent.name, memento.currentLine.name, memento.oldValueHolder.layer).Execute();
                    new EditLineColorNetAction(drawable.name, drawableParent.name, memento.currentLine.name, memento.oldValueHolder.color).Execute();
                }
            }
            if (GameEditLine.editMenuInstance != null)
            {
                Destroyer.Destroy(GameEditLine.editMenuInstance);
            }
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
                memento.currentLine = GameDrawableIDFinder.FindChild(memento.drawable, memento.currentLineName);
            }
            if (memento.oldLine == null || (memento.oldLine != null && memento.oldLine.name.Equals(memento.currentLine.name)))
            {
                if (memento.currentLine != null)
                {
                    GameEditLine.ChangeThickness(memento.currentLine, memento.newValueHolder.thickness);
                    GameEditLine.ChangeLayer(memento.currentLine, memento.newValueHolder.layer);
                    GameEditLine.ChangeColor(memento.currentLine, memento.newValueHolder.color);

                    GameObject drawable = memento.currentLine.transform.parent.gameObject;
                    GameObject drawableParent = drawable.transform.parent.gameObject;

                    new EditLineThicknessNetAction(drawable.name, drawableParent.name, memento.currentLine.name, memento.newValueHolder.thickness).Execute();
                    new EditLineLayerNetAction(drawable.name, drawableParent.name, memento.currentLine.name, memento.newValueHolder.layer).Execute();
                    new EditLineColorNetAction(drawable.name, drawableParent.name, memento.currentLine.name, memento.newValueHolder.color).Execute();
                }
            }
            if (GameEditLine.editMenuInstance != null)
            {
                Destroyer.Destroy(GameEditLine.editMenuInstance);
            }
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
            return new MoveRotatorAction();
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
            return ActionStateTypes.MoveRotator;
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