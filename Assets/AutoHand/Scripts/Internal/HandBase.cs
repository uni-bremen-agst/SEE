using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Autohand {

    /// <summary>
    /// 
    /// </summary>
    public enum HandMovementType {
        /// <summary>Movement method for Auto Hand V2 and below</summary>
        Legacy,
        /// <summary>Uses physics forces</summary>
        Forces
    }

    public enum HandType {
        both,
        right,
        left,
        none
    }

    public enum GrabType {
        /// <summary>On grab, hand will move to the grabbable, create grab connection, then return to follow position</summary>
        HandToGrabbable,
        /// <summary>On grab, grabbable will move to the hand, then create grab connection</summary>
        GrabbableToHand,
        /// <summary>On grab, grabbable instantly travel to the hand</summary>
        InstantGrab
    }

    [Serializable]
    public struct VelocityTimePair {
        public float time;
        public Vector3 velocity;
    }

    public delegate void HandGrabEvent(Hand hand, Grabbable grabbable);
    public delegate void HandGameObjectEvent(Hand hand, GameObject other);

    [Serializable]  public class UnityHandGrabEvent : UnityEvent<Hand, Grabbable> { }
    [Serializable] public class UnityHandEvent : UnityEvent<Hand> { }



    [RequireComponent(typeof(Rigidbody)), DefaultExecutionOrder(10)]
    /// <summary>This is the base of the Auto Hand hand class, used for organizational purposes</summary>
    public class HandBase : MonoBehaviour {


        [AutoHeader("AUTO HAND")]
        public bool ignoreMe;

        public Finger[] fingers;

        [Tooltip("An empty GameObject that should be placed on the surface of the center of the palm")]
        public Transform palmTransform;

        [FormerlySerializedAs("isLeft")]
        [Tooltip("Whether this is the left (on) or right (off) hand")]
        public bool left = false;


        [Space]


        [Tooltip("Maximum distance for pickup"), Min(0.01f)]
        public float reachDistance = 0.3f;


        [AutoToggleHeader("Enable Movement", 0, 0, tooltip = "Whether or not to enable the hand's Rigidbody Physics movement")]
        public bool enableMovement = true;

        [EnableIf("enableMovement"), Tooltip("Follow target, the hand will always try to match this transforms position with rigidbody movements")]
        public Transform follow;

        [EnableIf("enableMovement"), Tooltip("Returns hand to the target after this distance [helps just in case it gets stuck]"), Min(0)]
        public float maxFollowDistance = 0.5f;


        [EnableIf("enableMovement"), Tooltip("Amplifier for applied velocity on released object"), Min(0)]
        public float throwPower = 1f;


        [HideInInspector]
        public bool advancedFollowSettings = true;

        [AutoToggleHeader("Enable Auto Posing", 0, 0, tooltip = "Auto Posing will override Unity Animations -- This will disable all the Auto Hand IK, including animations from: finger sway, pose areas, finger bender scripts (runtime Auto Posing will still work)")]
        [Tooltip("Turn this on when you want to animate the hand or use other IK Drivers")]
        public bool enableIK = true;

        [EnableIf("enableIK"), Tooltip("How much the fingers sway from the velocity")]
        public float swayStrength = 0.7f;

        [EnableIf("enableIK"), Tooltip("This will offset each fingers bend (0 is no bend, 1 is full bend)")]
        public float gripOffset = 0.1f;






        //HIDDEN ADVANCED SETTINGS
        [NonSerialized, Tooltip("The maximum allowed velocity of the hand"), Min(0)]
        public float maxVelocity = 12f;

        [NonSerialized, Tooltip("Follow target speed (Can cause jittering if turned too high - recommend increasing drag with speed)"), Min(0)]
        public float followPositionStrength = 70;
        [HideInInspector, NonSerialized]
        public float startDrag = 10;

        [HideInInspector, NonSerialized, Tooltip("Follow target rotation speed (Can cause jittering if turned too high - recommend increasing angular drag with speed)"), Min(0)]
        public float followRotationStrength = 110;
        [HideInInspector, NonSerialized]
        public float startAngularDrag = 35;

         [HideInInspector, NonSerialized, Tooltip("After this many seconds velocity data within a 'throw window' will be tossed out. (This allows you to get only use acceeleration data from the last 'x' seconds of the throw.)")]
        public float throwVelocityExpireTime = 0.125f;
        [HideInInspector, NonSerialized, Tooltip("After this many seconds velocity data within a 'throw window' will be tossed out. (This allows you to get only use acceeleration data from the last 'x' seconds of the throw.)")]
        public float throwAngularVelocityExpireTime = 0.25f;

        [HideInInspector, NonSerialized, Tooltip("Increase for closer finger tip results / Decrease for less physics checks - The number of steps the fingers take when bending to grab something")]
        public int fingerBendSteps = 100;

        [HideInInspector, NonSerialized]
        public float sphereCastRadius = 0.04f;

        [HideInInspector]
        public bool usingPoseAreas = true;

        [HideInInspector]
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;


        Grabbable HoldingObj = null;
        public Grabbable holdingObj {
            get { return HoldingObj; }
            internal set { HoldingObj = value; }
        }

        Grabbable _lookingAtObj = null;
        public Grabbable lookingAtObj {
            get { return _lookingAtObj; }
            protected set { _lookingAtObj = value; }
        }

        Transform _moveTo = null;
        public Transform moveTo {
            get {
                if(!gameObject.activeInHierarchy)
                    return null;

                if(_moveTo == null) {
                    _moveTo = new GameObject().transform;
                    _moveTo.parent = transform.parent;
                    _moveTo.name = "HAND FOLLOW POINT";
                }

                return _moveTo;
            }
        }


        Rigidbody _body;
        public Rigidbody body { 
            get{
                if(_body == null)
                    _body = GetComponent<Rigidbody>();

                return _body;
            }
            internal set { _body = body; } 
        }

        Vector3 _grabPositionOffset = Vector3.zero;
        public Vector3 grabPositionOffset {
            get { return _grabPositionOffset; }
            internal set { _grabPositionOffset = value; }
        }

        Quaternion _grabRotationOffset = Quaternion.identity;
        public Quaternion grabRotationOffset {
            get { return _grabRotationOffset; }
            internal set { _grabRotationOffset = value; }
        }

        public bool disableIK {
            get { return !enableIK; }
            set { enableIK = !value; }
        }

        private CollisionTracker _collisionTracker;
        public CollisionTracker collisionTracker {
            get {
                if(_collisionTracker == null)
                    _collisionTracker = gameObject.AddComponent<CollisionTracker>();
                return _collisionTracker;
            }
            protected set {
                if(_collisionTracker != null)
                    Destroy(_collisionTracker);

                _collisionTracker = value;
            }
        }

        protected GrabbablePose _grabPose;
        protected GrabbablePose grabPose {
            get {
                return _grabPose;
            }
    
            set {
                if(value == null && _grabPose != null)
                    _grabPose.CancelHandPose(this as Hand);

                _grabPose = value;
            }
        }

        [HideInInspector, NonSerialized]
        public ConfigurableJoint heldJoint;

        public bool grabbing { get; protected set; }
        public bool squeezing { get; protected set; }
        public HandPoseArea handPoseArea { get; protected set; }

        protected float gripAxis;
        protected float squeezeAxis;

        protected Coroutine handAnimateRoutine;
        protected HandPoseData preHandPoseAreaPose;

        internal List<Collider> handColliders = new List<Collider>();

        Transform _handGrabPoint;
        internal Transform handGrabPoint {
            get {
                if(_handGrabPoint == null && gameObject.scene.isLoaded) {
                    _handGrabPoint = new GameObject().transform;
                    _handGrabPoint.name = "grabPoint";
                }
                return _handGrabPoint;
            }
        }


        Transform _localGrabbablePoint;
        internal Transform localGrabbablePoint {
            get {
                if(!gameObject.activeInHierarchy)
                    _localGrabbablePoint = null;
                else if(gameObject.activeInHierarchy && _localGrabbablePoint == null) {
                    _localGrabbablePoint = new GameObject().transform;
                    _localGrabbablePoint.name = "grabPosition";
                    _localGrabbablePoint.parent = transform;
                }


                return _localGrabbablePoint;
            }
        }

        internal int handLayers;

        protected Collider palmCollider;
        protected RaycastHit highlightHit;

        protected HandVelocityTracker velocityTracker;
        protected Transform palmChild;
        protected Vector3 lastFrameFollowPos;
        protected Quaternion lastFrameFollowRot;
        protected bool ignoreMoveFrame;

        protected Vector3 followVel;
        protected Vector3 followAngularVel;

        internal bool allowUpdateMovement = true;

        protected Vector3[] updatePositionTracked = new Vector3[3];

        protected List<RaycastHit> closestHits = new List<RaycastHit>();
        protected List<Grabbable> closestGrabs = new List<Grabbable>();
        protected int tryMaxDistanceCount;

        protected Vector3 lastFollowPosition;
        protected Vector3 lastFollowRotation;

        protected int noCollisionFrames = 0;
        protected int collisionFrames = 0;
        protected bool prerendered = false;
        protected Vector3 preRenderPos;
        protected Quaternion preRenderRot;
        protected float currGrip = 1f;
        protected bool usingDynamicTimestep;

        protected virtual void Awake() {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.None;
            body.useGravity = false;

            body.solverIterations = 100;
            body.solverVelocityIterations = 100;

            if(palmCollider == null) {
                palmCollider = palmTransform.gameObject.AddComponent<BoxCollider>();
                (palmCollider as BoxCollider).size = new Vector3(0.2f, 0.15f, 0.05f);
                (palmCollider as BoxCollider).center = new Vector3(0f, 0f, -0.025f);
                palmCollider.enabled = false;
            }

            if(palmChild == null) {
                palmChild = new GameObject().transform;
                palmChild.parent = palmTransform;
            }

#if (UNITY_2020_3_OR_NEWER)
            var cams = FindObjectsOfType<Camera>(true);
#else
            var cams = FindObjectsOfType<Camera>();
#endif
            foreach(var cam in cams) {
                if(cam.targetDisplay == 0) {
                    bool found = false;
                    var handStabilizers = cam.gameObject.GetComponents<HandStabilizer>();
                    foreach(var handStabilizer in handStabilizers) {
                        if(handStabilizer.hand == this)
                            found = true;
                    }
                    if(!found)
                        cam.gameObject.AddComponent<HandStabilizer>().hand = this;
                }
            }
            
            if(velocityTracker == null)
                velocityTracker = new HandVelocityTracker(this);

            usingDynamicTimestep = AutoHandSettings.UsingDynamicTimestep();
            if(usingDynamicTimestep) {
                if(FindObjectOfType<DynamicTimestepSetter>() == null) {
                    new GameObject() { name = "DynamicFixedTimeSetter" }.AddComponent<DynamicTimestepSetter>();
                    Debug.Log("AUTO HAND: Creating Dynamic Timestepper");
                }
            }
        }

        protected virtual void OnEnable() {
            SetHandCollidersRecursive(transform);
        }

        protected virtual void OnDisable() {
            handColliders.Clear();
        }

        protected virtual void OnDestroy() {
            if(handGrabPoint != null)
                Destroy(handGrabPoint.gameObject);
            if(localGrabbablePoint != null)
                Destroy(localGrabbablePoint.gameObject);
            if(moveTo != null)
                Destroy(moveTo.gameObject);
        }

        protected virtual void FixedUpdate(){
            if(follow != null) {
                followVel = follow.position - lastFollowPosition;
                followAngularVel = follow.rotation.eulerAngles - lastFollowRotation;
                lastFollowPosition = follow.position;
                lastFollowRotation = follow.rotation.eulerAngles;
            }

            if (!IsGrabbing() && enableMovement && follow != null && !body.isKinematic) {
                MoveTo(Time.fixedDeltaTime);
                TorqueTo(Time.fixedDeltaTime);
            }

            velocityTracker.UpdateThrowing();

            if(ignoreMoveFrame){
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            ignoreMoveFrame = false;

            if(CollisionCount() > 0) {
                noCollisionFrames = 0;
                collisionFrames++;
            }
            else {
                noCollisionFrames++;
                collisionFrames = 0;
            }

            if(holdingObj != null)
                holdingObj.HeldFixedUpdate();

            for(int i = 1; i < updatePositionTracked.Length; i++)
                updatePositionTracked[i] = updatePositionTracked[i - 1];
            updatePositionTracked[0] = transform.localPosition;

            if(!IsGrabbing())
                UpdateFingers(Time.fixedDeltaTime);
        }

        protected virtual void Update(){
            SetMoveTo();
        }

        //This is used to force the hand to always look like its where it should be even when physics is being weird
        public virtual void OnPreRender(){
            preRenderPos = transform.position;
            preRenderRot = transform.rotation;

            //Hides fixed joint jitterings
            if(!prerendered && holdingObj != null && holdingObj.customGrabJoint == null && !IsGrabbing()) {
                transform.position = handGrabPoint.position;
                transform.rotation = handGrabPoint.rotation;
                prerendered = true;
            }
        }

        //This puts everything where it should be for the physics update
        public virtual void OnPostRender(){
            //Returns position after hiding for camera
            if(prerendered && holdingObj != null && holdingObj.customGrabJoint == null && !IsGrabbing()) {
                transform.position = preRenderPos;
                transform.rotation = preRenderRot;
            }
            prerendered = false;
        }




        /// <summary>Creates Joints between hand and grabbable, does not call grab events</summary>
        protected virtual void CreateJoint(Grabbable grab, float breakForce, float breakTorque){
            if(grab.customGrabJoint == null){
                var jointCopy = (Resources.Load<ConfigurableJoint>("DefaultJoint"));
                var newJoint = gameObject.AddComponent<ConfigurableJoint>().GetCopyOf(jointCopy);
                newJoint.anchor = Vector3.zero;
                newJoint.breakForce = breakForce;
                if(grab.HeldCount() == 1)
                    newJoint.breakForce += 500;
                newJoint.breakTorque = breakTorque;
                newJoint.connectedBody = grab.body;
                newJoint.enablePreprocessing = jointCopy.enablePreprocessing;
                newJoint.autoConfigureConnectedAnchor = false;
                newJoint.connectedAnchor = grab.body.transform.InverseTransformPoint(handGrabPoint.position);
                newJoint.angularXMotion = jointCopy.angularXMotion;
                newJoint.angularYMotion = jointCopy.angularYMotion;
                newJoint.angularZMotion = jointCopy.angularZMotion;

                heldJoint = newJoint;
            }
            else {
                var newJoint = grab.body.gameObject.AddComponent<ConfigurableJoint>().GetCopyOf(grab.customGrabJoint);
                newJoint.anchor = Vector3.zero;
                if(grab.HeldCount() == 1)
                    newJoint.breakForce += 500;
                newJoint.breakForce = breakForce;
                newJoint.breakTorque = breakTorque;
                newJoint.connectedBody = body;
                newJoint.enablePreprocessing = grab.customGrabJoint.enablePreprocessing;
                newJoint.autoConfigureConnectedAnchor = false;
                newJoint.connectedAnchor = grab.body.transform.InverseTransformPoint(handGrabPoint.position);
                newJoint.angularXMotion = grab.customGrabJoint.angularXMotion;
                newJoint.angularYMotion = grab.customGrabJoint.angularYMotion;
                newJoint.angularZMotion = grab.customGrabJoint.angularZMotion;
                heldJoint = newJoint;
            }
        }

        float lastMoveToDistance = float.MaxValue;
        //====================== MOVEMENT  =======================
        //========================================================
        //========================================================

        /// <summary>Moves the hand to the controller rotation using physics movement</summary>
        protected virtual void MoveTo(float deltaTime) {
            SetMoveTo();

            if(followPositionStrength <= 0)
                return;

            var movePos = moveTo.position;
            var distance = Vector3.Distance(movePos, transform.position);

            if(lastMoveToDistance != distance) {
                lastMoveToDistance = distance;
                //Returns if out of distance, if you aren't holding anything
                if(distance > maxFollowDistance) {
                    if(holdingObj != null) {
                        if(holdingObj.parentOnGrab && tryMaxDistanceCount < 1) {
                            SetHandLocation(movePos, transform.rotation);
                            tryMaxDistanceCount += 2;
                        }
                        else if(!holdingObj.parentOnGrab || tryMaxDistanceCount >= 1) {
                            holdingObj.ForceHandRelease(this as Hand);
                            SetHandLocation(movePos, transform.rotation);
                        }
                    }
                    else {
                        SetHandLocation(movePos, transform.rotation);
                    }
                }

                if(tryMaxDistanceCount > 0)
                    tryMaxDistanceCount--;

                distance = Mathf.Clamp(distance, 0, 0.5f);

                SetVelocity(0.5f);


                void SetVelocity(float minVelocityChange) {

                    var velocityClamp = holdingObj != null ? holdingObj.maxHeldVelocity : maxVelocity;

                    Vector3 vel = (movePos - transform.position).normalized * followPositionStrength * distance;

                    vel.x = Mathf.Clamp(vel.x, -velocityClamp, velocityClamp);
                    vel.y = Mathf.Clamp(vel.y, -velocityClamp, velocityClamp);
                    vel.z = Mathf.Clamp(vel.z, -velocityClamp, velocityClamp);

                    var towardsVel = new Vector3(
                        Mathf.MoveTowards(body.velocity.x, vel.x, minVelocityChange + Mathf.Abs(body.velocity.x) * Time.fixedDeltaTime * 60),
                        Mathf.MoveTowards(body.velocity.y, vel.y, minVelocityChange + Mathf.Abs(body.velocity.y) * Time.fixedDeltaTime * 60),
                        Mathf.MoveTowards(body.velocity.z, vel.z, minVelocityChange + Mathf.Abs(body.velocity.z) * Time.fixedDeltaTime * 60)
                    );

                    body.velocity = towardsVel;
                }
            }
        }

        /// <summary>Rotates the hand to the controller rotation using physics movement</summary>
        protected virtual void TorqueTo(float deltaTime) {
            var delta = (moveTo.rotation * Quaternion.Inverse(body.rotation));
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (float.IsInfinity(axis.x))
                return;

            if(angle > 180f)
                angle -= 360f;

            var multiLinear = Mathf.Deg2Rad * angle * followRotationStrength;
            Vector3 angular = multiLinear * axis.normalized;
            angle = Mathf.Abs(angle);

            var angleStrengthOffset = Mathf.Lerp(1f, 2f, angle / 45f);
            body.angularDrag = Mathf.Lerp(startAngularDrag + 10, startAngularDrag, angle) * Time.fixedDeltaTime * 60;


            body.angularVelocity = new Vector3(
                Mathf.MoveTowards(body.angularVelocity.x, angular.x, followRotationStrength * 5f * Time.fixedDeltaTime * 60 * angleStrengthOffset),
                Mathf.MoveTowards(body.angularVelocity.y, angular.y, followRotationStrength * 5f * Time.fixedDeltaTime * 60 * angleStrengthOffset),
                Mathf.MoveTowards(body.angularVelocity.z, angular.z, followRotationStrength * 5f * Time.fixedDeltaTime * 60 * angleStrengthOffset)
            );

            
        }

        ///<summary>Moves the hand and whatever it might be holding (if teleport allowed) to given pos/rot</summary>
        public virtual void SetHandLocation(Vector3 pos, Quaternion rot) {

            if(holdingObj && holdingObj.parentOnGrab) {
                if (!IsGrabbing()) {
                    ignoreMoveFrame = true;

                    if(holdingObj.HeldCount() > 1) {
                        pos += transform.position - moveTo.position;
                        rot *= (Quaternion.Inverse(moveTo.rotation)* transform.rotation);
                    }

                    var handRuler = AutoHandExtensions.transformRuler;
                    handRuler.position = transform.position;
                    handRuler.rotation = transform.rotation;

                    var grabRuler = AutoHandExtensions.transformRulerChild;
                    grabRuler.position = holdingObj.body.transform.position;
                    grabRuler.rotation = holdingObj.body.transform.rotation;

                    handRuler.position = pos;
                    handRuler.rotation = rot;

                    var deltaHandRot = rot * Quaternion.Inverse(transform.rotation);
                
                    var deltaGrabPos = grabRuler.position - holdingObj.body.transform.position;
                    var deltaGrabRot = Quaternion.Inverse(grabRuler.rotation) * holdingObj.body.transform.rotation;

                    transform.position = handRuler.position;
                    transform.rotation = handRuler.rotation;
                    body.position = transform.position;
                    body.rotation = transform.rotation;
                    holdingObj.body.transform.position = grabRuler.position;
                    holdingObj.body.transform.rotation = grabRuler.rotation;
                    holdingObj.body.position = holdingObj.body.transform.position;
                    holdingObj.body.rotation = holdingObj.body.transform.rotation;

                    body.velocity = deltaHandRot * body.velocity;
                    body.angularVelocity = deltaHandRot * body.angularVelocity;

                    grabPositionOffset = deltaGrabRot * grabPositionOffset;

                    foreach(var jointed in holdingObj.jointedBodies)
                        if(!(jointed.CanGetComponent(out Grabbable grab) && grab.HeldCount() > 0)) {
                            jointed.position += deltaGrabPos;
                            jointed.transform.RotateAround(holdingObj.body.transform, deltaGrabRot);
                        }

                    velocityTracker.ClearThrow();

                }


            }
            else {
                ignoreMoveFrame = true;
                transform.position = pos;
                transform.rotation = rot;
                body.position = pos;
                body.rotation = rot;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            SetMoveTo();
        }

        ///<summary>Moves the hand and keeps the local rotation</summary>
        public virtual void SetHandLocation(Vector3 pos) {
            SetMoveTo();
            SetHandLocation(pos, transform.rotation);
        }

        /// <summary>Resets the hand location to the follow</summary>
        public void ResetHandLocation() {
            SetHandLocation(moveTo.position, moveTo.rotation);
        }

        /// <summary>Updates the target used to calculate velocity / movements towards follow</summary>
        public void SetMoveTo(bool ignoreHeld = false) {
            if(follow == null)
                return;

            //Sets [Move To] Object
            moveTo.position = follow.position + grabPositionOffset;
            moveTo.rotation = follow.rotation * grabRotationOffset;

            //Adjust the [Move To] based on offsets 
            if(holdingObj != null) {

                if(left) {
                    var leftRot = -holdingObj.heldRotationOffset;
                    leftRot.x *= -1;
                    moveTo.localRotation *= Quaternion.Euler(leftRot);
                    var moveLeft = holdingObj.heldPositionOffset;
                    moveLeft.x *= -1;
                    moveTo.position += transform.rotation * moveLeft;
                }
                else {
                    moveTo.position += transform.rotation * holdingObj.heldPositionOffset;
                    moveTo.localRotation *= Quaternion.Euler(holdingObj.heldRotationOffset);
                }
            }


            if(!ignoreHeld && holdingObj != null && !holdingObj.ignoreWeight) {
                var heldBy = holdingObj.GetHeldBy(true, true);
                for(int i = 0; i < heldBy.Count; i++)
                    if(heldBy[i] != this && holdingObj.moveTos.ContainsKey(heldBy[i])) {
                        var mag = (holdingObj.moveTos[heldBy[i]]).magnitude*8f ;
                        mag = Mathf.Lerp(1.5f, 1.05f, mag);
                        moveTo.position += holdingObj.moveTos[heldBy[i]] / mag;
                    }
            }
        }

        /// <summary>Whether or not this hand can grab the grabbbale based on hand and grabbable settings</summary>
        public bool CanGrab(Grabbable grab) {
            var cantHandSwap = (grab.IsHeld() && grab.singleHandOnly && !grab.allowHeldSwapping);
            return (grab.CanGrab(this) && !IsGrabbing() && !cantHandSwap);
        }



        public float GetTriggerAxis() {
            return gripAxis;
        }





        Collider[] handHighlightNonAlloc = new Collider[128];
        /// <summary>Finds the closest raycast from a cone of rays -> Returns average direction of all hits</summary>
        protected virtual Vector3 HandClosestHit(out RaycastHit closestHit, out Grabbable grabbable, float dist, int layerMask, Grabbable target = null) {
            Grabbable grab;
            Vector3 palmForward = palmTransform.forward;
            Vector3 palmPosition = palmTransform.position;
            GameObject rayHitObject;
            Grabbable lastRayHitGrabbable = null;
            Ray ray = new Ray();
            RaycastHit hit;
            Collider col;

            closestGrabs.Clear();
            closestHits.Clear();
            var checkSphereRadius = reachDistance * 1.35f;
            int overlapCount = Physics.OverlapSphereNonAlloc(palmPosition + palmForward * (checkSphereRadius * 0.9f), checkSphereRadius, handHighlightNonAlloc, layerMask, QueryTriggerInteraction.Ignore);


            for(int i = 0; i < overlapCount; i++) {
                col = handHighlightNonAlloc[i];

                if(!(col is MeshCollider) || (col as MeshCollider).convex == true) {
                    Vector3 closestPoint = col.ClosestPoint(palmTransform.transform.position);
                    ray.direction = closestPoint - palmTransform.position;
                }
                else {
                    ray.direction = palmTransform.forward;
                }

                ray.origin = palmTransform.transform.position;
                ray.origin = Vector3.MoveTowards(ray.origin, col.bounds.center, 0.001f);

                if(ray.direction != Vector3.zero && Vector3.Angle(ray.direction, palmTransform.forward) < 100 && Physics.Raycast(ray, out hit, checkSphereRadius*2, layerMask, QueryTriggerInteraction.Ignore)) {

                    rayHitObject = hit.collider.gameObject;
                    if(closestGrabs.Count > 0)
                        lastRayHitGrabbable = closestGrabs[closestGrabs.Count - 1];

                    if(closestGrabs.Count > 0 && rayHitObject == lastRayHitGrabbable.gameObject) {
                        if(target == null) {
                            closestGrabs.Add(lastRayHitGrabbable);
                            closestHits.Add(hit);
                        }
                    }
                    else if(rayHitObject.HasGrabbable(out grab) && CanGrab(grab)) {
                        if(target == null || target == grab) {
                            closestGrabs.Add(grab);
                            closestHits.Add(hit);
                        }
                    }
                }
            }

            int closestHitCount = closestHits.Count;

            if(closestHitCount > 0) {
                closestHit = closestHits[0];
                grabbable = closestGrabs[0];
                Vector3 dir = Vector3.zero;
                for(int i = 0; i < closestHitCount; i++) {
                    if(closestHits[i].distance / closestGrabs[i].grabPriorityWeight < closestHit.distance / grabbable.grabPriorityWeight) {
                        closestHit = closestHits[i];
                        grabbable = closestGrabs[i];
                    }

                    dir += closestHits[i].point - palmTransform.position;
                }

                if(holdingObj == null && !IsGrabbing()) {
                    if(handGrabPoint.parent != closestHit.transform)
                        handGrabPoint.parent = closestHit.collider.transform;
                    handGrabPoint.position = closestHit.point;
                    handGrabPoint.up = closestHit.normal;
                }

                return dir / closestHitCount;
            }

            closestHit = new RaycastHit();
            grabbable = null;
            return Vector3.zero;
        }

        private void OnDrawGizmosSelected() {
            var radius = reachDistance;
            Gizmos.DrawWireSphere(palmTransform.position + palmTransform.forward * radius, radius);
        }


        public bool IsPosing() {
            return handPoseArea != null || (holdingObj != null && holdingObj.HasCustomPose()) || handAnimateRoutine != null;
        }

        float fingerSwayVel;
        /// <summary>Determines how the hand should look/move based on its flags</summary>
        protected virtual void UpdateFingers(float deltaTime) {
            var averageVel = Vector3.zero;
            for (int i = 1; i < updatePositionTracked.Length; i++)
                averageVel += updatePositionTracked[i] - updatePositionTracked[i - 1];
            averageVel /= updatePositionTracked.Length;
            if(transform.parent != null)
                averageVel = (Quaternion.Inverse(palmTransform.rotation)*transform.parent.rotation)*averageVel;


            //Responsable for movement finger sway
            if (!grabbing && !disableIK && !IsPosing() && !holdingObj)
            {


                float vel = (averageVel*60).z;

                if (CollisionCount() > 0) vel = 0;
                fingerSwayVel = Mathf.MoveTowards(fingerSwayVel, vel, deltaTime * (Mathf.Abs((fingerSwayVel-vel) * 30f)));



                float grip = gripOffset + swayStrength * fingerSwayVel;
                currGrip = grip;

                foreach (var finger in fingers)
                {
                    finger.UpdateFinger(grip);
                }
            }
        }



        public int CollisionCount() {
            if(holdingObj != null)
                return collisionTracker.collisionObjects.Count + holdingObj.CollisionCount();
            return collisionTracker.collisionObjects.Count;
        }


        public void HandIgnoreCollider(Collider collider, bool ignore) {
            for(int i = 0; i < handColliders.Count; i++)
                Physics.IgnoreCollision(handColliders[i], collider, ignore);
        }


        public void SetLayerRecursive(Transform obj, int newLayer) {
            obj.gameObject.layer = newLayer;
            for(int i = 0; i < obj.childCount; i++) {
                SetLayerRecursive(obj.GetChild(i), newLayer);
            }
        }


        protected void SetHandCollidersRecursive(Transform obj) {
            handColliders.Clear();
            AddHandCol(obj);

            void AddHandCol(Transform obj1) {
                foreach(var col in obj1.GetComponents<Collider>())
                    handColliders.Add(col);

                for(int i = 0; i < obj1.childCount; i++) {
                    AddHandCol(obj1.GetChild(i));
                }
            }
        }



        /// <summary>Returns the current throw velocity</summary>
        public Vector3 ThrowVelocity() { return velocityTracker.ThrowVelocity(); }

        /// <summary>Returns the current throw angular velocity</summary>
        public Vector3 ThrowAngularVelocity() { return velocityTracker.ThrowAngularVelocity(); }






        /// <summary>Returns true during the time between when a grab starts and a hold begins</summary>
        public bool IsGrabbing() {
            return grabbing;
        }


        public bool IsHolding() {
            return holdingObj != null;
        }


        public static int GetHandsLayerMask() {
            return LayerMask.GetMask(Hand.rightHandLayerName, Hand.leftHandLayerName);
        }



    }
}