using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using System;
using UnityEngine;
using SEE.Net.Actions;
using static SEE.Utils.Raycasting;
using SEE.Game.Operator;

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
        /// The scaling gizmo selected by the user to scale <see cref="objectToScale"/>.
        /// Will be null if none was selected yet.
        /// </summary>
        private GameObject draggedSphere;

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
                if (gameObject.TryGetComponentOrLog(out NodeOperator nodeOperator))
                {
                    nodeOperator.ScaleTo(Scale, 0);
                    nodeOperator.MoveTo(Position, 0);
                }
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

        private void MoveAndScale()
        {
            new ScaleNodeNetAction(objectToScale.name, objectToScale.transform.localScale, 0).Execute();
            new MoveNetAction(objectToScale.name, objectToScale.transform.localScale, 0).Execute();
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

        /// <summary
        /// See <see cref="ReversibleAction.Update"/>.
        ///
        /// Note: The action is finalized only if the user selects anything except the
        /// <see cref="objectToScale"/> or any of the scaling gizmos.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (objectToScale != null)
            {
                // We can scale objectToScale.
                if (!scalingGizmosAreDrawn)
                {
                    DrawGamingGizmos();
                }
                if (SEEInput.Scale())
                {
                    if (draggedSphere == null && Raycasting.RaycastAnything(out RaycastHit raycastHit))
                    {
                        draggedSphere = SelectedScalingGizmo(raycastHit.collider.gameObject);
                    }
                    if (draggedSphere != null)
                    {
                        Scaling();
                    }
                }
            }

            if (SEEInput.Select())
            {
                HitGraphElement hitGraphElement = Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _);
                if (objectToScale != null)
                {
                    // An object to be scaled has been selected already. Yet, we have another selection event.
                    // Is something else selected?
                    if (objectToScale != raycastHit.collider.gameObject)
                    {
                        // The user has selected something different from objectToScale.
                        // Is it one of our scaling gizmos?
                        GameObject selectedScalingGizmo = SelectedScalingGizmo(raycastHit.collider.gameObject);
                        if (selectedScalingGizmo != null)
                        {
                            draggedSphere = selectedScalingGizmo;
                            return false;
                        }
                        else
                        {
                            // Summary: An object to be scaled had been selected. The user then tried another
                            // selection. The user this time neither selected the object to be scaled again nor one
                            // of the scaling gizmos. That means, the action is finished and needs to be
                            // finalized if the user has actually triggered a change at all.
                            if (objectToScale.transform.position != beforeAction.Position
                                || objectToScale.transform.lossyScale != beforeAction.Scale)
                            {
                                currentState = ReversibleAction.Progress.Completed;
                                // Scaling action is finalized.
                                afterAction = new Memento(objectToScale);
                                draggedSphere = null;
                                return true;
                            }
                            else
                            {
                                // Nothing has changed. We will continue with the action.
                                // We continue with the newly selected node if a node was selected.
                                if (hitGraphElement == HitGraphElement.Node)
                                {
                                    objectToScale = raycastHit.collider.gameObject;
                                }
                                else
                                {
                                    objectToScale = null;
                                }
                                RemoveSpheres();
                                draggedSphere = null;
                                return false;
                            }
                        }
                    }
                }
                else if (hitGraphElement == HitGraphElement.Node)
                {
                    // No object to be scaled had been selected yet, but now we have one.
                    objectToScale = raycastHit.collider.gameObject;
                    beforeAction = new Memento(objectToScale);
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Looks at all the incoming and outgoing edges of a node and replaces these edges depending on the new scaling of the node.
        /// </summary>
        private void AdjustEdge()
        {
            GameEdgeMover.MoveAllConnectingEdgesOfNode(objectToScale);
        }

        /// <summary>
        /// Scales <see cref="objectToScale"/> and drags and re-draws the scaling gizmos.
        /// </summary>
        private void Scaling()
        {
            DragSphere(draggedSphere);

            ScaleNode();
            SetOnRoof();
            SetOnSide();
            AdjustSizeOfScalingGizmos();
            AdjustEdge();
        }

        /// <summary>
        /// Adjusts the size of the scaling elements according to the size of <see cref="objectToScale"/>.
        /// </summary>
        private void AdjustSizeOfScalingGizmos()
        {
            SphereRadius(topSphere);
            SphereRadius(firstSideSphere);
            SphereRadius(secondSideSphere);
            SphereRadius(thirdSideSphere);
            SphereRadius(forthSideSphere);
            SphereRadius(firstCornerSphere);
            SphereRadius(secondCornerSphere);
            SphereRadius(thirdCornerSphere);
            SphereRadius(forthCornerSphere);
        }

        /// <summary>
        /// Drags the given <paramref name="scalingGizmo"/> along its axis.
        /// </summary>
        /// <param name="scalingGizmo">scaling gizmo to be dragged</param>
        private void DragSphere(GameObject scalingGizmo)
        {
            // Move the draggedSphere along its axis according to the user's request.
            // Each gizmo is locked to one particular axis.
            if (scalingGizmo == topSphere)
            {
                MoveToLockAxes(scalingGizmo, false, true, false);
            }
            else if (scalingGizmo == firstCornerSphere || scalingGizmo == secondCornerSphere
                     || scalingGizmo == thirdCornerSphere || scalingGizmo == forthCornerSphere)
            {
                MoveToLockAxes(scalingGizmo, true, false, true);
            }
            else if (scalingGizmo == firstSideSphere || scalingGizmo == secondSideSphere)
            {
                MoveToLockAxes(scalingGizmo, true, false, false);
            }
            else if (scalingGizmo == thirdSideSphere || scalingGizmo == forthSideSphere)
            {
                MoveToLockAxes(scalingGizmo, false, false, true);
            }
            else
            {
                throw new ArgumentException($"Unexpected scaling gizmo {scalingGizmo.name}.");
            }
        }

        /// <summary>
        /// Moves the given <paramref name="movingObject"/> on a sphere around the
        /// camera. The radius of this sphere is the original distance
        /// from the <paramref name="movingObject"/> to the camera. The point
        /// on that sphere is determined by a ray driven by the user hitting
        /// this sphere. The speed of travel is defined by <see cref="MOVING_SPEED"/>.
        ///
        /// This method is expected to be called at every Update().
        ///
        /// You can lock any of the three axes.
        /// </summary>
        /// <param name="movingObject">the object to be moved</param>
        /// <param name="lockX">whether the movement should be locked on this axis</param>
        /// <param name="lockY">whether the movement should be locked on this axis</param>
        /// <param name="lockZ">whether the movement should be locked on this axis</param>
        private static void MoveToLockAxes(GameObject movingObject, bool lockX, bool lockY, bool lockZ)
        {
            // The speed by which to move a selected object.
            const float MOVING_SPEED = 1.0f;

            float step = MOVING_SPEED * Time.deltaTime;
            Vector3 target = TipOfRayPosition(movingObject);
            Vector3 movingObjectPos = movingObject.transform.position;

            if (!lockX)
            {
                target.x = movingObjectPos.x;
            }

            if (!lockY)
            {
                target.y = movingObjectPos.y;
            }

            if (!lockZ)
            {
                target.z = movingObjectPos.z;
            }

            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, target, step);

            /// <summary>
            /// Returns the position of the tip of the ray drawn from the camera towards
            /// the position the user is currently pointing to. The distance of that
            /// point along this ray is the distance between the camera from which the
            /// ray originated and the position of the given <paramref name="selectedObject"/>.
            ///
            /// That means, the selected object moves on a sphere around the camera
            /// at the distance of the selected object.
            /// </summary>
            /// <param name="selectedObject">the selected object currently moved around</param>
            /// <returns>tip of the ray</returns>
            static Vector3 TipOfRayPosition(GameObject selectedObject)
            {
                return UserPointsTo().GetPoint(Vector3.Distance(UserPointsTo().origin, selectedObject.transform.position));
            }
        }

        /// <summary>
        /// Draws the gizmos that allow a user to scale the object in all three dimensions.
        /// </summary>
        private void DrawGamingGizmos()
        {
            // Top sphere
            topSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SphereRadius(topSphere);

            // Corner spheres
            firstCornerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SphereRadius(firstCornerSphere);

            secondCornerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SphereRadius(secondCornerSphere);

            thirdCornerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SphereRadius(thirdCornerSphere);

            forthCornerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SphereRadius(forthCornerSphere);

            // Side spheres
            firstSideSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SphereRadius(firstSideSphere);

            secondSideSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SphereRadius(secondSideSphere);

            thirdSideSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SphereRadius(thirdSideSphere);

            forthSideSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SphereRadius(forthSideSphere);

            // Positioning
            SetOnRoof();
            SetOnSide();
            scalingGizmosAreDrawn = true;
        }

        /// <summary>
        /// Sets the new scale of <see cref="objectToScale"/> based on the scaling gizmos.
        /// </summary>
        private void ScaleNode()
        {
            Vector3 scale = Vector3.zero;
            scale.y += topSphere.transform.position.y - topOldSpherePos.y;
            scale.x -= firstSideSphere.transform.position.x - firstSideOldSpherePos.x;
            scale.x += secondSideSphere.transform.position.x - secondSideOldSpherePos.x;
            scale.z -= thirdSideSphere.transform.position.z - thirdSideOldSpherePos.z;
            scale.z += forthSideSphere.transform.position.z - forthSideOldSpherePos.z;

            // Corner scaling
            float scaleCorner = 0;
            scaleCorner -= firstCornerSphere.transform.position.x - firstCornerOldSpherePos.x
                + (firstCornerSphere.transform.position.z - firstCornerOldSpherePos.z);
            scaleCorner += secondCornerSphere.transform.position.x - secondCornerOldSpherePos.x
                - (secondCornerSphere.transform.position.z - secondCornerOldSpherePos.z);
            scaleCorner += thirdCornerSphere.transform.position.x - thirdCornerOldSpherePos.x
                + (thirdCornerSphere.transform.position.z - thirdCornerOldSpherePos.z);
            scaleCorner -= forthCornerSphere.transform.position.x - forthCornerOldSpherePos.x
                - (forthCornerSphere.transform.position.z - forthCornerOldSpherePos.z);

            scale.x += scaleCorner;
            scale.z += scaleCorner;

            // Move the gameObject so the user thinks she/he scaled only in one direction
            Vector3 position = objectToScale.transform.position;
            position.y += scale.y / 2;

            // Setting the old positions
            topOldSpherePos = topSphere.transform.position;
            firstCornerOldSpherePos = firstCornerSphere.transform.position;
            secondCornerOldSpherePos = secondCornerSphere.transform.position;
            thirdCornerOldSpherePos = thirdCornerSphere.transform.position;
            forthCornerOldSpherePos = forthCornerSphere.transform.position;
            firstSideOldSpherePos = firstSideSphere.transform.position;
            secondSideOldSpherePos = secondSideSphere.transform.position;
            thirdSideOldSpherePos = thirdSideSphere.transform.position;
            forthSideOldSpherePos = forthSideSphere.transform.position;

            scale = objectToScale.transform.lossyScale + scale;

            // Fixes negative dimension
            if (scale.x <= 0)
            {
                scale.x = objectToScale.transform.lossyScale.x;
            }
            if (scale.y <= 0)
            {
                scale.y = objectToScale.transform.lossyScale.y;
                position.y = objectToScale.transform.position.y;
            }
            if (scale.z <= 0)
            {
                scale.z = objectToScale.transform.lossyScale.z;
            }

            // Transform the new position and scale
            objectToScale.transform.position = position;
            objectToScale.SetScale(scale);
            MoveAndScale();
            currentState = ReversibleAction.Progress.InProgress;
        }

        /// <summary>
        /// Sets the top scale gizmo at the top of <see cref="objectToScale"/>.
        /// </summary>
        private void SetOnRoof()
        {
            Vector3 pos = objectToScale.transform.position;
            // The scaling sphere is just above the center of the roof of objectToScale.
            pos.y = objectToScale.GetRoof() + ScalingSphereRadius();
            topSphere.transform.position = pos;
            topOldSpherePos = topSphere.transform.position;
        }

        /// <summary>
        /// Returns the radius of the sphere used to visualize the gizmo to scale the object.
        /// </summary>
        /// <returns>radius of the sphere</returns>
        private float ScalingSphereRadius()
        {
            // Assumptions: We assume firstCornerSphere has the same scale as every
            // other scaling sphere and that it is actually a sphere (more precisely,
            // that its width and depth are the same so that we can use the x scale
            // or the z scale; it does not matter).
            return firstCornerSphere.transform.lossyScale.x / 2.0f;
        }

        /// <summary>
        /// Sets the side spheres.
        /// </summary>
        private void SetOnSide()
        {
            Transform trns = objectToScale.transform;
            float sphereRadius = ScalingSphereRadius();
            float xOffset = trns.lossyScale.x / 2 + sphereRadius;
            float zOffset = trns.lossyScale.z / 2 + sphereRadius;

            Vector3 Corner(float xOffset, float zOffset)
            {
                Vector3 result = trns.position;
                result.y = objectToScale.GetRoof();
                result.x += xOffset;
                result.z += zOffset;
                return result;
            }

            // Calulate the positions of the scaling handles at the four corners of the roof.
            {
                // south-west corner
                {
                    Vector3 pos = Corner(-xOffset, -zOffset);
                    firstCornerSphere.transform.position = pos;
                    firstCornerOldSpherePos = pos;
                }
                // south-east corner
                {
                    Vector3 pos = Corner(xOffset, -zOffset);
                    secondCornerSphere.transform.position = pos;
                    secondCornerOldSpherePos = pos;
                }
                // north-east corner
                {
                    Vector3 pos = Corner(xOffset, zOffset);
                    thirdCornerSphere.transform.position = pos;
                    thirdCornerOldSpherePos = pos;
                }
                // north-west corner
                {
                    Vector3 pos = Corner(-xOffset, zOffset);
                    forthCornerSphere.transform.position = pos;
                    forthCornerOldSpherePos = pos;
                }
            }

            // Calulate the positions of the scaling handles at the four sides of the roof.
            {
                // west side
                {
                    Vector3 pos = Corner(-xOffset, 0);
                    firstSideSphere.transform.position = pos;
                    firstSideOldSpherePos = pos;
                }
                // east side
                {
                    Vector3 pos = Corner(xOffset, 0);
                    secondSideSphere.transform.position = pos;
                    secondSideOldSpherePos = pos;
                }
                // south side
                {
                    Vector3 pos = Corner(0, -zOffset);
                    thirdSideSphere.transform.position = pos;
                    thirdSideOldSpherePos = pos;
                }
                // north side
                {
                    Vector3 pos = Corner(0, zOffset);
                    forthSideSphere.transform.position = pos;
                    forthSideOldSpherePos = pos;
                }
            }
        }

        /// <summary>
        /// The minimal scale a scaling sphere may have in world space.
        /// </summary>
        private const float minimalSphereScale = 0.01f;

        /// <summary>
        /// The size of the scaling spheres will be relative to the game object to be scaled.
        /// This factor determines that scale. It will be multiplied by the x or z scale of
        /// <see cref="objectToScale"/> (the smaller of the two). If that value is shorter
        /// than <see cref="minimalSphereScale"/>, <see cref="minimalSphereScale"/> will
        /// be used instead.
        /// </summary>
        private const float relativeSphereScale = 0.1f;

        /// <summary>
        /// Sets the radius of a sphere dependent on the X and Z scale of <paramref name="sphere"/>
        /// that is to be scaled.</summary>
        /// <param name="sphere">the sphere to be scaled</param>
        private void SphereRadius(GameObject sphere)
        {
            Vector3 goScale = objectToScale.transform.lossyScale;
            sphere.transform.localScale = Vector3.one * Mathf.Max(Mathf.Min(goScale.x, goScale.z) * relativeSphereScale, minimalSphereScale);
        }

        /// <summary>
        /// Destroys all scaling gizmos. Sets <see cref="scalingGizmosAreDrawn"/> to false.
        /// </summary>
        public void RemoveSpheres()
        {
            Destroyer.DestroyGameObject(topSphere);
            Destroyer.DestroyGameObject(firstCornerSphere);
            Destroyer.DestroyGameObject(secondCornerSphere);
            Destroyer.DestroyGameObject(thirdCornerSphere);
            Destroyer.DestroyGameObject(forthCornerSphere);
            Destroyer.DestroyGameObject(firstSideSphere);
            Destroyer.DestroyGameObject(secondSideSphere);
            Destroyer.DestroyGameObject(thirdSideSphere);
            Destroyer.DestroyGameObject(forthSideSphere);
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
