using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Net.Actions;
using SEE.Game.Operator;
using SEE.Audio;
using RTG;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to scale a node.
    /// </summary>
    internal class ScaleNodeAction : AbstractPlayerAction
    {
        /// <summary>
        /// This constructor will be used when this kind of action is to be
        /// continued with a new instance where <paramref name="gameNodeToBeContinuedWith"/>
        /// was selected already.
        /// </summary>
        /// <param name="gameNodeToBeContinuedWith">the game node selected already</param>
        private ScaleNodeAction(GameObject gameNodeToBeContinuedWith) : base()
        {
            currentState = ReversibleAction.Progress.NoEffect;
            StartWith(gameNodeToBeContinuedWith);
        }

        /// <summary>
        /// This constructor will be used if no game node was selected in a previous
        /// instance of this type of action.
        /// </summary>
        private ScaleNodeAction() : base()
        {
            currentState = ReversibleAction.Progress.NoEffect;
        }

        /// <summary>
        /// Returns a new instance of <see cref="ScaleNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new ScaleNodeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="ScaleNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            if (gameNodeToBeContinuedInNextAction)
            {
                return new ScaleNodeAction(gameNodeToBeContinuedInNextAction);
            }
            else
            {
                return CreateReversibleAction();
            }
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ScaleNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.ScaleNode;
        }

        /// <summary>
        /// The gameObject that is currently selected and should be scaled.
        /// Will be null if no object has been selected yet.
        /// </summary>
        private GameObject gameNodeSelected;

        /// <summary>
        /// The user may have selected another node after manipulating
        /// <see cref="gameNodeSelected"/>. In this case, the current action
        /// is considered finished and another instance of this type of action
        /// should be continued with the newly selected game node. For that
        /// reason, that newly selected game node will be saved in this
        /// field and then used when a new instance of this class is to
        /// be created.
        /// </summary>
        private GameObject gameNodeToBeContinuedInNextAction;

        /// <summary>
        /// A memento of the position and scale of <see cref="gameNodeSelected"/> before
        /// or after, respectively, it was scaled.
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The local scale at the point in time when the memento was created.
            /// </summary>
            public readonly Vector3 initialLocalScale;

            /// <summary>
            /// The local scale at the point in time when the action has completed.
            /// Required for <see cref="Redo"/>.
            /// </summary>
            private Vector3 finalLocalScale;

            /// <summary>
            /// The <see cref="NodeOperator"/> of the game object to be scaled.
            /// </summary>
            private readonly NodeOperator nodeOperator;

            /// <summary>
            /// Constructor taking a snapshot of the position and scale of <paramref name="gameObject"/>.
            /// </summary>
            /// <param name="gameObject">object whose position and scale are to be captured</param>
            public Memento(GameObject gameObject)
            {
                initialLocalScale = gameObject.transform.localScale;
                nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
            }

            /// <summary>
            /// Sets the final local scale of the manipulated game object to
            /// given <paramref name="newLocalScale"/>. Broadcasts this value
            /// to all clients.
            /// </summary>
            /// <param name="newLocalScale">the new local scale when the action has completed</param>
            public void Finalize(Vector3 newLocalScale)
            {
                finalLocalScale = newLocalScale;
                BroadcastScale(newLocalScale);
            }

            /// <summary>
            /// Reverts the state of the game object that was passed to the
            /// constructor to the state when the constructor was called.
            /// </summary>
            public void Undo()
            {
                Set(initialLocalScale);
            }

            /// <summary>
            /// Sets the state of the game object that was passed to the
            /// constructor to the state passed by <see cref="Finalize(Vector3)"/>.
            /// </summary>
            public void Redo()
            {
                Set(finalLocalScale);
            }

            private void Set(Vector3 localScale)
            {
                // FIXME: If a duration > 0 is used, the node operator does not scale the object.
                nodeOperator.ScaleTo(localScale, 0);
                BroadcastScale(localScale);
            }

            /// <summary>
            /// Broadcasts the <paramref name="localScale"/> to call clients.
            /// </summary>
            /// <param name="localScale"></param>
            private void BroadcastScale(Vector3 localScale)
            {
                new ScaleNodeNetAction(nodeOperator.name, localScale, AbstractOperator.DefaultAnimationDuration).Execute();
            }
        }

        /// <summary>
        /// The memento for <see cref="gameNodeSelected"/>.
        /// This memento is needed for <see cref="Undo"/> and <see cref="Redo"/>.
        /// </summary>
        private Memento memento;

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

        /// <summary
        /// See <see cref="ReversibleAction.Update"/>.
        ///
        /// Note: The action is finalized only if the user selects anything except the
        /// <see cref="gameNodeSelected"/> or any of the scaling gizmos.
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
                        Finalize();
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
                                StartWith(raycastHit.collider.gameObject);
                                return false;
                            }
                            else
                            {
                                // This action is considered finished and a different action should
                                // be started to continue with the newly selected node.
                                Finalize();
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
                        StartWith(raycastHit.collider.gameObject);
                        return false;
                    }
                }
            }
            else if (SEEInput.Drag() || SEEInput.ToggleMenu() || SEEInput.Cancel())
            {
                // TODO: Should we do really react to these interactions?
                Finalize();
                return true;
            }
            return false;

            void Finalize()
            {
                UnityEngine.Assertions.Assert.IsNotNull(gameNodeSelected);
                memento.Finalize(gameNodeSelected.transform.localScale);
                gizmo.Disable();
                currentState = ReversibleAction.Progress.Completed;
                AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DROP_SOUND, gameNodeSelected);
            }

            // Yields true if the object to be manipulated has had a change.
            // Precondition: the object to be manipluated is not null.
            bool HasChanges()
            {
                return gameNodeSelected.transform.localScale != memento.initialLocalScale;
            }
        }

        /// <summary>
        /// Starts the manipulation with the given <paramref name="gameNode"/> considered
        /// as the <see cref="gameNodeSelected"/>. Saves its initial state for later <see cref="Undo"/>
        /// and enables the gizmo.
        /// </summary>
        /// <param name="gameNode">game node to start the manipulation with</param>
        void StartWith(GameObject gameNode)
        {
            gameNodeSelected = gameNode;
            memento = new Memento(gameNodeSelected);
            gizmo.Enable(gameNodeSelected);
            AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.PICKUP_SOUND, gameNodeSelected);
        }

        #region Gizmo

        /// <summary>
        /// The gizmo used to manipulate <see cref="gameNodeSelected"/>.
        /// </summary>
        private readonly Gizmo gizmo = new();

        /// <summary>
        /// Manages the gizmo to manipulate the selected game node.
        /// </summary>
        private class Gizmo
        {
            /// <summary>
            /// Gizmo used for scaling the objects.
            /// </summary>
            private ObjectTransformGizmo objectScaleGizmo;

            /// <summary>
            /// Returns true if the user is currently hovering the gizmo.
            /// </summary>
            /// <returns></returns>
            public bool IsHovered()
            {
                return objectScaleGizmo != null && objectScaleGizmo.Gizmo.IsHovered;
            }

            /// <summary>
            /// Enables the transformation gizmo with <see cref="gameNodeSelected"/> as target.
            /// </summary>
            public void Enable(GameObject gameNodeSelected)
            {
                objectScaleGizmo ??= RTGizmosEngine.Get.CreateObjectScaleGizmo();
                objectScaleGizmo.SetTargetObject(gameNodeSelected);
                objectScaleGizmo.Gizmo.SetEnabled(true);
            }

            /// <summary>
            /// Disables the transformation gizmo.
            /// </summary>
            public void Disable()
            {
                objectScaleGizmo?.Gizmo.SetEnabled(false);
            }
        }
        #endregion

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
    }
}
