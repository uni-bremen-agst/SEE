using System.Collections.Generic;
using Autohand;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.GO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

//using Valve.VR.InteractionSystem;

namespace SEE.Game
{
    /// <summary>
    /// Adds components required for interacting with a game object.
    /// </summary>
    internal static class InteractionDecorator
    {
        /// <summary>
        /// Adds the following components to given <paramref name="gameObject"/>:
        /// <see cref="Interactable"/>, <see cref="InteractableObject"/>,
        /// <see cref="ShowHovering"/>, <see cref="ShowSelection"/>, <see cref="ShowGrabbing"/>.
        /// If <paramref name="gameObject"/> has a <see cref="NodeRef"/>, then the following
        /// components are added in addition to the ones above:
        /// <see cref="GameNodeScaler"/>, <see cref="ShowLabel"/>, <see cref="HighlightErosion"/>.
        ///
        /// Note: The <paramref name="gameObject"/> is assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameObject">game object where the components are to be added to</param>
        public static void PrepareForInteraction(GameObject gameObject)
        {
            gameObject.isStatic = false; // we want to move the object during the game

#if false // FIXME STEAMVR
            Interactable interactable = gameObject.AddComponentIfNecessary<Interactable>();
            interactable.highlightOnHover = false;
#endif
            gameObject.AddComponentIfNecessary<InteractableObject>();
            // The following additions of components must come after the addition of InteractableObject
            // because they require the presence of an InteractableObject.
            gameObject.AddComponentIfNecessary<ShowHovering>();
            gameObject.AddComponentIfNecessary<ShowSelection>();
            gameObject.AddComponentIfNecessary<ShowGrabbing>();
            if (gameObject.HasNodeRef())
            {
                gameObject.AddComponentIfNecessary<GameNodeScaler>();
                gameObject.AddComponentIfNecessary<ShowLabel>();
                gameObject.AddComponentIfNecessary<ShowEdges>();
                gameObject.AddComponentIfNecessary<HighlightErosion>();

                SetupRigidbody();
                SetupGrabbable();
                gameObject.AddOrGetComponent<VrGrabAction>();

            }

            // Add AutoHand related components.
            void SetupRigidbody()
            {
                Rigidbody rigidbody = gameObject.AddOrGetComponent<Rigidbody>();
                rigidbody.useGravity = false; // No gravity for every node,
                rigidbody.isKinematic = true; // Initial every node is kinematic.
                rigidbody.freezeRotation = true; // No rotation while grabbed.

                // It is recommended to turn on interpolation for the main character but disable it for everything else.
                // https://docs.unity3d.com/ScriptReference/Rigidbody-interpolation.html
                // Interpolation is initially deactivated and is only activated for the moving object.
                rigidbody.interpolation = RigidbodyInterpolation.None; // Interpolation

                // For best results, set this value to CollisionDetectionMode.ContinuousDynamic for fast moving objects,
                // and for other objects which these need to collide with, set it to CollisionDetectionMode.Continuous.
                // https://docs.unity3d.com/ScriptReference/Rigidbody-collisionDetectionMode.html
                // Initially set to continous.
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

                // FIXME: obsolete because of ignoreWeight flag in grabbable component?
                rigidbody.mass = 15;
                rigidbody.drag = 15;
                rigidbody.angularDrag = 1;

                // Set the physic material to reduce bounciness.
                if (gameObject.TryGetComponent(out BoxCollider boxCollider))
                {
                    PhysicMaterial noBounciness = Resources.Load<PhysicMaterial>("Materials/Physics/NoBounciness");
                    boxCollider.material = noBounciness;

                    /*
                    IgnoreHandPlayerCollision ignoreHandPlayerCollision = gameObject.AddComponent<IgnoreHandPlayerCollision>();
                    ignoreHandPlayerCollision.colliders = new List<Collider>();
                    ignoreHandPlayerCollision.colliders.Add(boxCollider);
                    */
                }
            }

            // Configure grabbable component.
            void SetupGrabbable()
            {
                Grabbable grabbable = gameObject.AddOrGetComponent<Grabbable>();

                //gameObject.AddComponent<GrabbableCollisionHaptics>();

                // Hand grab type
                grabbable.grabType = HandGrabType.GrabbableToHand;

                // Which hands are allowed to grab (Both\Left\Right).
                grabbable.handType = HandType.right;

                // Single hand only for now.
                grabbable.singleHandOnly = true;

                // If false single handed items cannot passes back and forth on grab.
                grabbable.allowHeldSwapping = false;

                // If true replaces physics material with NoFriction while grabbed.
                grabbable.heldNoFriction = false;

                // Will parent the grabbable to the Hand's PARENT on the grab.
                // Should be true for any object you can pickup and carry away.
                grabbable.parentOnGrab = false;

                // Will apply a movement follower component (only while held) to simulate weightlessness.
                grabbable.ignoreWeight = true;

                // the hand holding this grabbable will ignore these colliders only while holding this grabbable.
                // For example, a door handle where you don't want the door collider to interfere with the hand colliders
                // grabbable.heldIgnoreColliders = ...;

                // Determines how long the hand will ignore the colliders of the grabbable when released.
                // Allows for smoother releases with thrown objects, recommend setting to 0 for things like doors and walls.
                //grabbable.ignoreReleaseTime = 0;

                grabbable.throwPower = 0;
                grabbable.jointBreakForce = 5000;

                //gameObject.AddComponent<GrabLock>();

                // Sets the highlight material.
                Material mat = Resources.Load<Material>("Materials/HighlightMaterial/Highlight");
                grabbable.hightlightMaterial = mat;

                // Distance Grabbing FIXME: Not working as intended right now.
                DistanceGrabbable distanceGrabbable = gameObject.AddComponent<DistanceGrabbable>();
                distanceGrabbable.targetedMaterial = mat;
                distanceGrabbable.rotate = false;
                distanceGrabbable.instantPull = true;
                //distanceGrabbable.grabType = DistanceGrabType.Velocity;
            }
        }

        /// <summary>
        /// Adds a component of type <typeparamref name="T"/> to <paramref name="gameObject"/>
        /// if <paramref name="gameObject"/> does not have one already. The new or the
        /// existing component, respectively, is returned.
        /// </summary>
        /// <typeparam name="T">component that should be part of <paramref name="gameObject"/></typeparam>
        /// <param name="gameObject">game object that should have a component of <typeparamref name="T"/></param>
        /// <returns>component in <paramref name="gameObject"/></returns>
        private static T AddComponentIfNecessary<T>(this GameObject gameObject) where T : MonoBehaviour
        {
            if (gameObject.TryGetComponent(out T component))
            {
                return component;
            }
            else
            {
                return gameObject.AddComponent<T>();
            }
        }

        /// <summary>
        /// Adds the following components to all <paramref name="gameObjects"/>:
        /// <see cref="Interactable"/>, <see cref="InteractableObject"/>,
        /// <see cref="ShowHovering"/>, <see cref="ShowSelection"/>, <see cref="ShowGrabbing"/>.
        /// If a element in <paramref name="gameObjects"/> has a <see cref="NodeRef"/>, then the following
        /// components are added in addition to the ones above:
        /// <see cref="GameNodeScaler"/>, <see cref="ShowLabel"/>, <see cref="EyeGazeHandler"/>,
        /// <see cref="HighlightErosion"/>.
        ///
        /// Note: All <paramref name="gameObjects"/> are assumed to represent a graph node
        /// or edge.
        /// </summary>
        /// <param name="gameObjects">game objects where the components are to be added to</param>
        public static void PrepareForInteraction(IEnumerable<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                PrepareForInteraction(go);
            }
        }
    }
}