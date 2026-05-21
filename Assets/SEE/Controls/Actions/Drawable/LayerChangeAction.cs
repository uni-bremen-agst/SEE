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
using SEE.Game.Drawable.ActionHelpers;
using SEE.UI;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the layer order of a <see cref="DrawableType"/>
    /// </summary>
    class LayerChangeAction : DrawableAction
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
        /// This struct can store all the information needed to revert or repeat a <see cref="LayerChangeAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The drawable surface on which the object to be layer changed is located.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// Is the state of the layer change.
            /// </summary>
            public readonly GameLayerChanger.LayerChangerStates State;
            /// <summary>
            /// The object whose order should be changed in the layering.
            /// </summary>
            public GameObject Obj;
            /// <summary>
            /// The ID of the object to be changed.
            /// </summary>
            public readonly string ID;
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
            /// <param name="surface">The drawable surface to save into this Memento.</param>
            /// <param name="state">The state to save into this Memento.</param>
            /// <param name="obj">The object to save into this Memento.</param>
            /// <param name="id">The object ID to save into this Memento.</param>
            /// <param name="oldOrder">The old order in layer value to save into this Memento.</param>
            /// <param name="newOrder">The new order in layer value to save into this Memento.</param>
            public Memento(GameObject surface, GameLayerChanger.LayerChangerStates state,
                GameObject obj, string id, int oldOrder, int newOrder)
            {
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                State = state;
                Obj = obj;
                ID = id;
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
                UICanvas.Canvas.AddOrGetComponent<ValueResetter>().SetAllowedState(GetActionStateType());
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.LayerChanger"/>.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// It increses or decreases the order in layer of a <see cref="DrawableType"/> object
                /// with a <see cref="OrderInLayerValueHolder"/> component depending on the left or right click.
                if (!isInAction && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                    && GameFinder.HasDrawableSurface(raycastHit.collider.gameObject)
                    && raycastHit.collider.gameObject.GetComponent<OrderInLayerValueHolder>() != null)
                {
                    if (SEEInput.LeftMouseInteraction())
                    {
                        CalcOrder(raycastHit.collider.gameObject, GameLayerChanger.LayerChangerStates.Increase);
                    } else if (SEEInput.RightMouseInteraction()) {
                        CalcOrder(raycastHit.collider.gameObject, GameLayerChanger.LayerChangerStates.Decrease);
                    }
                }
                /// It completes the action after a layer change if the user releases the pressed mouse button.
                if ((SEEInput.MouseUp(MouseButton.Left) || SEEInput.MouseUp(MouseButton.Right)) && isInAction)
                {
                    isInAction = false;
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Calculates the new order in layer.
        /// </summary>
        /// <param name="hitObject">The object which order should be changed.</param>
        /// <param name="state">The state if the order should increased or decreased.</param>
        private void CalcOrder(GameObject hitObject, GameLayerChanger.LayerChangerStates state)
        {
            isInAction = true;
            GameObject surface = GameFinder.GetDrawableSurface(hitObject);
            int oldOrder = hitObject.GetComponent<OrderInLayerValueHolder>().OrderInLayer;
            int newOrder = oldOrder;
            if (state == GameLayerChanger.LayerChangerStates.Increase)
            {
                newOrder = oldOrder + 1;
            } else
            {
                newOrder = oldOrder - 1;
                newOrder = newOrder < 0 ? 0 : newOrder;
            }
            GameLayerChanger.ChangeOrderInLayer(hitObject, newOrder, state);
            memento = new Memento(surface, state,
                            hitObject, hitObject.name, oldOrder, newOrder);
            new LayerChangerNetAction(memento.Surface.ID, memento.Surface.ParentID,
                memento.Obj.name, memento.State, memento.NewOrder).Execute();
            CurrentState = IReversibleAction.Progress.InProgress;
        }

        /// <summary>
        /// Reverts this action, i.e., changes the order in layer back to its old value.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.Obj == null && memento.ID != null)
            {
                memento.Obj = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.ID);
            }
            GameLayerChanger.ChangeOrderInLayer(memento.Obj, memento.OldOrder, memento.State);
            new LayerChangerNetAction(memento.Surface.ID, memento.Surface.ParentID,
                                      memento.Obj.name, memento.State, memento.OldOrder).Execute();
        }

        /// <summary>
        /// Repeats this action, i.e., changed the order in layer back to it's new value.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.Obj == null && memento.ID != null)
            {
                memento.Obj = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.ID);
            }
            GameLayerChanger.ChangeOrderInLayer(memento.Obj, memento.NewOrder, memento.State);
            new LayerChangerNetAction(memento.Surface.ID, memento.Surface.ParentID,
                                      memento.Obj.name, memento.State, memento.NewOrder).Execute();
        }

        /// <summary>
        /// A new instance of <see cref="LayerChangeAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="LayerChangeAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new LayerChangeAction();
        }

        /// <summary>
        /// A new instance of <see cref="LayerChangeAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>New instance of <see cref="LayerChangeAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.LayerChanger"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.LayerChanger;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>An empty set.</returns>
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
