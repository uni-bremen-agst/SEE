using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using System;
using UnityEngine;
using SEE.Net.Actions;
using static SEE.Utils.Raycasting;
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
        /// The old position of the top sphere.
        /// </summary>
        private Vector3 topOldSpherePos;

        /// <summary>
        /// The old position of the first corner sphere.
        /// </summary>
        private Vector3 firstCornerOldSpherePos;

        /// <summary>
        /// The old position of the second corner sphere.
        /// </summary>
        private Vector3 secondCornerOldSpherePos;

        /// <summary>
        /// The old position of the third corner sphere.
        /// </summary>
        private Vector3 thirdCornerOldSpherePos;

        /// <summary>
        /// The old position of the forth corner sphere.
        /// </summary>
        private Vector3 forthCornerOldSpherePos;

        /// <summary>
        /// The old position of the first side sphere.
        /// </summary>
        private Vector3 firstSideOldSpherePos;

        /// <summary>
        /// The old position of the second side sphere.
        /// </summary>
        private Vector3 secondSideOldSpherePos;

        /// <summary>
        /// The old position of the third side sphere.
        /// </summary>
        private Vector3 thirdSideOldSpherePos;

        /// <summary>
        /// The old position of the forth side sphere.
        /// </summary>
        private Vector3 forthSideOldSpherePos;

        /// <summary>
        /// The sphere on top of the gameObject to scale.
        /// </summary>
        private GameObject topSphere;

        /// <summary>
        /// The sphere on the first corner of the gameObject to scale.
        /// </summary>
        private GameObject firstCornerSphere; //x0 y0

        /// <summary>
        /// The sphere on the second corner of the gameObject to scale.
        /// </summary>
        private GameObject secondCornerSphere; //x1 y0

        /// <summary>
        /// The sphere on the third corner of the gameObject to scale.
        /// </summary>
        private GameObject thirdCornerSphere; //x1 y1

        /// <summary>
        /// The sphere on the forth corner of the gameObject to scale.
        /// </summary>
        private GameObject forthCornerSphere; //x0 y1

        /// <summary>
        /// The sphere on the first side of the gameObject to scale.
        /// </summary>
        private GameObject firstSideSphere; //x0 y0

        /// <summary>
        /// The sphere on the second side of the gameObject to scale.
        /// </summary>
        private GameObject secondSideSphere; //x1 y0

        /// <summary>
        /// The sphere on the third side of the gameObject to scale.
        /// </summary>
        private GameObject thirdSideSphere; //x1 y1

        /// <summary>
        /// The sphere on the forth side of the gameObject to scale.
        /// </summary>
        private GameObject forthSideSphere; //x0 y1

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
            RemoveSpheres();
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
        /// True if the gizmos that allow a user to scale the object in all three dimensions
        /// are drawn.
        /// </summary>
        private bool scalingGizmosAreDrawn = false;

        /// <summary>
        /// Gizmo used for scaling the objects.
        /// </summary>
        private ObjectTransformGizmo _objectScaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();

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
                Gizmo hoveredGizmo = RTGizmosEngine.Get.HoveredGizmo;
                if (!isGameObject || hoveredGizmo != null)
                {
                    // A gizmo or a Game Object outside the graph was clicked.
                    return false;
                }
                if (objectToScale != raycastHit.collider.gameObject)
                {
                    objectToScale = raycastHit.collider.gameObject;
                    // todo memento
                    _objectScaleGizmo.SetTargetObject(objectToScale);
                    _objectScaleGizmo.SetEnabled(true);
                    AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.PICKUP_SOUND, raycastHit.collider.gameObject);
                }
            }

            return false;
        }

        /// <summary>
        /// Destroys all scaling gizmos. Sets <see cref="scalingGizmosAreDrawn"/> to false.
        /// </summary>
        public void RemoveSpheres()
        {
            Destroyer.Destroy(topSphere);
            Destroyer.Destroy(firstCornerSphere);
            Destroyer.Destroy(secondCornerSphere);
            Destroyer.Destroy(thirdCornerSphere);
            Destroyer.Destroy(forthCornerSphere);
            Destroyer.Destroy(firstSideSphere);
            Destroyer.Destroy(secondSideSphere);
            Destroyer.Destroy(thirdSideSphere);
            Destroyer.Destroy(forthSideSphere);
            scalingGizmosAreDrawn = false;
        }

        /// <summary>
        /// If <paramref name="gameObject"/> is any of our scaling gizmos,
        /// this gizmo will be returned; otherwise null
        /// </summary>
        /// <param name="gameObject">the hit game object</param>
        /// <returns><paramref name="gameObject"/> if it is one of our scaling gizmos or null</returns>
        private GameObject SelectedScalingGizmo(GameObject gameObject)
        {
            if (!scalingGizmosAreDrawn)
            {
                return null;
            }
            else if (gameObject == topSphere
                     || gameObject == firstCornerSphere
                     || gameObject == secondCornerSphere
                     || gameObject == thirdCornerSphere
                     || gameObject == forthCornerSphere
                     || gameObject == firstSideSphere
                     || gameObject == secondSideSphere
                     || gameObject == thirdSideSphere
                     || gameObject == forthSideSphere)
            {
                return gameObject;
            }
            else
            {
                return null;
            }
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