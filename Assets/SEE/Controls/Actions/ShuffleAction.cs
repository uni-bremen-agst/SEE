using SEE.Game;
using SEE.Game.Operator;
using SEE.UI3D;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Implements shuffling a code city on its plane.
    /// </summary>
    /// <remarks>This component is attached to a player. It is used in the prefab for
    /// players. This class has subclasses specialized to the environment (desktop,
    /// VR, etc.).</remarks>
    internal class ShuffleAction : MonoBehaviour
    {
        /// <summary>
        /// Whether the user has initiated shuffling.
        /// </summary>
        private bool shuffling = false;

        /// <summary>
        /// The root game node of the code city (tagged by Tags.Node) that is shuffled.
        /// </summary>
        private Transform cityRootNode = null;

        /// <summary>
        /// The original position of <see cref="cityRootNode"/> before the user has
        /// started to shuffle it.
        /// </summary>
        private Vector3 originalPosition;

        /// <summary>
        /// The Operator of <see cref="cityRootNode"/>.
        /// </summary>
        private NodeOperator nodeOperator;

        /// <summary>
        /// The number of degrees in a full circle.
        /// </summary>
        private const float fullCircleDegree = 360.0f;

        /// <summary>
        /// The number of snap steps in the circle.
        /// </summary>
        private const float snapStepCount = 8;

        /// <summary>
        /// The angle of each snap step in the circle in degrees.
        /// </summary>
        private const float snapStepAngle = fullCircleDegree / snapStepCount;

        /// <summary>
        /// The gizmo serving as a visual aid for the shuffling.
        /// </summary>
        private static MoveGizmo gizmo;

        /// <summary>
        /// The position of <see cref="cityRootNode"/> when it was started to be shuffled
        /// (its center position in world space).
        /// </summary>
        private Vector3 dragStartTransformPosition = Vector3.positiveInfinity;

        /// <summary>
        /// The difference between the position where the plane containing the <see cref="cityRootNode"/>
        /// was hit and the actual center position of <see cref="cityRootNode"/> in world space when the user
        /// started to shuffle the code city.
        /// </summary>
        private Vector3 dragStartOffset = Vector3.positiveInfinity;

        /// <summary>
        /// Assigns <see cref="gizmo"/> if not already assigned.
        /// </summary>
        private void Awake()
        {
            if (gizmo == null)
            {
                gizmo = MoveGizmo.Create();
            }
        }

        private void Update()
        {
            bool synchronize = false;

            if (SEEInput.Cancel()) // cancel shuffling
            {
                if (shuffling)
                {
                    // Reset to initial state.
                    nodeOperator.MoveTo(originalPosition);
                    gizmo.gameObject.SetActive(false);
                    cityRootNode = null;
                    shuffling = false;
                    synchronize = true;
                }
            }
            else if (SEEInput.Drag() && SEEInput.DragHovered())
            {
                if (!shuffling)
                {
                    // Let's see whether we have hit a code-city element.
                    if (Raycasting.RaycastGraphElement(out RaycastHit _, out GraphElementRef elementRef, false) != HitGraphElement.None)
                    {
                        // cityRootNode is the node containing the hoveredObject
                        cityRootNode = SceneQueries.GetCityRootTransformUpwards(elementRef.gameObject.transform);
                        Assert.IsNotNull(cityRootNode);
                        // Remember the original position of the city-root node so that it can be reset
                        // to its original position.
                        originalPosition = cityRootNode.position;
                        // The node operator that is going to be used to move the city-root node
                        nodeOperator = cityRootNode.gameObject.NodeOperator();

                        // Where exactly have we hit the plane containing cityRootNode (if at all)?
                        if (Raycasting.RaycastPlane(new UnityEngine.Plane(Vector3.up, cityRootNode.position), out Vector3 cityPlaneHitPoint))
                        {
                            gizmo.gameObject.SetActive(true);
                            dragStartTransformPosition = originalPosition;
                            dragStartOffset = cityPlaneHitPoint - originalPosition;
                        }
                        // The user is initiating shuffling.
                        shuffling = true;
                    }
                }

                // Continue shuffling.
                if (shuffling
                    && Raycasting.RaycastPlane(new UnityEngine.Plane(Vector3.up, cityRootNode.position), out Vector3 planeHitPoint))
                {
                    // The plane in which the cityRoodNode lies is hit. We accept movements
                    // only within this area.
                    Vector3 totalDragOffsetFromStart = Vector3.Scale(planeHitPoint - (dragStartTransformPosition + dragStartOffset), cityRootNode.localScale);
                    if (SEEInput.Snap())
                    {
                        Vector2 point2 = new(totalDragOffsetFromStart.x, totalDragOffsetFromStart.z);
                        float angleDeg = point2.Angle360();
                        float snappedAngleDeg = Mathf.Round(angleDeg / snapStepAngle) * snapStepAngle;
                        float snappedAngleRad = Mathf.Deg2Rad * snappedAngleDeg;
                        Vector2 dir = new(Mathf.Cos(snappedAngleRad), Mathf.Sin(-snappedAngleRad));
                        Vector2 proj = dir * Vector2.Dot(point2, dir);
                        totalDragOffsetFromStart = new Vector3(proj.x, totalDragOffsetFromStart.y, proj.y);
                    }

                    // The root node must remain in its plane and must not be re-parented.
                    Vector3 newPosition = dragStartTransformPosition + totalDragOffsetFromStart;
                    nodeOperator.MoveXTo(newPosition.x, 0);
                    nodeOperator.MoveZTo(newPosition.z, 0);

                    gizmo.SetPositions(dragStartTransformPosition + dragStartOffset, cityRootNode.position);

                    synchronize = true;
                }
            }
            else if (SEEInput.Reset()) // reset to center of table
            {
                if (cityRootNode && !shuffling)
                {
                    GO.Plane plane = cityRootNode.GetComponentInParent<GO.Plane>();
                    nodeOperator.MoveTo(plane.CenterTop);
                    new ShuffleNetAction(cityRootNode.name, plane.CenterTop).Execute();
                    gizmo.gameObject.SetActive(false);

                    synchronize = false; // We just called ShuffleNetAction for the synchronization.
                }
            }
            else if (shuffling)
            {
                // Shuffling has ended.
                gizmo.gameObject.SetActive(false);
                shuffling = false;
            }

            if (synchronize)
            {
                new ShuffleNetAction(cityRootNode.name, cityRootNode.position).Execute();
            }
        }
    }
}
