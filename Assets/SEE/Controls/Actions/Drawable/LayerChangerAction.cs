using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Game.UI.Drawable;
using SEE.Game.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the layer order of a <see cref="DrawableType"/>
    /// </summary>
    class LayerChangerAction : AbstractPlayerAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Bool to identifiy if the action is running.
        /// </summary>
        private bool isInAction = false;

        /// <summary>
        /// Indicates whether information about the control has already been displayed.
        /// </summary>
        public static bool showInfo = false;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="LayerChangerAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The drawable on which the object to be layer changed is located.
            /// </summary>
            public readonly DrawableConfig drawable;
            /// <summary>
            /// Is the state of the layer change.
            /// </summary>
            public readonly GameLayerChanger.LayerChangerStates state;
            /// <summary>
            /// The object that should be changed his order in layer.
            /// </summary>
            public GameObject obj;
            /// <summary>
            /// The id of the object to be changed.
            /// </summary>
            public readonly string id;
            /// <summary>
            /// The old order in layer value.
            /// </summary>
            public readonly int oldOrder;
            /// <summary>
            /// The new order in layer value.
            /// </summary>
            public readonly int newOrder;

            /// <summary>
            /// The constructor, whcih simply assigns its only parameter to a field in this struct.
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
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.state = state;
                this.obj = obj;
                this.id = id;
                this.oldOrder = oldOrder;
                this.newOrder = newOrder;
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
            if (!showInfo)
            {
                showInfo = true;
                ShowNotification.Info("Usage note", "Use the left mouse button to increase the layer." +
                    "\nUse the right mouse button to decrease the layer.");
                GameObject.Find("UI Canvas").AddComponent<ValueResetter>().SetAllowedState(GetActionStateType());
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.LayerChanger"/>.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        { 
            if (!Raycasting.IsMouseOverGUI())
            {
                /// Increse block
                /// it increses the order in layer of a <see cref="DrawableType"/> object 
                /// with a <see cref="OrderInLayerValueHolder"/> component.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !isInAction 
                    && Raycasting.RaycastAnything(out RaycastHit raycastHit) 
                    && GameFinder.hasDrawable(raycastHit.collider.gameObject) 
                    && raycastHit.collider.gameObject.GetComponent<OrderInLayerValueHolder>() != null)
                {
                    Increase(raycastHit.collider.gameObject);
                }

                /// Decrease block
                /// it decreases the order in layer of a <see cref="DrawableType"/> object 
                /// with a <see cref="OrderInLayerValueHolder"/> component.
                if ((Input.GetMouseButtonDown(1) || Input.GetMouseButton(1)) && !isInAction 
                    && Raycasting.RaycastAnything(out RaycastHit raycastHit2) 
                    && GameFinder.hasDrawable(raycastHit2.collider.gameObject)
                    && raycastHit2.collider.gameObject.GetComponent<OrderInLayerValueHolder>() != null)
                {
                    Decrease(raycastHit2.collider.gameObject);
                }

                /// The completes block. 
                /// It completes the action after a layer changing, if the user releases the pressed mouse button.
                if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) ) && isInAction)
                {
                    isInAction = false;
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Increases the order in layer by one. 
        /// Additionally, a memento of this action is created, and the progress state is set to InProgress.
        /// </summary>
        /// <param name="hittedObject">The object to increase the order</param>
        private void Increase(GameObject hittedObject)
        {
            isInAction = true;
            GameObject drawable = GameFinder.GetDrawable(hittedObject);

            int oldOrder = hittedObject.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();
            int newOrder = oldOrder + 1;
            GameLayerChanger.Increase(hittedObject, newOrder);
            memento = new Memento(drawable, GameLayerChanger.LayerChangerStates.Increase,
                            hittedObject, hittedObject.name, oldOrder, newOrder);
            new LayerChangerNetAction(memento.drawable.ID, memento.drawable.ParentID,
                memento.obj.name, memento.state, memento.newOrder).Execute();
            currentState = ReversibleAction.Progress.InProgress;
        }

        /// <summary>
        /// Decreases the order in layer by one.
        /// Additionally, a memento of this action is created, and the progress state is set to InProgress.
        /// </summary>
        /// <param name="hittedObject">The object to decrease the order.</param>
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
            new LayerChangerNetAction(memento.drawable.ID, memento.drawable.ParentID,
                memento.obj.name, memento.state, memento.newOrder).Execute();
            currentState = ReversibleAction.Progress.InProgress;
        }

        /// <summary>
        /// Reverts this action, i.e., changed the order in layer back to it's old value.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.obj == null && memento.id != null)
            {
                memento.obj = GameFinder.FindChild(memento.drawable.GetDrawable(), memento.id);
            }
            switch (memento.state)
            {
                case GameLayerChanger.LayerChangerStates.Increase:
                    GameLayerChanger.Decrease(memento.obj, memento.oldOrder);
                    new LayerChangerNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                        memento.obj.name, GameLayerChanger.LayerChangerStates.Decrease, 
                        memento.oldOrder).Execute();
                    break;
                case GameLayerChanger.LayerChangerStates.Decrease:
                    GameLayerChanger.Increase(memento.obj, memento.oldOrder);
                    new LayerChangerNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                        memento.obj.name, GameLayerChanger.LayerChangerStates.Increase, 
                        memento.oldOrder).Execute();
                    break;
            }
        }

        /// <summary>
        /// Repeats this action, i.e., changed the order in layer back to it's new value.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.obj == null && memento.id != null)
            {
                memento.obj = GameFinder.FindChild(memento.drawable.GetDrawable(), memento.id);
            }
            switch (memento.state)
            {
                case GameLayerChanger.LayerChangerStates.Increase:
                    GameLayerChanger.Increase(memento.obj, memento.newOrder);
                    break;
                case GameLayerChanger.LayerChangerStates.Decrease:
                    GameLayerChanger.Decrease(memento.obj, memento.newOrder);
                    break;
            }
            new LayerChangerNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                memento.obj.name, memento.state, memento.newOrder).Execute();
        }

        /// <summary>
        /// A new instance of <see cref="LayerChangerAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LayerChangerAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new LayerChangerAction();
        }

        /// <summary>
        /// A new instance of <see cref="LayerChangerAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LayerChangerAction"/></returns>
        public override ReversibleAction NewInstance()
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
