using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils.History;
using SEE.GO;
using System;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the layer order of a <see cref="DrawableType"/>
    /// </summary>
    class LayerChangerAction : DrawableAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// True if the action is running.
        /// </summary>
        private bool isInAction = false;

        /// <summary>
        /// Indicates whether information about the control has already been displayed.
        /// </summary>
        private static bool showInfo = false;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="LayerChangerAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The drawable on which the object to be layer changed is located.
            /// </summary>
            public readonly DrawableConfig Drawable;
            /// <summary>
            /// Is the state of the layer change.
            /// </summary>
            public readonly GameLayerChanger.LayerChangerStates State;
            /// <summary>
            /// The object whose order should be changed in the layering.
            /// </summary>
            public GameObject Obj;
            /// <summary>
            /// The id of the object to be changed.
            /// </summary>
            public readonly string Id;
            /// <summary>
            /// The old layer order.
            /// </summary>
            public readonly int OldOrder;
            /// <summary>
            /// The new layer order.
            /// </summary>
            public readonly int NewOrder;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="drawable">The drawable to save into this Memento</param>
            /// <param name="state">The state to save into this Memento</param>
            /// <param name="obj">The object to save into this Memento</param>
            /// <param name="id">The object id to save into this Memento</param>
            /// <param name="oldOrder">The old order in layer value to save into this Memento</param>
            /// <param name="newOrder">The new order in layer value to save into this Memento</param>
            public Memento(GameObject drawable, GameLayerChanger.LayerChangerStates state,
                GameObject obj, string id, int oldOrder, int newOrder)
            {
                Drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                State = state;
                Obj = obj;
                Id = id;
                OldOrder = oldOrder;
                NewOrder = newOrder;
            }
        }

        /// <summary>
        /// Is required to display the control information again after a change in action.
        /// </summary>
        public static void Reset()
        {
            showInfo = false;
        }

        /// <summary>
        /// Displays a control hint when invoking this action.
        /// A <see cref="ValueResetter"/> is added to the UI canvas so that
        /// the information can be shown again after an action change.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            if (!showInfo)
            {
                showInfo = true;
                ShowNotification.Info("Usage note", "Use the left mouse button to increase the layer." +
                    "\nUse the right mouse button to decrease the layer.");
                Canvas.AddOrGetComponent<ValueResetter>().SetAllowedState(GetActionStateType());
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.LayerChanger"/>.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// Increase block.
                /// It increses the order in layer of a <see cref="DrawableType"/> object
                /// with a <see cref="OrderInLayerValueHolder"/> component.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !isInAction
                    && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                    && GameFinder.HasDrawable(raycastHit.collider.gameObject)
                    && raycastHit.collider.gameObject.GetComponent<OrderInLayerValueHolder>() != null)
                {
                    Increase(raycastHit.collider.gameObject);
                }

                /// Decrease block
                /// it decreases the order in layer of a <see cref="DrawableType"/> object
                /// with a <see cref="OrderInLayerValueHolder"/> component.
                if ((Input.GetMouseButtonDown(1) || Input.GetMouseButton(1)) && !isInAction
                    && Raycasting.RaycastAnything(out RaycastHit raycastHit2)
                    && GameFinder.HasDrawable(raycastHit2.collider.gameObject)
                    && raycastHit2.collider.gameObject.GetComponent<OrderInLayerValueHolder>() != null)
                {
                    Decrease(raycastHit2.collider.gameObject);
                }

                /// It completes the action after a layer change if the user releases the pressed mouse button.
                if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) ) && isInAction)
                {
                    isInAction = false;
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Increases the order in layer by one.
        /// Additionally, a memento of this action is created and the progress state is set to InProgress.
        /// </summary>
        /// <param name="hitObject">The object to increase the order for</param>
        private void Increase(GameObject hitObject)
        {
            isInAction = true;
            GameObject drawable = GameFinder.GetDrawable(hitObject);

            int oldOrder = hitObject.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();
            int newOrder = oldOrder + 1;
            GameLayerChanger.Increase(hitObject, newOrder);
            memento = new Memento(drawable, GameLayerChanger.LayerChangerStates.Increase,
                            hitObject, hitObject.name, oldOrder, newOrder);
            new LayerChangerNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                memento.Obj.name, memento.State, memento.NewOrder).Execute();
            CurrentState = IReversibleAction.Progress.InProgress;
        }

        /// <summary>
        /// Decreases the order in layer by one.
        /// Additionally, a memento of this action is created and the progress state is set to InProgress.
        /// </summary>
        /// <param name="hittedObject">The object to decrease the order for</param>
        private void Decrease(GameObject hittedObject)
        {
            isInAction = true;
            GameObject drawable = GameFinder.GetDrawable(hittedObject);

            int oldOrder = hittedObject.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();
            int newOrder = oldOrder - 1;
            newOrder = newOrder < 0 ? 0 : newOrder;
            GameLayerChanger.Decrease(hittedObject, newOrder);
            memento = new Memento(drawable, GameLayerChanger.LayerChangerStates.Decrease,
                        hittedObject, hittedObject.name, oldOrder, newOrder);
            new LayerChangerNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                memento.Obj.name, memento.State, memento.NewOrder).Execute();
            CurrentState = IReversibleAction.Progress.InProgress;
        }

        /// <summary>
        /// Reverts this action, i.e., changes the order in layer back to its old value.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.Obj == null && memento.Id != null)
            {
                memento.Obj = GameFinder.FindChild(memento.Drawable.GetDrawable(), memento.Id);
            }
            switch (memento.State)
            {
                case GameLayerChanger.LayerChangerStates.Increase:
                    GameLayerChanger.Decrease(memento.Obj, memento.OldOrder);
                    break;
                case GameLayerChanger.LayerChangerStates.Decrease:
                    GameLayerChanger.Increase(memento.Obj, memento.OldOrder);
                    break;
                default:
                    throw new NotImplementedException($"Unexpected {memento.State}");
            }

            new LayerChangerNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                                      memento.Obj.name, memento.State, memento.OldOrder).Execute();
        }

        /// <summary>
        /// Repeats this action, i.e., changed the order in layer back to it's new value.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.Obj == null && memento.Id != null)
            {
                memento.Obj = GameFinder.FindChild(memento.Drawable.GetDrawable(), memento.Id);
            }
            switch (memento.State)
            {
                case GameLayerChanger.LayerChangerStates.Increase:
                    GameLayerChanger.Increase(memento.Obj, memento.NewOrder);
                    break;
                case GameLayerChanger.LayerChangerStates.Decrease:
                    GameLayerChanger.Decrease(memento.Obj, memento.NewOrder);
                    break;
                default:
                    throw new NotImplementedException($"Unexpected {memento.State}");
            }
            new LayerChangerNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                                      memento.Obj.name, memento.State, memento.NewOrder).Execute();
        }

        /// <summary>
        /// A new instance of <see cref="LayerChangerAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LayerChangerAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new LayerChangerAction();
        }

        /// <summary>
        /// A new instance of <see cref="LayerChangerAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LayerChangerAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.LayerChanger"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.LayerChanger;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>an empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.Obj == null)
            {
                return new();
            }
            else
            {
                return new() { memento.Obj.name };
            }
        }
    }
}
