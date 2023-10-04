using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RootMotion.FinalIK.HitReaction;
using Assets.SEE.Net.Actions.Whiteboard;
using SEE.Net.Actions;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Allows to create drawings by the mouse cursor.
    /// It serves as an example for a continuous action that modifies the
    /// scene while active.
    /// </summary>
    class LayerChangerAction : AbstractPlayerAction
    {
        private bool isInAction = false;
        private Memento memento;
        private bool result = false;

        /// <summary>
        /// Continues the line at the point of the mouse position and draws it.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && isInAction == false &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) && // Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawableParent(raycastHit.collider.gameObject))
                {
                    isInAction = true;
                    GameObject hittedObject = raycastHit.collider.gameObject;
                    GameObject drawable = GameDrawableFinder.FindDrawable(hittedObject);

                    switch (hittedObject.tag) { 
                        case Tags.Line:
                            int oldOrder = hittedObject.GetComponent<LineRenderer>().sortingOrder;
                            int newOrder = oldOrder + 1;
                            result = GameLayerChanger.Increase(DrawableTypes.Line, hittedObject, newOrder);
                            if (result)
                            {
                                memento = new Memento(drawable, GameLayerChanger.LayerChangerStates.Increase, DrawableTypes.Line,
                                    hittedObject, hittedObject.name, oldOrder, newOrder);
                                new LayerChangerNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable),
                                    memento.obj.name, memento.state, memento.type, memento.newOrder).Execute();
                                currentState = ReversibleAction.Progress.InProgress;
                            }
                            break;
                        default: break;
                    }
                }
                if ((Input.GetMouseButtonDown(1) || Input.GetMouseButton(1)) && isInAction == false &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit2) && GameDrawableFinder.hasDrawableParent(raycastHit2.collider.gameObject))
                {
                    isInAction = true;
                    GameObject hittedObject = raycastHit2.collider.gameObject;
                    GameObject drawable = GameDrawableFinder.FindDrawable(hittedObject);

                    switch (hittedObject.tag)
                    {
                        case Tags.Line:
                            int oldOrder = hittedObject.GetComponent<LineRenderer>().sortingOrder;
                            int newOrder = oldOrder - 1;
                            newOrder = newOrder < 0 ? 0 : newOrder;
                            result = GameLayerChanger.Decrease(DrawableTypes.Line, hittedObject, newOrder);
                            if (result)
                            {
                                memento = new Memento(drawable, GameLayerChanger.LayerChangerStates.Decrease, DrawableTypes.Line,
                                hittedObject, hittedObject.name, oldOrder, newOrder);
                                new LayerChangerNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable),
                                    memento.obj.name, memento.state, memento.type, memento.newOrder).Execute();
                                currentState = ReversibleAction.Progress.InProgress;
                            }
                            break;
                        default: break;
                    }
                    
                }
                if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) ) && isInAction && result)
                {
                    isInAction = false;
                    currentState = ReversibleAction.Progress.Completed;
                }
                return Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1);
            }
            return result;
        }

        private struct Memento
        {
            public readonly GameObject drawable;
            public readonly GameLayerChanger.LayerChangerStates state;
            public readonly DrawableTypes type;
            public GameObject obj;
            public readonly string id;
            public readonly int oldOrder;
            public readonly int newOrder;

            public Memento(GameObject drawable, GameLayerChanger.LayerChangerStates state, DrawableTypes type, GameObject obj, string id, int oldOrder, int newOrder)
            {
                this.drawable = drawable;
                this.state = state;
                this.type = type;
                this.obj = obj;
                this.id = id;
                this.oldOrder = oldOrder;
                this.newOrder = newOrder;
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
            switch (memento.state)
            {
                case GameLayerChanger.LayerChangerStates.Increase:
                    GameLayerChanger.Decrease(memento.type, memento.obj, memento.oldOrder);
                    break;
                case GameLayerChanger.LayerChangerStates.Decrease:
                    GameLayerChanger.Increase(memento.type, memento.obj, memento.oldOrder);
                    break;
            }
            new LayerChangerNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.obj.name, memento.state, memento.type, memento.oldOrder).Execute();
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
            switch (memento.state)
            {
                case GameLayerChanger.LayerChangerStates.Increase:
                    GameLayerChanger.Increase(memento.type, memento.obj, memento.newOrder);
                    break;
                case GameLayerChanger.LayerChangerStates.Decrease:
                    GameLayerChanger.Decrease(memento.type, memento.obj, memento.newOrder);
                    break;
            }
            new LayerChangerNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.obj.name, memento.state, memento.type, memento.newOrder).Execute();
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new LayerChangerAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.DrawOnWhiteboard"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.LayerChanger;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>an empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.obj == null)
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
