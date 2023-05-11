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
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ScaleNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.ScaleNode;
        }

        /// <summary>
        /// The gameObject that is currently selected and should be scaled.
        /// Will be null if no object has been selected yet.
        /// </summary>
        private GameObject objectToScale;

        /// <summary>
        /// A memento of the position and scale of <see cref="objectToScale"/> before
        /// or after, respectively, it was scaled.
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The scale at the point in time when the memento was created (in world space).
            /// </summary>
            public readonly Vector3 Scale;

            /// <summary>
            /// The position at the point in time when the memento was created (in world space).
            /// </summary>
            public readonly Vector3 Position;

            /// <summary>
            /// Constructor taking a snapshot of the position and scale of <paramref name="gameObject"/>.
            /// </summary>
            /// <param name="gameObject">object whose position and scale are to be captured</param>
            public Memento(GameObject gameObject)
            {
                Position = gameObject.transform.position;
                Scale = gameObject.transform.lossyScale;
            }

            /// <summary>
            /// Reverts the position and scale of <paramref name="gameObject"/> to
            /// <see cref="Position"/> and <see cref="Scale"/>.
            /// </summary>
            /// <param name="gameObject">object whose position and scale are to be restored</param>
            public void Revert(GameObject gameObject)
            {
                NodeOperator nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
                nodeOperator.ScaleTo(Scale, 0);
                nodeOperator.MoveTo(Position, 0);
            }
        }

        /// <summary>
        /// Removes all scaling gizmos.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
        }

        /// <summary>
        /// The memento for <see cref="objectToScale"/> before the action begun,
        /// that is, the original values. This memento is needed for <see cref="Undo"/>.
        /// </summary>
        private Memento beforeAction;

        /// <summary>
        /// The memento for <see cref="objectToScale"/> after the action was completed,
        /// that is, the values after the scaling. This memento is needed for <see cref="Redo"/>.
        /// </summary>
        private Memento afterAction;

        /// <summary>
        /// Undoes this ScaleNodeAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            beforeAction.Revert(objectToScale);
            MoveAndScale();
        }

        /// <summary>
        /// Scales and moves <see cref="objectToScale"/> in all clients to its current localScale and position.
        /// </summary>
        private void MoveAndScale()
        {
            new ScaleNodeNetAction(objectToScale.name, objectToScale.transform.localScale, 0).Execute();
            new MoveNetAction(objectToScale.name, objectToScale.transform.position, 0).Execute();
        }

        /// <summary>
        /// Redoes this ScaleNodeAction.
        /// </summary>
        public override void Redo()
        {
            if (afterAction != null)
            {
                // The user might have canceled the scaling operation, in which case
                // afterAction will be null. Only if something has actually changed,
                // we need to re-do the action.
                base.Redo();
                afterAction.Revert(objectToScale);
                MoveAndScale();
            }
        }

        /// <summary>
        /// Gizmo used for scaling the objects.
        /// </summary>
        private ObjectTransformGizmo _objectScaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
        private GameObject gizmoHidingObject = new GameObject("Gizmo Hiding Object");

        /// <summary
        /// See <see cref="ReversibleAction.Update"/>.
        ///
        /// Note: The action is finalized only if the user selects anything except the
        /// <see cref="objectToScale"/> or any of the scaling gizmos.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (SEEInput.Select())
            {
                HitGraphElement hitGraphElement = Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _);
                bool isGameObject = hitGraphElement == HitGraphElement.Node;

                if (!isGameObject)
                {
                    // Object outside the graph was selected, should be ignored.
                    _objectScaleGizmo.SetTargetObject(gizmoHidingObject);
                    _objectScaleGizmo.SetEnabled(false);
                    return true;
                }
                Gizmo hoveredGizmo = RTGizmosEngine.Get.HoveredGizmo;
                if (hoveredGizmo != null && objectToScale != null)
                {
                    // Hovered gizmo was selected, create memento
                    beforeAction = new Memento(objectToScale);
                }
                if (objectToScale != raycastHit.collider.gameObject)
                {
                    if (objectToScale != null)
                    {
                        afterAction = new Memento(objectToScale);
                    }
                    objectToScale = raycastHit.collider.gameObject;
                    _objectScaleGizmo.SetTargetObject(objectToScale);
                    _objectScaleGizmo.SetEnabled(true);
                    AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.PICKUP_SOUND, objectToScale);
                }
            }
            return false;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>()
            {
                objectToScale.name
            };
        }
    }
}