using Autohand;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class VrGrabber : MonoBehaviour
    {
        /// <summary>
        /// Left back vector of Portal
        /// </summary>
        private Vector2 leftFront;

        /// <summary>
        /// Right back vector of Portal.
        /// </summary>
        private Vector2 rightBack;

        /// <summary>
        ///
        /// </summary>
        private bool AdjustGrabbedNode = false;

        /// <summary>
        /// Is gameobject grabbed.
        /// </summary>
        public static bool IsGrabbed { get; private set; }= false;

        /// <summary>
        /// The grabbed gameobject.
        /// </summary>
        public static GameObject GrabbedObject { get; private set; }

        /// <summary>
        /// Is the grabbed object being released.
        /// </summary>
        private bool IsReleased = false;

        /// <summary>
        /// Layer to ignore collision of parent with its children while grabbed.
        /// </summary>
        private const int IgnoreChildrenLayer = 10;

        /// <summary>
        /// Original layer.
        /// </summary>
        private const int GrabbableLayer = 29;

        private bool IsColliding = false;

        /// <summary>
        /// Adds listeners to the grabbable component for onGrab and onRelease. Also for the distance grabbable
        /// component for on pull.
        /// </summary>
        public void Start()
        {
            Grabbable grabbable = gameObject.GetComponent<Grabbable>();

            grabbable.onGrab = new UnityHandGrabEvent();
            grabbable.onRelease = new UnityHandGrabEvent();

            grabbable.onGrab.AddListener(OnGrab);
            grabbable.onRelease.AddListener(OnRelease);

            // FIXME: Distance grabbing not working properly right now.
            DistanceGrabbable distanceGrabbable = gameObject.GetComponent<DistanceGrabbable>();
            distanceGrabbable.OnPull = new UnityHandGrabEvent();
            distanceGrabbable.OnPull.AddListener(OnPull);
        }

        /// <summary>
        /// This function is executed when a node is grabbed.
        /// </summary>
        /// <param name="hand">Hand object.</param>
        /// <param name="grabbable">Grabbable Component.</param>
        private void OnGrab(Hand hand, Grabbable grabbable)
        {
            if (gameObject.TryGetNode(out Node node) && !node.IsRoot())
            {
                // Set layer of all children to ignore collision with the grabbed parent object (include inactive gameobjects).
                GameObjectExtensions.SetAllChildLayer(grabbable.gameObject.transform, IgnoreChildrenLayer, true);
                grabbable.gameObject.layer = GrabbableLayer;

                Rigidbody rigidbody = grabbable.gameObject.GetComponent<Rigidbody>();
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.isKinematic = false;

                AdjustGrabbedNode = true;
                IsGrabbed = true;

                GrabbedObject = grabbable.gameObject;
                Debug.LogWarning("Player grabbed node: " + GrabbedObject.name);

                // Get portal of node and save values.
                Portal.GetPortal(gameObject, out Vector2 leftFront, out Vector2 rightBack);
                this.leftFront = leftFront;
                this.rightBack = rightBack;

                // Set portal to infinite while moving the node.
                Portal.SetInfinitePortal(gameObject);
            }
        }

        /// <summary>
        /// This function is executed when the player releases the object.
        /// </summary>
        /// <param name="hand">Hand object.</param>
        /// <param name="grabbable">Grabbable Component.</param>
        private void OnRelease(Hand hand, Grabbable grabbable)
        {
            if (gameObject.TryGetNode(out Node node) && !node.IsRoot())
            {
                IsReleased = true;
                IsGrabbed = false;
                Rigidbody rigidbody = grabbable.gameObject.GetComponent<Rigidbody>();
                rigidbody.interpolation = RigidbodyInterpolation.None;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rigidbody.isKinematic = true;

                Debug.LogWarning("Player released grabbed object: " + GrabbedObject);
                GrabbedObject = null;

                // Set portal to old values when released
                // FIXME: Should remain visible outside the portal when released, or only be able to be moved within the portal.
                Portal.SetInfinitePortal(gameObject);
                //Portal.SetPortal(gameObject, leftFront, rightBack);

                // Set layer back to normal (Grabbable layer).
                GameObjectExtensions.SetAllChildLayer(grabbable.gameObject.transform, GrabbableLayer, true);
            }
        }

        /// <summary>
        /// Reparent on collision.
        /// </summary>
        /// <param name="collisionInfo"></param>
        private void OnCollisionEnter(Collision collisionInfo)
        {
            //IsColliding = true;
            if (collisionInfo.gameObject.TryGetNode(out Node node) && IsGrabbed && !node.IsRoot() && GrabbedObject == gameObject)
            {
                if (GrabbedObject.transform.parent.gameObject != collisionInfo.gameObject)
                {
                    Debug.LogWarning("Collision with node: " + collisionInfo.gameObject.name);
                    MoveAction.ReparentVR(collisionInfo.gameObject);
                }
            }
        }

        /// <summary>
        /// UnReparent on exit collision.
        /// </summary>
        /// <param name="collisionInfo"></param>
        private void OnCollisionExit(Collision collisionInfo)
        {
            //IsColliding = false;
            if (collisionInfo.gameObject.TryGetNode(out Node node) && IsGrabbed && !node.IsRoot() &&
                    gameObject == GrabbedObject)
                {
                    if (!IsDescendant(GrabbedObject, collisionInfo.gameObject))
                    {
                        Debug.LogWarning("UnReparenting - exit collision with node: " + collisionInfo.gameObject.name);
                        MoveAction.UnReparentVR();
                    }
                }

        }

        /// <summary>
        /// We will use FixedUpdate when working with rigidbodys.
        /// FixedUpdate occurs at a measured time step that typically does not coincide with MonoBehaviour.Update.
        /// </summary>
/*
        public void FixedUpdate()
        {

            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();

            if (IsColliding)
            {
                rigidbody.velocity = new Vector3(0, 0, 0);
            }

            /*
            // Adjust grabbed node only once.
            if (AdjustGrabbedNode)
            {
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.isKinematic = false;
                AdjustGrabbedNode = false;
            }
            // Adjust grabbed node when released.
            else if (IsReleased)
            {
                // IsColliding = false;
                IsGrabbed = false;

                rigidbody.interpolation = RigidbodyInterpolation.None;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rigidbody.isKinematic = true;

                IsReleased = false;
            }

        }
    */

        /// <summary>
        /// This function is called when the object is grabbed from a distance. //FIXME not working properly
        /// Currently has the same functionality as <see cref="OnGrab"/>
        /// </summary>
        /// <param name="hand">Hand object.</param>
        /// <param name="grabbable">Grabbable object.</param>
        private void OnPull(Hand hand, Grabbable grabbable)
        {
            if (gameObject.TryGetNode(out Node node) && !node.IsRoot())
            {
                // Set layer of all children to ignore collision with the grabbed parent object (include inactive gameobjects).
                GameObjectExtensions.SetAllChildLayer(grabbable.gameObject.transform, IgnoreChildrenLayer, true);
                grabbable.gameObject.layer = GrabbableLayer;

                AdjustGrabbedNode = true;
                IsGrabbed = true;

                GrabbedObject = grabbable.gameObject;
                Debug.LogWarning("Player grabbed node: " + GrabbedObject.name);

                // Get portal of node and save values.
                Portal.GetPortal(gameObject, out Vector2 leftFront, out Vector2 rightBack);
                this.leftFront = leftFront;
                this.rightBack = rightBack;

                // Set portal to infinite while moving the node.
                Portal.SetInfinitePortal(gameObject);
            }
        }

        // True if node is a descendant of root in the underlying graph.
        private bool IsDescendant(GameObject node, GameObject root)
        {
            return node.GetNode().IsDescendantOf(root.GetNode());
        }
    }
}