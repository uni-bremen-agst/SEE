using System.Collections;
using Autohand;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /*
     * The VrGrabAction class is responsible for handling the grabbing and releasing of objects in a VR environment.
     * When an object is grabbed, it adjusts its properties and handles collisions with other objects.
     *
     * FIXME: Consider scaling based on something else, or not. It's a matter for discussion among the team.
     */
    public class VrGrabAction : MonoBehaviour
    {
        /// <summary>
        /// Indicates whether a game object is currently grabbed
        /// </summary>
        public static bool IsGrabbed { get; private set; } = false;

        /// <summary>
        /// The currently grabbed game object
        /// </summary>
        public static GameObject GrabbedObject { get; private set; }

        /// <summary>
        /// Layer to ignore collision between parent and children
        /// </summary>
        private const int IgnoreChildrenLayer = 10;

        /// <summary>
        /// The original layer of grabbable objects
        /// </summary>
        private const int GrabbableLayer = 29;

        /// <summary>
        /// The initial position of the grabbed object
        /// </summary>
        private Vector3 initialPosition;

        /// <summary>
        /// The initial rotation of the grabbed object
        /// </summary>
        private Quaternion initialRotation;

        /// <summary>
        /// The initial rotation of the grabbed object
        /// </summary>
        private Vector3 initialLocalScale;
        
        /// <summary>
        /// Indicates whether a collision is allowed to occur
        /// </summary>
        private bool allowCollision = true;
        
        /// <summary>
        /// The collision cooldown. Currently on half a second
        /// </summary>
        private float collisionCooldownTime = 0.5f;


        /// <summary>
        /// Adds listeners to the grabbable component for onGrab and onRelease events.
        /// </summary>
        public void Start()
        {
            Grabbable grabbable = gameObject.GetComponent<Grabbable>();
            grabbable.onGrab = new UnityHandGrabEvent();
            grabbable.onRelease = new UnityHandGrabEvent();
            grabbable.onGrab.AddListener(OnGrab);
            grabbable.onRelease.AddListener(OnRelease);
        }

        /// <summary>
        /// Called when the object is grabbed.
        /// </summary>
        /// <param name="hand">The hand that grabbed the object.</param>
        /// <param name="grabbable">The Grabbable component of the grabbed object.</param>
        private void OnGrab(Hand hand, Grabbable grabbable)
        {
            var currentGameObject = grabbable.gameObject;

            // Set the initial position and rotation of the object
            initialPosition = currentGameObject.transform.position;
            initialRotation = currentGameObject.transform.rotation;
            initialLocalScale = currentGameObject.transform.localScale;

            if (gameObject.TryGetNode(out Node node) && !node.IsRoot() && currentGameObject != null)
            {
                IsGrabbed = true;
                // Set the layer of all children to ignore collision with the grabbed parent object
                GameObjectExtensions.SetAllChildLayer(grabbable.gameObject.transform, IgnoreChildrenLayer, true);
                grabbable.gameObject.layer = GrabbableLayer;

                // Rigidbody Setup
                Rigidbody rigidbody = grabbable.gameObject.GetComponent<Rigidbody>();
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.isKinematic = false;

                GrabbedObject = grabbable.gameObject;
                //Debug.LogWarning("Player grabbed node: " + GrabbedObject.name);
                Portal.SetInfinitePortal(gameObject);
                StartCoroutine(StartCollisionCooldown());
            }
        }
        
        /// <summary>
        /// Starts a cooldown period during which collisions are disabled.
        /// </summary>
        /// <returns>An IEnumerator to be used with StartCoroutine.</returns>
        private IEnumerator StartCollisionCooldown()
        {
            allowCollision = false;

            yield return new WaitForSeconds(collisionCooldownTime);

            allowCollision = true;
        }

        /// <summary>
        /// Called when the object is released.
        /// </summary>
        /// <param name="hand">The hand that released the object.</param>
        /// <param name="grabbable">The Grabbable component of the released object.</param>
        private void OnRelease(Hand hand, Grabbable grabbable)
        {
            if (gameObject.TryGetNode(out Node node) && !node.IsRoot() && IsInCollision())
            {
                IsGrabbed = false;
                Rigidbody rigidbody = grabbable.gameObject.GetComponent<Rigidbody>();
                rigidbody.interpolation = RigidbodyInterpolation.None;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rigidbody.isKinematic = true;

                //Debug.LogWarning("Player released grabbed object: " + GrabbedObject);

                Portal.SetInfinitePortal(gameObject);

                // Set the layer of all children back to normal (Grabbable layer)
                GameObjectExtensions.SetAllChildLayer(grabbable.gameObject.transform, GrabbableLayer, true);
            }

            if (GrabbedObject != null)
            {
                // Reset the position and rotation of the grabbed object to its initial state
                GrabbedObject.transform.position = initialPosition;
                GrabbedObject.transform.rotation = initialRotation;
                GrabbedObject.transform.localScale = initialLocalScale;
            }
        }

        /// <summary>
        /// Checks if the grabbed object is currently in collision with any other object.
        /// </summary>
        /// <returns>True if the object is still in collision, false otherwise.</returns>
        private bool IsInCollision()
        {
            Collider[] colliders = GrabbedObject.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                if (collider.enabled && IsColliding(collider))
                {
                    return true; // Object is still in collision
                }
            }

            return false; // Object is no longer in collision
        }

        /// <summary>
        /// Checks if a specific collider is currently in collision.
        /// </summary>
        /// <param name="collider">The collider to check.</param>
        /// <returns>True if a collision is detected, false otherwise.</returns>
        private bool IsColliding(Collider collider)
        {
            if (!allowCollision) return false;
            
            Collider[] colliders = Physics.OverlapBox(collider.bounds.center, collider.bounds.extents,
                collider.transform.rotation);
            foreach (Collider otherCollider in colliders)
            {
                if (otherCollider != collider && otherCollider.enabled && otherCollider.gameObject != GrabbedObject)
                {
                    return true; // Collision detected
                }
            }

            return false; // No collision detected
        }

        /// <summary>
        /// Called when a collision occurs with another object.
        /// </summary>
        /// <param name="collisionInfo">Information about the collision.</param>
        private void OnCollisionEnter(Collision collisionInfo)
        {
            // Check if the collided object has a Node, is not the root node, and is the currently grabbed object
            if (collisionInfo.gameObject.TryGetNode(out Node node) && IsGrabbed && !node.IsRoot() &&
                GrabbedObject == gameObject)
            {
                if (GrabbedObject.transform.parent.gameObject != collisionInfo.gameObject && IsInCollision())
                {
                    //Debug.LogWarning("Collision with node: " + collisionInfo.gameObject.name);
                    MoveAction.StartReparentProcess(collisionInfo.gameObject, GrabbedObject);
                    // Set the new initial position and rotation of the object
                    initialPosition = GrabbedObject.transform.position;
                    initialRotation = GrabbedObject.transform.rotation;
                    initialLocalScale = GrabbedObject.transform.localScale;
                }

                if ( GrabbedObject.transform.parent.gameObject == collisionInfo.gameObject && IsInCollision())
                {
                    // Set the new initial position and rotation of the object
                    initialPosition = GrabbedObject.transform.position;
                    initialRotation = GrabbedObject.transform.rotation;
                    initialLocalScale = GrabbedObject.transform.localScale;
                }
            }
        }

        /// <summary>
        /// Called when the object exits a collision with another object.
        /// </summary>
        /// <param name="collisionInfo">Information about the collision.</param>
        private void OnCollisionExit(Collision collisionInfo)
        {
        }

        /*
        private bool IsDescendant(GameObject node, GameObject root)
        {
            return node.GetNode().IsDescendantOf(root.GetNode());
        }
        
        //FIXME not working properly
        /// <summary>
        /// We will use FixedUpdate when working with rigidbodys.
        /// FixedUpdate occurs at a measured time step that typically does not coincide with MonoBehaviour.Update.
        /// </summary>
        public void FixedUpdate()
        {
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        
            if (IsColliding)
            {
                rigidbody.velocity = new Vector3(0, 0, 0);
            }
        
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
        
        /// <summary>
        /// This function is called when the object is grabbed from a distance. 
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
        */
    }
}