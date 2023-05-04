using System.Collections.Generic;
using HighlightPlus;
using SEE.Game;
using SEE.Game.UI3D;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using SEE.Audio;
using SEE.Game.Operator;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to rotate nodes.
    /// </summary>
    internal class RotateAction : AbstractPlayerAction
    {
        /// <summary>
        /// The number of degrees in a full circle.
        /// </summary>
        private const float FullCircleDegree = 360.0f;

        private struct Hit
        {
            /// <summary>
            /// The root of the code city. This is the top-most game object representing a node,
            /// i.e., is tagged by <see cref="Tags.Node"/>.
            /// </summary>
            internal Transform CityRootNode;
            /// <summary>
            /// The node that was hit by the raycast.
            /// </summary>
            internal Transform HitNode;
            internal CityCursor Cursor;
            internal UnityEngine.Plane Plane;
        }

        private const float SnapStepCount = 8;
        private const float SnapStepAngle = FullCircleDegree / SnapStepCount;
        private const int TextureResolution = 1024;
        private static readonly RotateGizmo gizmo = RotateGizmo.Create(TextureResolution);

        private bool rotating;
        private Hit hit;
        private float originalEulerAngleY;
        private Vector3 originalPosition;
        private float startAngle;

        /// <summary>
        /// The operator for the node that is being rotated.
        /// </summary>
        private NodeOperator @operator;

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="RotateAction"/></returns>
        internal static ReversibleAction CreateReversibleAction() => new RotateAction
        {
            rotating = false,
            hit = new Hit(),
            originalEulerAngleY = 0.0f,
            originalPosition = Vector3.zero,
            startAngle = 0.0f
        };
        
        // FIXME: Action is not reversible!

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance() => new RotateAction
        {
            rotating = rotating,
            hit = hit,
            originalEulerAngleY = originalEulerAngleY,
            originalPosition = originalPosition,
            startAngle = startAngle
        };

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Rotate"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Rotate;
        }

        /// <summary>
        /// Returns the set of IDs of all game objects changed by this action.
        /// <see cref="ReversibleAction.GetChangedObjects"/>
        /// </summary>
        /// <returns>empty set because this action does not change anything</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Returns the operator for the given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node to get the operator for.</param>
        /// <returns>the operator for the given <paramref name="node"/></returns>
        private NodeOperator GetOperatorForNode(Component node)
        {
            if (@operator == null || @operator.transform != node)
            {
                @operator = node.gameObject.AddOrGetComponent<NodeOperator>();
            }
            return @operator;
        }

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>always false</returns>
        public override bool Update()
        {
            InteractableObject obj = InteractableObject.HoveredObjectWithWorldFlag;
            Transform cityRootNode = null;
            CityCursor cityCursor = null;

            if (obj)
            {
                cityRootNode = SceneQueries.GetCityRootTransformUpwards(obj.transform);
                cityCursor = cityRootNode.GetComponentInParent<CityCursor>();
            }

            bool synchronize = false;

            if (SEEInput.Cancel()) // cancel rotation
            {
                if (rotating)
                {
                    NodeOperator nodeOperator = GetOperatorForNode(hit.HitNode);
                    nodeOperator.RotateTo(Quaternion.Euler(0, originalEulerAngleY, 0), 0);
                    nodeOperator.MoveTo(originalPosition, 0);
                    foreach (InteractableObject interactable in hit.Cursor.E.GetFocusses())
                    {
                        if (interactable.IsGrabbed)
                        {
                            interactable.SetGrab(false, true);
                        }
                    }
                    gizmo.gameObject.SetActive(false);
                    AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DROP_SOUND);
                    rotating = false;
                    synchronize = true;
                }
                else if (obj)
                {
                    // TODO(torben): Explanation in MoveAction.cs: @UnselectInWrongPlace
                    InteractableObject.UnselectAllInGraph(obj.ItsGraph, true);
                }
            }
            else if (SEEInput.Drag()) // start or continue rotation
            {
                Vector3 planeHitPoint;
                if (cityRootNode)
                {
                    UnityEngine.Plane plane = new(Vector3.up, cityRootNode.position);
                    if (!rotating && Raycasting.RaycastPlane(plane, out planeHitPoint)) // start rotation
                    {
                        AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.PICKUP_SOUND);
                        rotating = true;
                        hit.CityRootNode = cityRootNode;
                        hit.HitNode = obj.transform;
                        hit.Cursor = cityCursor;
                        hit.Plane = plane;

                        foreach (InteractableObject interactable in hit.Cursor.E.GetFocusses())
                        {
                            interactable.SetGrab(true, true);
                        }
                        gizmo.gameObject.SetActive(true);
                        gizmo.Center = cityCursor.E.HasFocus() ? hit.Cursor.E.ComputeCenter() : hit.CityRootNode.position;

                        Vector2 toHit = planeHitPoint.XZ() - gizmo.Center.XZ();
                        float toHitAngle = toHit.Angle360();

                        originalEulerAngleY = cityRootNode.rotation.eulerAngles.y;
                        originalPosition = cityRootNode.position;
                        startAngle = AngleMod(cityRootNode.rotation.eulerAngles.y - toHitAngle);
                        gizmo.StartAngle = gizmo.TargetAngle = Mathf.Deg2Rad * toHitAngle;
                    }
                }

                if (rotating && Raycasting.RaycastPlane(hit.Plane, out planeHitPoint)) // continue rotation
                {
                    Vector2 toHit = planeHitPoint.XZ() - gizmo.Center.XZ();
                    float toHitAngle = toHit.Angle360();
                    float angle = AngleMod(startAngle + toHitAngle);
                    if (SEEInput.Snap())
                    {
                        angle = AngleMod(Mathf.Round(angle / SnapStepAngle) * SnapStepAngle);
                    }

                    NodeOperator nodeOperator = GetOperatorForNode(hit.HitNode);
                    nodeOperator.RotateTo(Quaternion.AngleAxis(angle, Vector3.up), 0);

                    float prevAngle = Mathf.Rad2Deg * gizmo.TargetAngle;
                    float currAngle = toHitAngle;

                    while (Mathf.Abs(currAngle + FullCircleDegree - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                    {
                        currAngle += FullCircleDegree;
                    }
                    while (Mathf.Abs(currAngle - FullCircleDegree - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                    {
                        currAngle -= FullCircleDegree;
                    }
                    if (SEEInput.Snap())
                    {
                        currAngle = Mathf.Round((currAngle + startAngle) / SnapStepAngle) * SnapStepAngle - startAngle;
                    }
                    gizmo.TargetAngle = Mathf.Deg2Rad * currAngle;

                    synchronize = true;
                }
            }
            else if (SEEInput.Reset()) // reset rotation to identity();
            {
                if (obj && !rotating)
                {
                    foreach (InteractableObject interactable in cityCursor.E.GetFocusses())
                    {
                        if (interactable.IsGrabbed)
                        {
                            interactable.SetGrab(false, true);
                        }
                    }
                    gizmo.gameObject.SetActive(false);

                    NodeOperator nodeOperator = GetOperatorForNode(hit.HitNode);
                    nodeOperator.RotateTo(Quaternion.AngleAxis(hit.HitNode.rotation.eulerAngles.y, Vector3.up), 0);
                    synchronize = true;
                }
            }
            else if (rotating) // finalize rotation
            {
                rotating = false;

                foreach (InteractableObject interactable in hit.Cursor.E.GetFocusses())
                {
                    if (interactable.IsGrabbed)
                    {
                        interactable.SetGrab(false, true);
                    }
                }
                AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DROP_SOUND);
                gizmo.gameObject.SetActive(false);
                currentState = ReversibleAction.Progress.Completed;
            }

            if (synchronize)
            {
                new RotateNodeNetAction(hit.CityRootNode.name, hit.CityRootNode.position, hit.CityRootNode.eulerAngles.y).Execute();
            }

            if (currentState != ReversibleAction.Progress.Completed)
            {
                currentState = rotating ? ReversibleAction.Progress.InProgress : ReversibleAction.Progress.NoEffect;
            }

            return true;
        }

        /// <summary>
        /// Converts the given angle in degrees into the range [0, 360) degrees and returns the result.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        /// <returns>The angle in the range [0, 360) degrees.</returns>
        private static float AngleMod(float degrees)
        {
            return (degrees % FullCircleDegree + FullCircleDegree) % FullCircleDegree;
        }
    }
}