using SEE.DataModel;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{

    public class NavigationAction : MonoBehaviour
    {
        private enum NavigationMode
        {
            Move = 0,
            Rotate
        }

        internal class ZoomCommand
        {
            public readonly int TargetZoomSteps;
            public readonly Vector2 ZoomCenter;
            private readonly float duration;
            private readonly float startTime;

            internal ZoomCommand(Vector2 zoomCenter, int targetZoomSteps, float duration)
            {
                TargetZoomSteps = targetZoomSteps;
                ZoomCenter = zoomCenter;
                this.duration = duration;
                startTime = Time.realtimeSinceStartup;
            }

            internal bool IsFinished()
            {
                bool result = Time.realtimeSinceStartup - startTime >= duration;
                return result;
            }

            internal float CurrentDeltaScale()
            {
                float x = Mathf.Min((Time.realtimeSinceStartup - startTime) / duration, 1.0f);
                float t = 0.5f - 0.5f * Mathf.Cos(x * Mathf.PI);
                float result = t * (float)TargetZoomSteps;
                return result;
            }
        }

        private struct GrabState
        {
            internal Transform transform;        // The transform of the grabbed object or null, if no object is currently grabbed
            internal Vector3 startPlaneHitPoint; // The point, where the screen raycast hit the table when the grabbing of the object began
            internal Vector3 offset;             // The offset between 'startPlaneHitPoint' and 'transform'
        }

        private struct MoveState
        {
            internal const float MaxVelocity = 10.0f;
            internal const float MaxSqrVelocity = MaxVelocity * MaxVelocity;
            internal const float SnapStepCount = 8;
            internal const float SnapStepAngle = 360.0f / SnapStepCount;
            internal const float DragFrictionFactor = 32.0f;

            internal UI3D.MoveGizmo moveGizmo;
            internal Bounds cityBounds;
            internal Vector3 dragStartTransformPosition;
            internal Vector3 dragStartOffset;
            internal Vector3 dragCanonicalOffset;
            internal Vector3 moveVelocity;
        }

        private struct RotateState
        {
            internal const float SnapStepCount = 8;
            internal const float SnapStepAngle = 360.0f / SnapStepCount;

            internal UI3D.RotateGizmo rotateGizmo;
            internal float originalEulerAngleY;
            internal Vector3 originalPosition;
            internal float startAngle;
        }

        private struct ZoomState
        {
            internal const float DefaultZoomDuration = 0.1f;
            internal const uint ZoomMaxSteps = 32;
            internal const float ZoomFactor = 0.5f;

            internal Vector3 originalScale;
            internal List<ZoomCommand> zoomCommands;
            internal uint currentTargetZoomSteps;
            internal float currentZoomFactor;
        }

        private struct CameraState
        {
            internal float distance;
            internal float yaw;
            internal float pitch;
        }

        private struct ActionState
        {
            internal bool toggleGrab;        // grab element of city
            internal bool startDrag;         // drag entire city
            internal bool drag;              // drag entire city
            internal bool cancel;
            internal bool snap;
            internal bool reset;
            internal Vector3 mousePosition;  // TODO(torben): this needs to be abstracted for other modalities
            internal float zoomStepsDelta;
            internal bool zoomToggleToObject;
        }

        private const int RightMouseButton = 1;
        
        private Transform cityTransform;
        private Plane raycastPlane;

        private NavigationMode mode;
        private bool movingOrRotating;
        private UI3D.Cursor cursor;

        GrabState grabState;
        MoveState moveState;
        RotateState rotateState;
        ZoomState zoomState;
        CameraState cameraState;
        ActionState actionState;

        private const string CityObjectName = "Implementation";

        private static Transform GetCity()
        {
            // FIXME: We must not rely on this particular name for the game object.

            GameObject seeCity = GameObject.Find(CityObjectName);
            if (seeCity == null)
            {
                Debug.LogErrorFormat("There is no game object named {0}.\n", CityObjectName);
                return null;
            }
            else
            {
                foreach (Transform child in seeCity.transform)
                {
                    if (child.tag == Tags.Node)
                    {
                        return child.transform;
                    }
                }
                Debug.LogErrorFormat("Game object named {0} has no child tagged by {1}.\n", CityObjectName, Tags.Node);
                return null;
            }
        }

        private void Start()
        {
            cityTransform = GetCity();
            Debug.LogFormat("City object is {0}\n", cityTransform.name);

            zoomState.originalScale = cityTransform.localScale;
            moveState.cityBounds = cityTransform.GetComponent<MeshCollider>().bounds;
            raycastPlane = new Plane(Vector3.up, cityTransform.position);
            
            moveState.dragStartTransformPosition = cityTransform.position;
            moveState.dragCanonicalOffset = Vector3.zero;
            moveState.moveVelocity = Vector3.zero;
            moveState.moveGizmo = UI3D.MoveGizmo.Create(0.008f * Table.MinDimXZ);
            rotateState.rotateGizmo = UI3D.RotateGizmo.Create(1024);

            zoomState.zoomCommands = new List<ZoomCommand>((int)ZoomState.ZoomMaxSteps);
            zoomState.currentTargetZoomSteps = 0;
            zoomState.currentZoomFactor = 1.0f;

            cursor = UI3D.Cursor.Create();

            Camera.main.transform.position = Table.TableTopCenterEpsilon;
            cameraState.distance = 1.0f;
            cameraState.yaw = 0.0f;
            cameraState.pitch = 30.0f;
            Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            Camera.main.transform.position -= Camera.main.transform.forward * cameraState.distance;
        }

        private void Update()
        {
            // Note: Input MUST NOT be inquired in FixedUpdate() for the input to feel responsive!

            actionState.toggleGrab |= Input.GetKeyDown(KeyCode.G);
            actionState.startDrag |= Input.GetMouseButtonDown(2);
            actionState.drag = Input.GetMouseButton(2);
            actionState.cancel |= Input.GetKeyDown(KeyCode.Escape);
            actionState.snap = Input.GetKey(KeyCode.LeftAlt);
            actionState.reset |= Input.GetKeyDown(KeyCode.R);
            actionState.mousePosition = Input.mousePosition;
            if (!actionState.drag)
            {
                actionState.zoomStepsDelta += Input.mouseScrollDelta.y;
                actionState.zoomToggleToObject |= Input.GetKeyDown(KeyCode.F);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                mode = NavigationMode.Move;
                movingOrRotating = false;
                rotateState.rotateGizmo.gameObject.SetActive(false);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                mode = NavigationMode.Rotate;
                movingOrRotating = false;
                moveState.moveGizmo.gameObject.SetActive(false);
            }

            rotateState.rotateGizmo.Radius = 0.2f * (Camera.main.transform.position - rotateState.rotateGizmo.Center).magnitude;

            if (!actionState.drag && Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray);
                Array.Sort(hits, (h0, h1) => h0.distance.CompareTo(h1.distance));
                bool selected = false;
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform.gameObject.GetComponent<NodeRef>() != null)
                    {
                        Select(hit.transform.gameObject);
                        selected = true;
                        break;
                    }
                }
                if (!selected)
                {
                    Select(null);
                }
            }
            if (cursor.GetFocus())
            {
                rotateState.rotateGizmo.Center = cursor.GetFocus().position;
            }

            // Camera
            const float Speed = 2.0f; // TODO(torben): this is arbitrary
            float speed = Input.GetKey(KeyCode.LeftShift) ? 4.0f * Speed : Speed;
            if (Input.GetKey(KeyCode.W))
            {
                cameraState.distance -= speed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                cameraState.distance += speed * Time.deltaTime;
            }
            if (Input.GetMouseButton(RightMouseButton))
            {
                float x = Input.GetAxis("mouse x");
                float y = Input.GetAxis("mouse y");
                cameraState.yaw += x;
                cameraState.pitch -= y;
            }
            Camera.main.transform.position = Table.TableTopCenterEpsilon;
            Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            Camera.main.transform.position -= Camera.main.transform.forward * cameraState.distance;
        }

        // This logic is in FixedUpdate(), so that the behaviour is framerate-
        // 'independent'.
        private void FixedUpdate()
        {
            // TODO(torben): abstract mouse away!

            Ray ray = Camera.main.ScreenPointToRay(actionState.mousePosition);
            bool raycastResult = raycastPlane.Raycast(ray, out float enter);
            Vector3 planeHitPoint = ray.GetPoint(enter);

            if (actionState.cancel && !movingOrRotating)
            {
                Select(null);
            }

            #region Grab Part Of City

            if (actionState.toggleGrab)
            {
                actionState.toggleGrab = false;

                if (grabState.transform == null)
                {
                    RaycastHit[] raycastHits = Physics.RaycastAll(ray);
                    int compareRaycastHits(RaycastHit h0, RaycastHit h1)
                    {
                        return h0.distance.CompareTo(h1.distance);
                    };
                    Array.Sort(raycastHits, compareRaycastHits);
                    NodeRef nodeRef = null;
                    foreach (RaycastHit raycastHit in raycastHits)
                    {
                        nodeRef = raycastHit.transform.GetComponent<NodeRef>();
                        if (nodeRef != null)
                        {
#if UNITY_EDITOR
                            if (nodeRef.node != null)
                            {
                                Debug.LogWarning("The node-ref of the grabbed object is not set!");
                            }
#endif

                            CollisionEventHandler collisionEventHandler = raycastHit.transform.gameObject.AddComponent<CollisionEventHandler>();
                            collisionEventHandler.onCollisionEnter += OnGrabbedObjectCollisionEnter;
                            collisionEventHandler.onCollisionExit += OnGrabbedObjectCollisionExit;
                            collisionEventHandler.onCollisionStay += OnGrabbedObjectCollisionStay;
                            collisionEventHandler.onTriggerEnter += OnGrabbedObjectTriggerEnter;
                            collisionEventHandler.onTriggerExit += OnGrabbedObjectTriggerExit;
                            collisionEventHandler.onTriggerStay += OnGrabbedObjectTriggerStay;

                            grabState.transform = raycastHit.transform;
                            grabState.startPlaneHitPoint = planeHitPoint;
                            grabState.offset = raycastHit.transform.position - planeHitPoint;
                            break;
                        }
                    }
                    actionState.toggleGrab = false;
                    OnStartGrabbing(grabState.transform);
                }
                else
                {
                    Transform tf = grabState.transform;
                    Destroy(grabState.transform.GetComponent<CollisionEventHandler>());
                    grabState.transform = null;
                    OnFinalizeGrabbing(tf);
                }
            }
            else if (grabState.transform != null)
            {
                if (actionState.cancel)
                {
                    actionState.cancel = false;
                    grabState.transform.position = grabState.startPlaneHitPoint + grabState.offset;
                    Destroy(grabState.transform.GetComponent<CollisionEventHandler>());
                    Transform tf = grabState.transform;
                    grabState.transform = null;
                    OnCancelGrabbing(tf);
                }
                else
                {
                    grabState.transform.position = planeHitPoint + grabState.offset;
                }
            }

            #endregion

            #region Move City

            else if (mode == NavigationMode.Move)
            {
                if (actionState.reset) // reset to center of table
                {
                    actionState.reset = false;
                    movingOrRotating = false;
                    moveState.moveGizmo.gameObject.SetActive(false);

                    cityTransform.position = Table.TableTopCenterEpsilon;
                }
                else if (actionState.cancel) // cancel movement
                {
                    actionState.cancel = false;
                    if (movingOrRotating)
                    {
                        movingOrRotating = false;
                        moveState.moveGizmo.gameObject.SetActive(false);

                        moveState.moveVelocity = Vector3.zero;
                        cityTransform.position = moveState.dragStartTransformPosition + moveState.dragStartOffset - Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale);
                    }
                }
                else if (actionState.drag) // start or continue movement
                {
                    if (raycastResult)
                    {
                        if (actionState.startDrag) // start movement
                        {
                            actionState.startDrag = false;
                            movingOrRotating = true;
                            moveState.moveGizmo.gameObject.SetActive(true);

                            moveState.dragStartTransformPosition = cityTransform.position;
                            moveState.dragStartOffset = planeHitPoint - cityTransform.position;
                            moveState.dragCanonicalOffset = moveState.dragStartOffset.DividePairwise(cityTransform.localScale);
                            moveState.moveVelocity = Vector3.zero;
                        }
                        if (movingOrRotating) // continue movement
                        {
                            Vector3 totalDragOffsetFromStart = planeHitPoint - (moveState.dragStartTransformPosition + moveState.dragStartOffset);

                            if (actionState.snap)
                            {
                                totalDragOffsetFromStart = Project(totalDragOffsetFromStart);
                            }

                            Vector3 oldPosition = cityTransform.position;
                            Vector3 newPosition = moveState.dragStartTransformPosition + totalDragOffsetFromStart;

                            moveState.moveVelocity = (newPosition - oldPosition) / Time.fixedDeltaTime;
                            cityTransform.position = newPosition;
                            moveState.moveGizmo.SetPositions(moveState.dragStartTransformPosition + moveState.dragStartOffset, cityTransform.position + Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale));
                        }
                    }
                }
                else if (movingOrRotating) // finalize movement
                {
                    movingOrRotating = false;
                    moveState.moveGizmo.gameObject.SetActive(false);
                }
            }

            #endregion

            #region Rotate

            else // mode == NavigationMode.Rotate
            {
                if (cursor.GetFocus())
                {
                    if (actionState.reset) // reset rotation to identity();
                    {
                        actionState.reset = false;
                        movingOrRotating = false;
                        rotateState.rotateGizmo.gameObject.SetActive(false);

                        cityTransform.RotateAround(rotateState.rotateGizmo.Center, Vector3.up, -cityTransform.rotation.eulerAngles.y);
                    }
                    else if (actionState.cancel) // cancel rotation
                    {
                        actionState.cancel = false;
                        if (movingOrRotating)
                        {
                            movingOrRotating = false;
                            rotateState.rotateGizmo.gameObject.SetActive(false);

                            cityTransform.rotation = Quaternion.Euler(0.0f, rotateState.originalEulerAngleY, 0.0f);
                            cityTransform.position = rotateState.originalPosition;
                        }
                    }
                    else if (actionState.drag) // start or continue rotation
                    {
                        if (raycastResult)
                        {
                            Vector2 toHit = new Vector2(planeHitPoint.x - rotateState.rotateGizmo.Center.x, planeHitPoint.z - rotateState.rotateGizmo.Center.z);
                            float toHitAngle = toHit.Angle360();

                            if (actionState.startDrag) // start rotation
                            {
                                actionState.startDrag = false;
                                movingOrRotating = true;
                                rotateState.rotateGizmo.gameObject.SetActive(true);

                                rotateState.originalEulerAngleY = cityTransform.rotation.eulerAngles.y;
                                rotateState.originalPosition = cityTransform.position;
                                rotateState.startAngle = AngleMod(cityTransform.rotation.eulerAngles.y - toHitAngle);
                                rotateState.rotateGizmo.SetMinAngle(Mathf.Deg2Rad * toHitAngle);
                                rotateState.rotateGizmo.SetMaxAngle(Mathf.Deg2Rad * toHitAngle);
                            }

                            if (movingOrRotating) // continue rotation
                            {
                                float angle = AngleMod(rotateState.startAngle + toHitAngle);
                                if (actionState.snap)
                                {
                                    angle = AngleMod(Mathf.Round(angle / RotateState.SnapStepAngle) * RotateState.SnapStepAngle);
                                }
                                cityTransform.RotateAround(cursor.GetFocus().position, Vector3.up, angle - cityTransform.rotation.eulerAngles.y);

                                float prevAngle = Mathf.Rad2Deg * rotateState.rotateGizmo.GetMaxAngle();
                                float currAngle = toHitAngle;

                                while (Mathf.Abs(currAngle + 360.0f - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                                {
                                    currAngle += 360.0f;
                                }
                                while (Mathf.Abs(currAngle - 360.0f - prevAngle) < Mathf.Abs(currAngle - prevAngle))
                                {
                                    currAngle -= 360.0f;
                                }
                                if (actionState.snap)
                                {
                                    currAngle = Mathf.Round((currAngle + rotateState.startAngle) / (RotateState.SnapStepAngle)) * (RotateState.SnapStepAngle) - rotateState.startAngle;
                                }

                                rotateState.rotateGizmo.SetMaxAngle(Mathf.Deg2Rad * currAngle);
                            }
                        }
                    }
                    else if (movingOrRotating) // finalize rotation
                    {
                        movingOrRotating = false;
                        rotateState.rotateGizmo.gameObject.SetActive(false);
                    }
                }
            }

            #endregion

            #region Zoom

            if (actionState.zoomToggleToObject)
            {
                actionState.zoomToggleToObject = false;
                if (cursor.GetFocus() != null)
                {
                    float optimalTargetZoomFactor = Table.MinDimXZ / (cursor.GetFocus().lossyScale.x / zoomState.currentZoomFactor);
                    float optimalTargetZoomSteps = ConvertZoomFactorToZoomSteps(optimalTargetZoomFactor);
                    int actualTargetZoomSteps = Mathf.FloorToInt(optimalTargetZoomSteps);
                    float actualTargetZoomFactor = ConvertZoomStepsToZoomFactor(actualTargetZoomSteps);

                    int zoomSteps = actualTargetZoomSteps - (int)zoomState.currentTargetZoomSteps;
                    if (zoomSteps == 0)
                    {
                        zoomSteps = -(int)zoomState.currentTargetZoomSteps;
                        actualTargetZoomFactor = 1.0f;
                    }

                    if (zoomSteps != 0)
                    {
                        float zoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                        Vector2 centerOfTableAfterZoom = zoomSteps == -(int)zoomState.currentTargetZoomSteps ? cityTransform.position.XZ() : cursor.GetFocus().position.XZ();
                        Vector2 toCenterOfTable = Table.CenterXZ - centerOfTableAfterZoom;
                        Vector2 zoomCenter = Table.CenterXZ - (toCenterOfTable * (zoomFactor / (zoomFactor - 1.0f)));
                        float duration = 2.0f * ZoomState.DefaultZoomDuration;
                        new Net.ZoomCommandAction(zoomCenter, zoomSteps, duration).Execute();
                    }
                }
            }

            if (Mathf.Abs(actionState.zoomStepsDelta) >= 1.0f)
            {
                float fZoomSteps = actionState.zoomStepsDelta >= 0.0f ? Mathf.Floor(actionState.zoomStepsDelta) : Mathf.Ceil(actionState.zoomStepsDelta);
                actionState.zoomStepsDelta -= fZoomSteps;
                int zoomSteps = Mathf.Clamp((int)fZoomSteps, -(int)zoomState.currentTargetZoomSteps, (int)ZoomState.ZoomMaxSteps - (int)zoomState.currentTargetZoomSteps);
                new Net.ZoomCommandAction(planeHitPoint.XZ(), zoomSteps, ZoomState.DefaultZoomDuration).Execute();
            }

            if (zoomState.zoomCommands.Count != 0)
            {
                float zoomSteps = (float)zoomState.currentTargetZoomSteps;
                int positionCount = 0;
                Vector2 positionSum = Vector3.zero;

                for (int i = 0; i < zoomState.zoomCommands.Count; i++)
                {
                    positionCount++;
                    positionSum += zoomState.zoomCommands[i].ZoomCenter;
                    if (zoomState.zoomCommands[i].IsFinished())
                    {
                        zoomState.zoomCommands.RemoveAt(i--);
                    }
                    else
                    {
                        zoomSteps -= zoomState.zoomCommands[i].TargetZoomSteps - zoomState.zoomCommands[i].CurrentDeltaScale();
                    }
                }
                Vector3 averagePosition = new Vector3(positionSum.x / positionCount, cityTransform.position.y, positionSum.y / positionCount);

                zoomState.currentZoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                Vector3 cityCenterToHitPoint = averagePosition - cityTransform.position;
                Vector3 cityCenterToHitPointUnscaled = cityCenterToHitPoint.DividePairwise(cityTransform.localScale);

                cityTransform.position += cityCenterToHitPoint;
                cityTransform.localScale = zoomState.currentZoomFactor * zoomState.originalScale;
                cityTransform.position -= Vector3.Scale(cityCenterToHitPointUnscaled, cityTransform.localScale);

                moveState.dragStartTransformPosition += moveState.dragStartOffset;
                moveState.dragStartOffset = Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale);
                moveState.dragStartTransformPosition -= moveState.dragStartOffset;
            }

            #endregion

            #region ApplyVelocityAndConstraints

            if (!movingOrRotating)
            {
                // Clamp velocity
                float sqrMag = moveState.moveVelocity.sqrMagnitude;
                if (sqrMag > MoveState.MaxSqrVelocity)
                {
                    moveState.moveVelocity = moveState.moveVelocity / Mathf.Sqrt(sqrMag) * MoveState.MaxVelocity;
                }

                // Apply velocity to city position
                cityTransform.position += moveState.moveVelocity * Time.fixedDeltaTime;

                // Apply friction to velocity
                Vector3 acceleration = MoveState.DragFrictionFactor * -moveState.moveVelocity;
                moveState.moveVelocity += acceleration * Time.fixedDeltaTime;
            }

            // Keep city constrained to table
            float radius = 0.5f * cityTransform.lossyScale.x;
            MathExtensions.TestCircleAABB(cityTransform.position.XZ(), 0.9f * radius, Table.MinXZ, Table.MaxXZ, out float distance, out Vector2 normalizedToSurfaceDirection);

            if (distance > 0.0f)
            {
                Vector2 toSurfaceDirection = distance * normalizedToSurfaceDirection;
                cityTransform.position += new Vector3(toSurfaceDirection.x, 0.0f, toSurfaceDirection.y);
            }

            #endregion
        }

        private float ConvertZoomStepsToZoomFactor(float zoomSteps)
        {
            float result = Mathf.Pow(2, zoomSteps * ZoomState.ZoomFactor);
            return result;
        }

        private float ConvertZoomFactorToZoomSteps(float zoomFactor)
        {
            float result = Mathf.Log(zoomFactor, 2) / ZoomState.ZoomFactor;
            return result;
        }

        internal void PushZoomCommand(Vector2 zoomCenter, int zoomSteps, float duration)
        {
            zoomSteps = Mathf.Clamp(zoomSteps, -(int)zoomState.currentTargetZoomSteps, (int)ZoomState.ZoomMaxSteps - (int)zoomState.currentTargetZoomSteps);
            if (zoomSteps != 0)
            {
                uint newZoomStepsInProgress = (uint)((int)zoomState.currentTargetZoomSteps + zoomSteps);
                if (duration != 0)
                {
                    zoomState.zoomCommands.Add(new ZoomCommand(zoomCenter, zoomSteps, duration));
                }
                zoomState.currentTargetZoomSteps = newZoomStepsInProgress;
            }
        }

        private float AngleMod(float degrees)
        {
            float result = ((degrees % 360.0f) + 360.0f) % 360.0f;
            return result;
        }

        private Vector3 Project(Vector3 offset)
        {
            Vector2 point2 = new Vector2(offset.x, offset.z);
            float angleDeg = point2.Angle360();
            float snappedAngleDeg = Mathf.Round(angleDeg / MoveState.SnapStepAngle) * MoveState.SnapStepAngle;
            float snappedAngleRad = Mathf.Deg2Rad * snappedAngleDeg;
            Vector2 dir = new Vector2(Mathf.Cos(snappedAngleRad), Mathf.Sin(-snappedAngleRad));
            Vector2 proj = dir * Vector2.Dot(point2, dir);
            Vector3 result = new Vector3(proj.x, offset.y, proj.y);
            return result;
        }

        private void Select(GameObject go)
        {
            Transform oldT = cursor.GetFocus();
            Transform newT = go ? go.transform : null;
            bool updateServer = false;

            if (oldT != newT)
            {
                if (oldT)
                {
                    Outline outline = oldT.GetComponent<Outline>();
                    if (outline)
                    {
                        updateServer = true;
                        Destroy(outline);
                        cursor.SetFocus(null);
                    }
                }
                if (newT)
                {
                    if (newT.GetComponent<Outline>() == null)
                    {
                        updateServer = true;
                        Outline.Create(newT.gameObject);
                        cursor.SetFocus(newT);
                    }
                }
            }

            if (updateServer)
            {
                HoverableObject oldH = oldT ? oldT.GetComponent<HoverableObject>() : null;
                HoverableObject newH = newT ? newT.GetComponent<HoverableObject>() : null;
                new Net.SelectionAction(oldH, newH).Execute();
            }
        }

        /// <summary>
        /// Is called, if an element of the city is grabbed.
        /// </summary>
        /// <param name="grabbedTransform">The transform of the grabbed object.</param>
        private void OnStartGrabbing(Transform grabbedTransform)
        {
            Debug.Log("Start grabbing '" + grabbedTransform.name + "'!");
        }

        /// <summary>
        /// Is called, if the grabbed element of the city is released.
        /// </summary>
        /// <param name="grabbedTransform">The transform of the released object.</param>
        private void OnFinalizeGrabbing(Transform grabbedTransform)
        {
            Debug.Log("Finalized grabbing '" + grabbedTransform.name + "'!");
        }

        /// <summary>
        /// Is called, if grabbing of an object is cancelled.
        /// </summary>
        /// <param name="grabbedTransform">The transform of the cancelled object.</param>
        private void OnCancelGrabbing(Transform grabbedTransform)
        {
            Debug.Log("Cancelled grabbing '" + grabbedTransform.name + "'!");
        }

        /// <summary>
        /// Is called, if the grabbed object enters collision with an object.
        /// </summary>
        /// <param name="collider">The collider of the grabbed object.</param>
        /// <param name="collision">The collision.</param>
        private void OnGrabbedObjectCollisionEnter(CollisionEventHandler collider, Collision collision)
        {
            Debug.Log("'" + collider.name + "' entered the collider of '" + collision.gameObject.name + "'!");
        }

        /// <summary>
        /// Is called, if the grabbed object exits collision with an object.
        /// </summary>
        /// <param name="collider">The collider of the grabbed object.</param>
        /// <param name="collision">The collision.</param>
        private void OnGrabbedObjectCollisionExit(CollisionEventHandler collider, Collision collision)
        {
            Debug.Log("'" + collider.name + "' exited the collider of '" + collision.gameObject.name + "'!");
        }

        /// <summary>
        /// Is called, if the grabbed object stays in collision with an object.
        /// </summary>
        /// <param name="collider">The collider of the grabbed object.</param>
        /// <param name="collision">The collision.</param>
        private void OnGrabbedObjectCollisionStay(CollisionEventHandler collider, Collision collision)
        {
            Debug.Log("'" + collider.name + "' stayed in the collider of '" + collision.gameObject.name + "'!");
        }

        /// <summary>
        /// Is called, if the grabbed object enters a trigger of an object.
        /// </summary>
        /// <param name="collider">The collider of the grabbed object.</param>
        /// <param name="collision">The other collider.</param>
        private void OnGrabbedObjectTriggerEnter(CollisionEventHandler collider, Collider other)
        {
            Debug.Log("'" + collider.name + "' entered the trigger of '" + other.name + "'!");
        }

        /// <summary>
        /// Is called, if the grabbed object exits a trigger of an object.
        /// </summary>
        /// <param name="collider">The collider of the grabbed object.</param>
        /// <param name="collision">The other collider.</param>
        private void OnGrabbedObjectTriggerExit(CollisionEventHandler collider, Collider other)
        {
            Debug.Log("'" + collider.name + "' exited the trigger of '" + other.name + "'!");
        }

        /// <summary>
        /// Is called, if the grabbed object stays in a trigger of an object.
        /// </summary>
        /// <param name="collider">The collider of the grabbed object.</param>
        /// <param name="collider">The other collider.</param>
        private void OnGrabbedObjectTriggerStay(CollisionEventHandler collider, Collider other)
        {
            Debug.Log("'" + collider.name + "' stayed in trigger of '" + other.name + "'!");
        }

    }
}
