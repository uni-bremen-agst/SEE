using SEE.Game;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to scale an existing node.
    /// </summary>
    public class ScaleNodeAction : AbstractPlayerAction
    {
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
        /// The scale at the start so the user can reset the changes made during scaling.
        /// </summary>
        private Vector3 originalScale;

        /// <summary>
        /// The position at the start so the user can reset the changes made during scaling.
        /// </summary>
        private Vector3 originalPosition;

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
        /// The gameObject which will end the scaling and start the save process.
        /// </summary>
        private GameObject endWithSave;

        /// <summary>
        /// The gameObject which will end the scaling process and start the discard changes process.
        /// </summary>
        private GameObject endWithOutSave;

        /// <summary>
        /// The gameObject in which will be saved which sphere was dragged.
        /// </summary>
        private GameObject draggedSphere;

        /// <summary>
        /// The gameObject which should be scaled.
        /// </summary>
        private GameObject objectToScale;

        /// <summary>
        /// A copy of <see cref="objectToScale"/>, temporarily saved for undo.
        /// </summary>
        private GameObject temporaryCopy;

        /// <summary>
        /// A copy of the scale in order to set the scale to its original after a redo operation.
        /// </summary>
        private Vector3 scaleCopy;

        /// <summary>
        /// Registers for local hovering.
        /// </summary>
        public override void Start()
        {
            InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
        }

        /// <summary>
        /// Unregisters from local hovering.
        /// Removes the scaling spheres after finishing the action or more
        /// explicitly canceling the action and switch to another.
        /// </summary>
        public override void Stop()
        {
            InteractableObject.LocalAnyHoverIn -= LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut -= LocalAnyHoverOut;
            RemoveSpheres();
        }

        /// <summary>
        /// True if the gizmos that allow a user to scale the object in all three dimensions
        /// are drawn.
        /// </summary>
        private bool scalingGizmosAreDrawn = false;

        /// <summary
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            if (objectToScale != null && !scalingGizmosAreDrawn)
            {
                // We draw the gizmos that allow a user to scale the object in all three dimensions.

                originalScale = objectToScale.transform.lossyScale;
                originalPosition = objectToScale.transform.position;

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

                // End operations
                endWithSave = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                SphereRadius(endWithSave);
                endWithSave.GetComponent<Renderer>().material.color = Color.green;

                endWithOutSave = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                SphereRadius(endWithOutSave);
                endWithOutSave.GetComponent<Renderer>().material.color = Color.red;

                // Positioning
                SetOnRoof();
                SetOnSide();
                scalingGizmosAreDrawn = true;
            }
            if (Input.GetMouseButtonDown(0) && objectToScale == null)
            {
                objectToScale = hoveredObject;
                temporaryCopy = hoveredObject;
            }
            if (scalingGizmosAreDrawn && Input.GetMouseButton(0))
            {
                if (draggedSphere == null)
                {
                    Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);

                    // Casts the ray and get the first game object hit
                    Physics.Raycast(ray, out RaycastHit hit);

                    // Moves the sphere that was hit.
                    // Top
                    if (hit.collider == topSphere.GetComponent<Collider>())
                    {
                        draggedSphere = topSphere;
                    } // Corners
                    else if (hit.collider == firstCornerSphere.GetComponent<Collider>())
                    {
                        draggedSphere = firstCornerSphere;
                    }
                    else if (hit.collider == secondCornerSphere.GetComponent<Collider>())
                    {
                        draggedSphere = secondCornerSphere;
                    }
                    else if (hit.collider == thirdCornerSphere.GetComponent<Collider>())
                    {
                        draggedSphere = thirdCornerSphere;
                    }
                    else if (hit.collider == forthCornerSphere.GetComponent<Collider>())
                    {
                        draggedSphere = forthCornerSphere;
                    }
                    // Sides
                    else if (hit.collider == firstSideSphere.GetComponent<Collider>())
                    {
                        draggedSphere = firstSideSphere;
                    }
                    else if (hit.collider == secondSideSphere.GetComponent<Collider>())
                    {
                        draggedSphere = secondSideSphere;
                    }
                    else if (hit.collider == thirdSideSphere.GetComponent<Collider>())
                    {
                        draggedSphere = thirdSideSphere;
                    }
                    else if (hit.collider == forthSideSphere.GetComponent<Collider>())
                    {
                        draggedSphere = forthSideSphere;
                    }
                    //End Scaling
                    else if (hit.collider == endWithSave.GetComponent<Collider>())
                    {
                        EndScale(true);
                        // scaling is finalized
                        result = true;
                    }
                    else if (hit.collider == endWithOutSave.GetComponent<Collider>())
                    {
                        EndScale(false);
                    }
                }

                if (draggedSphere == topSphere)
                {
                    GameNodeMover.MoveToLockAxes(draggedSphere, false, true, false);
                }
                else if (draggedSphere == firstCornerSphere || draggedSphere == secondCornerSphere
                         || draggedSphere == thirdCornerSphere || draggedSphere == forthCornerSphere)
                {
                    GameNodeMover.MoveToLockAxes(draggedSphere, true, false, true);
                }
                else if (draggedSphere == firstSideSphere || draggedSphere == secondSideSphere)
                {
                    GameNodeMover.MoveToLockAxes(draggedSphere, true, false, false);
                }
                else if (draggedSphere == thirdSideSphere || draggedSphere == forthSideSphere)
                {
                    GameNodeMover.MoveToLockAxes(draggedSphere, false, false, true);
                }
                else
                {
                    draggedSphere = null;
                }

                if (objectToScale != null)
                {
                    ScaleNode();
                    SetOnRoof();
                    SetOnSide();
                }
            }
            else
            {
                if (objectToScale != null && scalingGizmosAreDrawn)
                {
                    draggedSphere = null;
                    // Adjust the size of the scaling elements
                    SphereRadius(topSphere);
                    SphereRadius(firstSideSphere);
                    SphereRadius(secondSideSphere);
                    SphereRadius(thirdSideSphere);
                    SphereRadius(forthSideSphere);
                    SphereRadius(firstCornerSphere);
                    SphereRadius(secondCornerSphere);
                    SphereRadius(thirdCornerSphere);
                    SphereRadius(forthCornerSphere);

                    SphereRadius(endWithOutSave);
                    SphereRadius(endWithSave);
                }
            }
            return result;
        }

        /// <summary>
        /// Sets the new scale of a node based on the sphere elements.
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
            scaleCopy = scale;
            //  scaledObjectTransform = objectToScale.transform;
            hadAnEffect = true;
            new ScaleNodeNetAction(objectToScale.name, scale, position).Execute();
        }

        /// <summary>
        /// Sets the top sphere at the top of <see cref="objectToScale"/> and
        /// the Save (<see cref="endWithSave"/>) and Discard (<see cref="endWithOutSave"/>)
        /// gizmos.
        /// </summary>
        private void SetOnRoof()
        {
            Vector3 pos = objectToScale.transform.position;
            // The scaling sphere is just above the center of the roof of objectToScale.
            pos.y = objectToScale.GetRoof() + ScalingSphereRadius();
            topSphere.transform.position = pos;
            topOldSpherePos = topSphere.transform.position;

            // The two gizmos to confirm or cancel the scaling are just above the 
            // roof of objectToScale. We are assuming endWithSave and endWithOutSave
            // have the same height.
            pos.y = objectToScale.GetRoof() + endWithSave.WorldSpaceScale().y / 2;
            // The two gizmos are left and right, respectively, from it. We want
            // them in the middle between respective objectToScale's edge and the
            // centered scaling sphere (which is at the center of objectToScale's
            // roof). We need to divide objectToScale.transform.lossyScale by 2
            // to obtain the extent, then once more divide by 2 to obtain half
            // that distance.
            float distance = objectToScale.transform.lossyScale.x / 4;
            pos.x += distance;
            endWithSave.transform.position = pos;
            pos.x -= 2 * distance; // multiplied by two to revert the above setting of pos.x
            endWithOutSave.transform.position = pos;
        }

        /// <summary>
        /// Returns the radius of the sphere used to visualize the
        /// handle (gizmo) to scale the object.
        /// </summary>
        /// <returns></returns>
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
        /// This will end the scaling action the user can choose between save and discard.
        /// </summary>
        /// <param name="save">Whether the changes should be saved</param>
        public void EndScale(bool save)
        {
            if (save)
            {
                // FIXME: Currently, the changes will not be saved after closing the game. 
                // SAVE THE CHANGES
                RemoveSpheres();
            }
            else
            {
                objectToScale.SetScale(originalScale);
                objectToScale.transform.position = originalPosition;
                new ScaleNodeNetAction(objectToScale.name, originalScale, originalPosition).Execute();
                hadAnEffect = true;
                RemoveSpheres();
            }
        }

        /// <summary>
        /// Resets all attributes from the gameObject.
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
            Destroyer.DestroyGameObject(endWithSave);
            Destroyer.DestroyGameObject(endWithOutSave);
            objectToScale = null;
            scalingGizmosAreDrawn = false;
        }

        /// <summary>
        /// Undoes this ScaleNodeAction
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            Destroyer.DestroyGameObject(objectToScale);
            temporaryCopy.transform.position = originalPosition;
            temporaryCopy.SetScale(originalScale);
        }

        /// <summary>
        /// Redoes this ScaleNodeAction
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            temporaryCopy.SetScale(scaleCopy);
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
    }
}
