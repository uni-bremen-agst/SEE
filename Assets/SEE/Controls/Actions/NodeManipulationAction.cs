using RTG;
using SEE.Audio;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Abstract superclass of actions transforming a game node by way of a RTG gizmo.
    /// </summary>
    /// <typeparam name="T">type of the state that is transformed</typeparam>
    internal abstract class NodeManipulationAction<T> : AbstractPlayerAction
    {
        #region ReversibleAction Overrides

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return gameNodeSelected == null ? new HashSet<string>() : new HashSet<string>()
            {
                gameNodeSelected.name
            };
        }

        #endregion ReversibleAction Overrides

        #region Undo Redo

        /// <summary>
        /// Un-does this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            memento.Undo();
        }

        /// <summary>
        /// Re-does this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            memento.Redo();
        }

        /// <summary>
        /// Disables the transformation gizmo.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            gizmo.Disable();
        }

        #endregion Undo Redo

        #region Update

        /// <summary>
        /// The gameObject that is currently selected and should be transformed.
        /// Will be null if no object has been selected yet.
        /// </summary>
        protected GameObject gameNodeSelected;

        /// <summary>
        /// The user may have selected another node after manipulating
        /// <see cref="gameNodeSelected"/>. In this case, the current action
        /// is considered finished and another instance of this type of action
        /// should be continued with the newly selected game node. For that
        /// reason, that newly selected game node will be saved in this
        /// field and then used when a new instance of this class is to
        /// be created.
        /// </summary>
        protected GameObject gameNodeToBeContinuedInNextAction;

        /// <summary
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (gizmo.IsHovered())
            {
                // Scaling in progress
                if (gameNodeSelected && HasChanges())
                {
                    currentState = ReversibleAction.Progress.InProgress;
                }
                return false;
            }

            if (SEEInput.Select())
            {
                if (Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.Node)
                {
                    // An object different from a graph node was selected.

                    if (gameNodeSelected && HasChanges())
                    {
                        // An object to be manipulated was selected already and it was changed.
                        // The action is finished.
                        FinalizeAction();
                        return true;
                    }
                    else
                    {
                        // No game node has been selected yet or the previously selected game node
                        // has had no changes. The action is continued.
                        return false;
                    }
                }
                else
                {
                    // A game node was selected by the user.
                    // Has the user already selected a game node in a previous iteration?
                    if (gameNodeSelected)
                    {
                        // The user has already selected a game node in a previous iteration.
                        // Are the two game nodes different?
                        if (gameNodeSelected != raycastHit.collider.gameObject)
                        {
                            // The newly and previously selected nodes are different.

                            // Have we had any changes yet? If not, we assume the user wants
                            // to manipulate the newly game node instead.
                            if (!HasChanges())
                            {
                                StartAction(raycastHit.collider.gameObject);
                                return false;
                            }
                            else
                            {
                                // This action is considered finished and a different action should
                                // be started to continue with the newly selected node.
                                FinalizeAction();
                                gameNodeToBeContinuedInNextAction = raycastHit.collider.gameObject;
                                return true;
                            }
                        }
                        else
                        {
                            // The user has selected the same node again.
                            // Nothing to be done.
                            return false;
                        }
                    }
                    else
                    {
                        // It's the first time, a game node was selected. The action starts.
                        StartAction(raycastHit.collider.gameObject);
                        return false;
                    }
                }
            }
            else if (SEEInput.Drag() || SEEInput.ToggleMenu() || SEEInput.Cancel())
            {
                // TODO: Should we do really react to these interactions?
                FinalizeAction();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Starts the manipulation with the given <paramref name="gameNode"/> considered
        /// as the <see cref="gameNodeSelected"/>. Saves its initial state for later <see cref="Undo"/>
        /// and enables the gizmo.
        /// </summary>
        /// <param name="gameNode">game node to start the manipulation with</param>
        protected void StartAction(GameObject gameNode)
        {
            gameNodeSelected = gameNode;
            memento = CreateMemento(gameNodeSelected);
            gizmo.Enable(gameNodeSelected);
            AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.PICKUP_SOUND, gameNodeSelected);
        }

        /// <summary>
        /// Finalizes the action: set the final state of the memento,
        /// disables <see cref="gizmo"/>, marks the action as <see cref="ReversibleAction.Progress.Completed"/>,
        /// and plays a final sound.
        /// This method should be called when the action is completed.
        /// </summary>
        protected virtual void FinalizeAction()
        {
            UnityEngine.Assertions.Assert.IsNotNull(gameNodeSelected);
            gizmo.Disable();
            currentState = ReversibleAction.Progress.Completed;
            AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DROP_SOUND, gameNodeSelected);
        }

        /// <summary>
        /// Yields true if <see cref="gameNodeSelected"/> has had a change.
        /// Precondition: <see cref="gameNodeSelected"/> is not null.
        /// </summary>
        /// <returns>true if <see cref="gameNodeSelected"/> has had a change</returns>
        protected abstract bool HasChanges();

        #endregion Update

        #region Memento

        /// <summary>
        /// Creates and returns a memento to memorize the state of <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">game node whose state is to be memorized</param>
        /// <returns>memento for <paramref name="gameNode"/></returns>
        protected abstract Memento<T> CreateMemento(GameObject gameNode);

        /// <summary>
        /// The memento for <see cref="gameNodeSelected"/>.
        /// This memento is needed for <see cref="Undo"/> and <see cref="Redo"/>.
        /// </summary>
        protected Memento<T> memento;

        /// <summary>
        /// A memento of the intial and final state of the game object being transformed.
        /// It will be used for <see cref="Undo"/> and <see cref="Redo"/>.
        /// </summary>
        /// <typeparam name="T">the type of the state being memorized</typeparam>
        protected abstract class Memento<T>
        {
            /// <summary>
            /// Constructors setting up the <see cref="nodeOperator"/>.
            /// </summary>
            /// <param name="gameObject">game object to be transformed</param>
            public Memento(GameObject gameObject)
            {
                nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
            }

            /// <summary>
            /// Reverts the state of the game object that was passed to the
            /// constructor to the state when the constructor was called.
            /// </summary>
            public void Undo()
            {
                Transform(InitialState);
            }

            /// <summary>
            /// Sets the state of the game object that was passed to the
            /// constructor to the state passed by <see cref="Finalize(Vector3)"/>.
            /// </summary>
            public void Redo()
            {
                Transform(finalState);
            }

            /// <summary>
            /// Sets the final state of the manipulated game object to given
            /// <paramref name="finalState"/>. Broadcasts this value to all clients.
            /// </summary>
            /// <param name="finalState">the final state when the action has completed</param>
            public void Finalize(T finalState)
            {
                this.finalState = finalState;
                BroadcastState(finalState);
            }

            /// <summary>
            /// Transforms the object using <see cref="nodeOperator"/> to the given
            /// <paramref name="value"/>. Broadcasts this state to all clients.
            /// </summary>
            /// <param name="value"></param>
            protected virtual void Transform(T value)
            {
                BroadcastState(value);
            }

            /// <summary>
            /// Broadcasts the <paramref name="state"/> to all clients.
            /// </summary>
            /// <param name="state">state to be broadcast</param>
            protected abstract void BroadcastState(T state);

            /// <summary>
            /// The local scale at the point in time when the memento was created.
            /// </summary>
            public T InitialState;

            /// <summary>
            /// The local scale at the point in time when the action has completed.
            /// Required for <see cref="Redo"/>.
            /// </summary>
            protected T finalState;

            /// <summary>
            /// The <see cref="NodeOperator"/> of the game object to be transformed.
            /// </summary>
            protected NodeOperator nodeOperator;
        }

        #endregion Memento

        #region Gizmo

        /// <summary>
        /// The gizmo used to manipulate <see cref="gameNodeSelected"/>.
        /// </summary>
        protected Gizmo gizmo;

        protected abstract class Gizmo
        {
            /// <summary>
            /// Gizmo used for transforming the object.
            /// </summary>
            protected ObjectTransformGizmo objectTransformationGizmo;

            /// <summary>
            /// Disables the transformation gizmo.
            /// </summary>
            public void Disable()
            {
                objectTransformationGizmo?.Gizmo.SetEnabled(false);
            }

            /// <summary>
            /// Enables the transformation gizmo with <see cref="gameNodeSelected"/> as target.
            /// </summary>
            public void Enable(GameObject gameNodeSelected)
            {
                objectTransformationGizmo.SetTargetObject(gameNodeSelected);
                objectTransformationGizmo.Gizmo.SetEnabled(true);
            }

            /// <summary>
            /// Returns true if the user is currently hovering the gizmo.
            /// </summary>
            /// <returns></returns>
            public bool IsHovered()
            {
                return objectTransformationGizmo != null && objectTransformationGizmo.Gizmo.IsHovered;
            }
        }

        #endregion
    }
}