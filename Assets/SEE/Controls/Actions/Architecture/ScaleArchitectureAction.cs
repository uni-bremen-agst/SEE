using SEE.Controls.Architecture;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SEE.Controls.Actions.Architecture
{
    
    /// <summary>
    /// Implementation of <see cref="AbstractArchitectureAction"/>.
    /// Allows the scaling of node elements.
    /// The implementation is identical to <see cref="ScaleNodeAction"/>,
    /// but relies on the <see cref="PenInteraction"/> component.
    /// </summary>
    public class ScaleArchitectureAction : AbstractArchitectureAction
    {
        public override ArchitectureActionType GetActionType()
        {
            return ArchitectureActionType.Scale;
        }

        /// <summary>
        /// The struct that contains the anchor gizmos and their points.
        /// </summary>
        private struct Gizmo
        {
            internal GameObject topAnchor;
            internal Vector3 topOld;
            internal GameObject topRightAnchor;
            internal Vector3 topRightOld;
            internal GameObject rightAnchor;
            internal Vector3 rightOld;
            internal GameObject bottomRightAnchor;
            internal Vector3 bottomRightOld;
            internal GameObject bottomAnchor;
            internal Vector3 bottomOld;
            internal GameObject bottomLeftAnchor;
            internal Vector3 bottomLeftOld;
            internal GameObject leftAnchor;
            internal Vector3 leftOld;
            internal GameObject topLeftAnchor;
            internal Vector3 topLeftOld;
        }
        
        /// <summary>
        /// The gizmo instance.
        /// </summary>
        private Gizmo _gizmo;
        
        /// <summary>
        /// The size of the scaling spheres will be relative to the game object to be scaled.
        /// This factor determines that scale. It will be multiplied by the x or z scale of
        /// <see cref="objectToScale"/> (the smaller of the two). If that value is shorter
        /// than <see cref="minimalSphereScale"/>, <see cref="minimalSphereScale"/> will
        /// be used instead.
        /// </summary>
        private const float relativeSphereScale = 0.1f;
        
        /// <summary>
        /// The minimal scale a scaling sphere may have in world space.
        /// </summary>
        private const float minimalSphereScale = 0.01f;

        /// <summary>
        /// The gameObject that is currently selected and should be scaled.
        /// Will be null if no object has been selected yet.
        /// </summary>
        private GameObject target;
        /// <summary>
        /// True if the gizmos that allow a user to scale the object in all three dimensions
        /// are drawn.
        /// </summary>
        private bool gizmoDrawn;
        
        /// <summary>
        /// The scaling gizmo selected by the user to scale <see cref="objectToScale"/>.
        /// Will be null if none was selected yet.
        /// </summary>
        private GameObject draggedAnchor;

        
        public static AbstractArchitectureAction NewInstance()
        {
            return new ScaleArchitectureAction();
        }
        
        public override void Start()
        {
            PenInteractionController.ObjectPrimaryClicked += OnObjectSelected;
            gizmoDrawn = false;
            _gizmo = new Gizmo();
        }

        public override void Stop()
        {
            PenInteractionController.ObjectPrimaryClicked -= OnObjectSelected;
            RemoveSpheres();
        }
        
        private void OnObjectSelected(ObjectPrimaryClicked data)
        {
            
            if (target != data.Object && data.Object.HasNodeRef())
            {
                RemoveSpheres();
                target = data.Object;
            }
            
        }


        public override void Update()
        {
            if (Raycasting.IsMouseOverGUI())
            {
                return;
            }
            if (target != null)
            {
                if (!gizmoDrawn)
                {
                    DrawGizmo();
                }

                if (Pen.current.pressure.ReadValue() > 0f && Pen.current.firstBarrelButton.isPressed)
                {
                    if (draggedAnchor == null && Raycasting.RaycastAnything(out RaycastHit raycastHit))
                    {
                        draggedAnchor = SelectedGizmo(raycastHit.collider.gameObject);
                    }

                    if (draggedAnchor != null)
                    {
                        Scaling();
                    }
                }
            }

            if (Pen.current.tip.wasPressedThisFrame)
            {
                HitGraphElement element =
                    Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef elementRef);
                if (target != null)
                {
                    if (target != raycastHit.collider.gameObject)
                    {
                        GameObject selectedAnchor = SelectedGizmo(raycastHit.collider.gameObject);
                        if (selectedAnchor != null)
                        {
                            draggedAnchor = selectedAnchor;
                            return;
                        }
                        else
                        {
                            if (element == HitGraphElement.Node)
                            {
                                if (!((NodeRef) elementRef).Value.IsRoot())
                                {
                                    target = raycastHit.collider.gameObject;
                                }
                                else
                                {
                                    target = null;
                                }
                                
                            }
                            else
                            {
                                target = null;
                            }

                            RemoveSpheres();
                            draggedAnchor = null;
                            _gizmo = new Gizmo();
                            return;
                        }
                    }
                }
            }
            
            
        }
        private void RemoveSpheres()
        {
            Destroyer.DestroyGameObject(_gizmo.topAnchor);
            Destroyer.DestroyGameObject(_gizmo.topRightAnchor);
            Destroyer.DestroyGameObject(_gizmo.rightAnchor);
            Destroyer.DestroyGameObject(_gizmo.bottomRightAnchor);
            Destroyer.DestroyGameObject(_gizmo.bottomAnchor);
            Destroyer.DestroyGameObject(_gizmo.bottomLeftAnchor);
            Destroyer.DestroyGameObject(_gizmo.leftAnchor);
            Destroyer.DestroyGameObject(_gizmo.topLeftAnchor);
            gizmoDrawn = false;
        }
            
        private GameObject SelectedGizmo(GameObject gam)
        {
            if (!gizmoDrawn) return null;
            if (gam == _gizmo.topAnchor || gam == _gizmo.topRightAnchor || gam == _gizmo.rightAnchor ||
                gam == _gizmo.bottomRightAnchor || gam == _gizmo.bottomAnchor || gam == _gizmo.bottomLeftAnchor ||
                gam == _gizmo.leftAnchor || gam == _gizmo.topLeftAnchor) return gam;
            return null;
        }
        
        /// <summary>
        /// Scales <see cref="objectToScale"/> and drags and re-draws the scaling gizmos. 
        /// </summary>
        private void Scaling()
        {
            DragAnchor(draggedAnchor);
            ScaleTarget();
            SetOnSide();
            AdjustSizeOfScalingAnchors();

        }
        
        /// <summary>
        /// Adjusts the size of the scaling elements according to the size of <see cref="objectToScale"/>.
        /// </summary>
        private void AdjustSizeOfScalingAnchors()
        {
            ApplyAnchorScale(_gizmo.topAnchor);
            ApplyAnchorScale(_gizmo.topRightAnchor);
            ApplyAnchorScale(_gizmo.rightAnchor);
            ApplyAnchorScale(_gizmo.bottomRightAnchor);
            ApplyAnchorScale(_gizmo.bottomAnchor);
            ApplyAnchorScale(_gizmo.bottomLeftAnchor);
            ApplyAnchorScale(_gizmo.leftAnchor);
            ApplyAnchorScale(_gizmo.topLeftAnchor);
            GameElementUpdater.UpdateEdgePoints(target, true);
        }
        
        /// <summary>
        /// Drags the given <paramref name="scalingGizmo"/> along its axis.
        /// </summary>
        /// <param name="anchor">scaling gizmo to be dragged</param>
        private void DragAnchor(GameObject anchor)
        {
            if (anchor == _gizmo.topLeftAnchor || anchor == _gizmo.topRightAnchor ||
                anchor == _gizmo.bottomLeftAnchor || anchor == _gizmo.bottomRightAnchor)
            {
                GameNodeMover.MoveToLockAxes(anchor,true, false, true);
            } else if (anchor == _gizmo.topAnchor || anchor == _gizmo.bottomAnchor)
            {
                GameNodeMover.MoveToLockAxes(anchor, false, false, true);
            }else if (anchor == _gizmo.leftAnchor || anchor == _gizmo.rightAnchor)
            {
                GameNodeMover.MoveToLockAxes(anchor, true, false , false);
            }
        }
        /// <summary>
        /// Draws the gizmos that allow a user to scale the object in all three dimensions.
        /// </summary>
        private void DrawGizmo()
        {
            _gizmo.topAnchor = InstantiateAndScaleAnchor();
            _gizmo.topRightAnchor = InstantiateAndScaleAnchor();
            _gizmo.rightAnchor = InstantiateAndScaleAnchor();
            _gizmo.bottomRightAnchor = InstantiateAndScaleAnchor();
            _gizmo.bottomAnchor = InstantiateAndScaleAnchor();
            _gizmo.bottomLeftAnchor = InstantiateAndScaleAnchor();
            _gizmo.leftAnchor = InstantiateAndScaleAnchor();
            _gizmo.topLeftAnchor = InstantiateAndScaleAnchor();
            SetOnSide();
            gizmoDrawn = true;
        }
        
        /// <summary>
        /// Sets the side spheres.
        /// </summary>
        private void SetOnSide()
        {
            Transform targetTransform = target.transform;
            float radius = ScalingAnchorRadius();
            float offsetX = targetTransform.lossyScale.x / 2 + radius;
            float offsetZ = targetTransform.lossyScale.z / 2 + radius;


            Vector3 Corner(float offsetX, float offsetZ)
            {
                Vector3 result = targetTransform.position;
                result.y = target.GetRoof();
                result.x += offsetX;
                result.z += offsetZ;
                return result;
            }

            {
                // Top Anchor
                Vector3 pos = Corner(0, offsetZ);
                _gizmo.topAnchor.transform.position = pos;
                _gizmo.topOld = pos;
            }
            {
                // Top right Anchor
                Vector3 pos = Corner(offsetX, offsetZ);
                _gizmo.topRightAnchor.transform.position = pos;
                _gizmo.topRightOld = pos;
            }
            {
                // Right Anchor
                Vector3 pos = Corner(offsetX, 0);
                _gizmo.rightAnchor.transform.position = pos;
                _gizmo.rightOld = pos;
            }

            {
                // Right bottom anchor
               Vector3 pos = Corner(offsetX, -offsetZ);
                _gizmo.bottomRightAnchor.transform.position = pos;
                _gizmo.bottomRightOld = pos;
            }

            {
                // Bottom anchor
                Vector3 pos = Corner(0, -offsetZ);
                _gizmo.bottomAnchor.transform.position = pos;
                _gizmo.bottomOld = pos;
            }

            {
                // Bottom left anchor
                Vector3 pos = Corner(-offsetX, -offsetZ);
                _gizmo.bottomLeftAnchor.transform.position = pos;
                _gizmo.bottomLeftOld = pos;
            }

            {
                //Left anchor
                Vector3 pos = Corner(-offsetX, 0);
                _gizmo.leftAnchor.transform.position = pos;
                _gizmo.leftOld = pos;
            }

            {
                //Top left anchor
                Vector3 pos = Corner(-offsetX, offsetZ);
                _gizmo.topLeftAnchor.transform.position = pos;
                _gizmo.topLeftOld = pos;
            }
        }

        
        /// <summary>
        /// Sets the new scale of <see cref="target"/> based on the scaling gizmos.
        /// </summary>
        private void ScaleTarget()
        {
            float yScale = target.transform.localScale.y;
            Vector3 scale = Vector3.zero;
            scale.x -= _gizmo.leftAnchor.transform.position.x - _gizmo.leftOld.x;
            scale.x += _gizmo.rightAnchor.transform.position.x - _gizmo.rightOld.x;
            scale.z -= _gizmo.bottomAnchor.transform.position.z - _gizmo.bottomOld.z;
            scale.z += _gizmo.topAnchor.transform.position.z - _gizmo.topOld.z;
            
            //Corner
            float sclaeCorner = 0;
            sclaeCorner -= _gizmo.bottomLeftAnchor.transform.position.x - _gizmo.bottomLeftOld.x +
                           (_gizmo.bottomLeftAnchor.transform.position.z - _gizmo.bottomLeftOld.z);
            sclaeCorner += _gizmo.bottomRightAnchor.transform.position.x - _gizmo.bottomRightOld.x -
                           (_gizmo.bottomRightAnchor.transform.position.z - _gizmo.bottomRightOld.z);
            sclaeCorner += _gizmo.topRightAnchor.transform.position.x - _gizmo.topRightOld.x +
                           (_gizmo.topRightAnchor.transform.position.z - _gizmo.topRightOld.z);
            sclaeCorner -= _gizmo.topLeftAnchor.transform.position.x - _gizmo.topLeftOld.x -
                           (_gizmo.topLeftAnchor.transform.position.z - _gizmo.topLeftOld.z);
            scale.x += sclaeCorner;
            scale.z += sclaeCorner;

            Vector3 position = target.transform.position;
            position.y += scale.y / 2;

            _gizmo.topOld = _gizmo.topAnchor.transform.position;
            _gizmo.topRightOld = _gizmo.topRightAnchor.transform.position;
            _gizmo.rightOld = _gizmo.rightAnchor.transform.position;
            _gizmo.bottomRightOld = _gizmo.bottomRightAnchor.transform.position;
            _gizmo.bottomOld = _gizmo.bottomAnchor.transform.position;
            _gizmo.bottomLeftOld = _gizmo.bottomLeftAnchor.transform.position;
            _gizmo.leftOld = _gizmo.leftAnchor.transform.position;
            _gizmo.topLeftOld = _gizmo.topLeftAnchor.transform.position;

            scale = target.transform.lossyScale + scale;
            if (scale.x <= 0)
            {
                scale.x = target.transform.lossyScale.x;
            }

            if (scale.y <= 0)
            {
                scale.y = target.transform.lossyScale.y;
                position.y = target.transform.position.y;
            }

            if (scale.z <= 0)
            {
                scale.z = target.transform.lossyScale.z;
            }

            target.transform.position = position;
            target.SetScale(scale);
            
        }

        private GameObject InstantiateAndScaleAnchor()
        {
            GameObject anchor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ApplyAnchorScale(anchor);
            return anchor;
        }

        private void ApplyAnchorScale(GameObject anchor)
        {
            Vector3 targetScale = target.transform.lossyScale;
            anchor.transform.localScale = Vector3.one * Mathf.Max(Mathf.Min(targetScale.x, targetScale.z) * relativeSphereScale, minimalSphereScale);
        }
        
        
        /// <summary>
        /// Returns the radius of the sphere used to visualize the gizmo to scale the object.
        /// </summary>
        /// <returns>radius of the sphere</returns>
        private float ScalingAnchorRadius()
        {
            // Assumptions: We assume firstCornerSphere has the same scale as every
            // other scaling sphere and that it is actually a sphere (more precisely,
            // that its width and depth are the same so that we can use the x scale
            // or the z scale; it does not matter).
            return _gizmo.topAnchor.transform.lossyScale.x / 2.0f;
        }
    }
}