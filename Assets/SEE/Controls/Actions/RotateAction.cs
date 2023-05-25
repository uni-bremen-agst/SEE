using System.Collections.Generic;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using SEE.Audio;
using RTG;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to rotate nodes.
    /// </summary>
    internal class RotateAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new RotateAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Rotate"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Rotate;
        }

        /// <summary>
        /// The gameObject that is currently selected and should be rotated.
        /// Will be null if no object has been selected yet.
        /// </summary>
        private GameObject objectToRotate;

        /// <summary>
        /// The rotation of <see cref="objectToRotate"/> before this action actually rotated it,
        /// i.e., its orginial rotation. This value is needed for <see cref="Undo"/>.
        /// </summary>
        private Quaternion originalRotation;

        /// <summary>
        /// The rotation of <see cref="objectToRotate"/> after this action rotated it,
        /// i.e., its new rotation. This value is needed for <see cref="Redo"/>.
        /// </summary>
        private Quaternion newRotation;

        /// <summary>
        /// Reverts the position and rotation of <paramref name="gameObject"/> to
        /// <see cref="Position"/> and <see cref="Rotation"/>.
        /// </summary>
        /// <param name="gameObject">object whose position and rotation are to be restored</param>
        public void Rotate(Quaternion Rotation)
        {
            NodeOperator nodeOperator = objectToRotate.AddOrGetComponent<NodeOperator>();
            nodeOperator.RotateTo(Rotation, 0);
            // TODO: We do not need lists. Only one object can be rotated.
            new RotateNodeNetAction(new List<GameObject>() { objectToRotate }).Execute();
        }

        /// <summary>
        /// Undoes this <see cref="RotateAction"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            Rotate(originalRotation);
        }

        /// <summary>
        /// Redoes this <see cref="RotateAction"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            Rotate(newRotation);
        }

        /// <summary>
        /// Gizmo used for rotating the objects.
        /// </summary>
        private ObjectTransformGizmo _objectRotationGizmo = RTGizmosEngine.Get.CreateObjectRotationGizmo();

        /// <summary>
        /// The gizmo currently hovered, null unless a gizmo is hovered (updated externally).
        /// </summary>
        private Gizmo hoveredGizmo;

        /// <summary
        /// See <see cref="ReversibleAction.Update"/>.
        ///
        /// Note: The action is finalized only if the user selects anything except the
        /// <see cref="objectToRotate"/> or any of the rotation gizmos.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            hoveredGizmo = RTGizmosEngine.Get.HoveredGizmo;
            if (hoveredGizmo != null && hoveredGizmo.IsHovered)
            {
                // Rotation in progress
                return false;
            }

            if (SEEInput.Select())
            {
                if (Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.Node)
                {
                    // Object outside the graph was selected, should be ignored.
                    SaveRotationChanges();
                    DisableGizmo();
                    return true;
                }
                else if (objectToRotate != raycastHit.collider.gameObject)
                {
                    // Selected a different object - save changes and change object assigned to gizmo.
                    SaveRotationChanges();
                    objectToRotate = raycastHit.collider.gameObject;
                    originalRotation = objectToRotate.transform.rotation;
                    _objectRotationGizmo.SetTargetObject(objectToRotate);
                    _objectRotationGizmo.SetEnabled(true);
                    AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.PICKUP_SOUND, objectToRotate);
                }
            }
            else if (SEEInput.Drag() || SEEInput.ToggleMenu() || SEEInput.Cancel())
            {
                SaveRotationChanges();
                DisableGizmo();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a memento after the rotation is done and propagates over network.
        /// </summary>
        private void SaveRotationChanges()
        {
            newRotation = objectToRotate.transform.rotation;
        }

        /// <summary>
        /// Empty game object used for hiding gizmos since they cant be deleted after instantiation.
        /// </summary>
        private GameObject gizmoHidingObject;

        /// <summary>
        /// Disables the rotation gizmo.
        /// </summary>
        private void DisableGizmo()
        {
            if (gizmoHidingObject == null)
            {
                GameObject searchObject = GameObject.Find("Gizmo Hiding Object");
                if (searchObject == null)
                {
                    gizmoHidingObject = new GameObject("Gizmo Hiding Object");
                }
                else
                {
                    gizmoHidingObject = searchObject;
                }
            }
            _objectRotationGizmo.SetTargetObject(gizmoHidingObject);
            _objectRotationGizmo.SetEnabled(false);
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return objectToRotate == null ? new HashSet<string>() : new HashSet<string>()
            {
                objectToRotate.name
            };
        }
    }
}
