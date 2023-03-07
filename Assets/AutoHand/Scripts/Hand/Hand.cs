using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace Autohand {
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/hand"), DefaultExecutionOrder(10)]
    public class Hand : HandBase {


        [AutoToggleHeader("Enable Highlight", 0, 0, tooltip = "Raycasting for grabbables to highlight is expensive, you can disable it here if you aren't using it")]
        public bool usingHighlight = true;

        [EnableIf("usingHighlight")]
        [Tooltip("The layers to highlight and use look assist on --- Nothing will default on start")]
        public LayerMask highlightLayers;

        [EnableIf("usingHighlight")]
        [Tooltip("Leave empty for none - used as a default option for all grabbables with empty highlight material")]
        public Material defaultHighlight;


        [AutoToggleHeader("Show Advanced")]
        public bool showAdvanced = false;


        [ShowIf("showAdvanced")]
        [Tooltip("Whether the hand should go to the object and come back on grab, or the object to float to the hand on grab. Will default to HandToGrabbable for objects that have \"parentOnGrab\" disabled")]
        public GrabType grabType = GrabType.HandToGrabbable;

        [ShowIf("showAdvanced")]
        [Tooltip("Makes grab smoother; also based on range and reach distance - a very near grab is minGrabTime and a max distance grab is maxGrabTime"), Min(0)]
        public float minGrabTime = 0.1f;
        [ShowIf("showAdvanced")]
        [Tooltip("Makes grab smoother; also based on range and reach distance - a very near grab is minGrabTime and a max distance grab is maxGrabTime"), Min(0)]
        public float maxGrabTime = 0.25f;

        [ShowIf("showAdvanced")]
        [Tooltip("The animation curve based on the grab time 0-1"), Min(0)]
        public AnimationCurve grabCurve;

        [ShowIf("showAdvanced")]
        [Tooltip("Speed at which the gentle grab returns the grabbable"), Min(0)] [FormerlySerializedAs("smoothReturnSpeed")]
        public float gentleGrabSpeed = 1;

        [ShowIf("showAdvanced")]
        [Tooltip("This is used in conjunction with custom poses. For a custom pose to work it must has the same PoseIndex as the hand. Used for when your game has multiple hands")]
        public int poseIndex = 0;

        [AutoLine]
        public bool ignoreMe1;





#if UNITY_EDITOR
        bool editorSelected = false;
#endif


        public static string[] grabbableLayers = { "Grabbable", "Grabbing" };

        //The layer is used and applied to all grabbables in if the hands layer is set to default
        public static string grabbableLayerNameDefault = "Grabbable";
        //This helps the auto grab distinguish between what item is being grabbaed and the items around it
        public static string grabbingLayerName = "Grabbing";

        //This was added by request just in case you want to add different layers for left/right hand
        public static string rightHandLayerName = "Hand";
        public static string leftHandLayerName = "Hand";



        ///Events for all my programmers out there :)/// 
        /// <summary>Called when the grab event is triggered, event if nothing is being grabbed</summary>
        public event HandGrabEvent OnTriggerGrab;
        /// <summary>Called at the very start of a grab before anything else</summary>
        public event HandGrabEvent OnBeforeGrabbed;
        /// <summary>Called when the hand grab connection is made (the frame the hand touches the grabbable)</summary>
	    public event HandGrabEvent OnGrabbed;

        /// <summary>Called when the release event is triggered, event if nothing is being held</summary>
        public event HandGrabEvent OnTriggerRelease;
        public event HandGrabEvent OnBeforeReleased;
        /// <summary>Called at the end the release</summary>
        public event HandGrabEvent OnReleased;

        /// <summary>Called when the squeeze button is pressed, regardless of whether an object is held or not (grab returns null)</summary>
        public event HandGrabEvent OnSqueezed;
        /// <summary>Called when the squeeze button is released, regardless of whether an object is held or not (grab returns null)</summary>
        public event HandGrabEvent OnUnsqueezed;

        /// <summary>Called when highlighting starts</summary>
        public event HandGrabEvent OnHighlight;
        /// <summary>Called when highlighting ends</summary>
        public event HandGrabEvent OnStopHighlight;

        /// <summary>Called whenever joint breaks or force release event is called</summary>
        public event HandGrabEvent OnForcedRelease;
        /// <summary>Called when the physics joint between the hand and the grabbable is broken by force</summary>
        public event HandGrabEvent OnGrabJointBreak;

        /// <summary>Legacy Event - same as OnRelease</summary>
        public event HandGrabEvent OnHeldConnectionBreak;

        public event HandGameObjectEvent OnHandCollisionStart;
        public event HandGameObjectEvent OnHandCollisionStop;
        public event HandGameObjectEvent OnHandTriggerStart;
        public event HandGameObjectEvent OnHandTriggerStop;

        public Grabbable lastHoldingObj { get; private set; }
        List<HandTriggerAreaEvents> triggerEventAreas = new List<HandTriggerAreaEvents>();

        Coroutine tryGrab;
        Coroutine highlightRoutine;
        float startGrabDist;
        HandPoseData openHandPose;
        float grabTime;
        Vector3 grabReturnPositionDistance;
        Quaternion grabReturnRotationDistance;

        Coroutine _grabRoutine;
        Coroutine grabRoutine {
            get { return _grabRoutine; }
            set {
                if(value != null && _grabRoutine != null) {
                    StopCoroutine(_grabRoutine);
                    if(holdingObj != null) {
                        holdingObj.body.velocity = Vector3.zero;
                        holdingObj.body.angularVelocity = Vector3.zero;
                        holdingObj.beingGrabbed = false;
                    }
                    BreakGrabConnection();
                }
                _grabRoutine = value;
            }
        }


        

        protected override void Awake() {
            SetLayerRecursive(transform, LayerMask.NameToLayer(left ? Hand.leftHandLayerName : Hand.rightHandLayerName));

            if(highlightLayers.value == 0 || highlightLayers == LayerMask.GetMask("")) {
                highlightLayers = LayerMask.GetMask(grabbableLayerNameDefault);
            }

            handLayers = LayerMask.GetMask(rightHandLayerName, leftHandLayerName, "HandPlayer");
            
            base.Awake();

            if(enableMovement) {
                body.drag = startDrag;
                body.angularDrag = startAngularDrag;
                body.useGravity = false;
            }
        }


        private void Start()
        {

#if UNITY_EDITOR
            if (Selection.activeGameObject == gameObject)
            {
                Selection.activeGameObject = null;
                Debug.Log("Auto Hand: highlighting hand component in the inspector can cause lag and quality reduction at runtime in VR. (Automatically deselecting at runtime) Remove this code at any time.", this);
                editorSelected = true;
            }

            Application.quitting += () => { if (editorSelected && Selection.activeGameObject == null) Selection.activeGameObject = gameObject; };
#endif
        }


        protected override void OnEnable() {
            base.OnEnable();
            highlightRoutine = StartCoroutine(HighlightUpdate(1/30f));
            collisionTracker.OnCollisionFirstEnter += OnCollisionFirstEnter;
            collisionTracker.OnCollisionLastExit += OnCollisionLastExit;
            collisionTracker.OnTriggerFirstEnter += OnTriggerFirstEnter;
            collisionTracker.OnTriggeLastExit += OnTriggerLastExit;

            collisionTracker.OnCollisionFirstEnter += OnCollisionFirstEnterEvent;
            collisionTracker.OnCollisionLastExit += OnCollisionLastExitEvent;
            collisionTracker.OnTriggerFirstEnter += OnTriggerFirstEnterEvent;
            collisionTracker.OnTriggeLastExit += OnTriggeLastExitEvent;

        }

        protected override void OnDisable() {
            foreach(var trigger in triggerEventAreas)
                trigger.Exit(this);

            if(tryGrab != null)
                StopCoroutine(tryGrab);
            if(highlightRoutine != null)
                StopCoroutine(highlightRoutine);

            base.OnDisable();
            collisionTracker.OnCollisionFirstEnter -= OnCollisionFirstEnter;
            collisionTracker.OnCollisionLastExit -= OnCollisionLastExit;
            collisionTracker.OnTriggerFirstEnter -= OnTriggerFirstEnter;
            collisionTracker.OnTriggeLastExit -= OnTriggerLastExit;

            collisionTracker.OnCollisionFirstEnter -= OnCollisionFirstEnterEvent;
            collisionTracker.OnCollisionLastExit -= OnCollisionLastExitEvent;
            collisionTracker.OnTriggerFirstEnter -= OnTriggerFirstEnterEvent;
            collisionTracker.OnTriggeLastExit -= OnTriggeLastExitEvent;
        }


        protected override void Update() {
            if(enableMovement) {
                if(holdingObj && !holdingObj.maintainGrabOffset && !IsGrabbing()) {
                    var deltaDist = Vector3.Distance(follow.position, lastFrameFollowPos);
                    var deltaRot = Quaternion.Angle(follow.rotation, lastFrameFollowRot);
                    grabPositionOffset = Vector3.MoveTowards(grabPositionOffset, Vector3.zero, (deltaDist) * gentleGrabSpeed * Time.deltaTime * 60f);
                    grabRotationOffset = Quaternion.RotateTowards(grabRotationOffset, Quaternion.identity, (deltaRot) * gentleGrabSpeed * Time.deltaTime * 60f);

                    if(!holdingObj.useGentleGrab) {
                        GetGrabTime();
                        grabPositionOffset = Vector3.MoveTowards(grabPositionOffset, Vector3.zero, grabReturnPositionDistance.magnitude * (Time.deltaTime / GetGrabTime()) + deltaDist * Time.deltaTime * 120f);
                        grabRotationOffset = Quaternion.RotateTowards(grabRotationOffset, Quaternion.identity, grabReturnRotationDistance.eulerAngles.magnitude * (Time.deltaTime / GetGrabTime()) + (deltaRot) * Time.deltaTime * 120f);
                    }
                }

                lastFrameFollowPos = follow.position;
                lastFrameFollowRot = follow.rotation;
            }
            base.Update();
        }


        float GetGrabTime() {
            var distanceDivider = Mathf.Clamp01(startGrabDist / reachDistance);
            return Mathf.Clamp(minGrabTime + ((maxGrabTime - minGrabTime) * distanceDivider), 0, maxGrabTime);
        }

        //================== CORE INTERACTION FUNCTIONS ===================
        //================================================================
        //================================================================


        /// <summary>Function for controller trigger fully pressed -> Grabs whatever is directly in front of and closest to the hands palm (by default this is called by the hand controller link component)</summary>
        public virtual void Grab() {
            Grab(grabType);
        }

        /// <summary>Function for controller trigger fully pressed -> Grabs whatever is directly in front of and closest to the hands palm</summary>
        public virtual void Grab(GrabType grabType) {
            OnTriggerGrab?.Invoke(this, null);
            foreach(var triggerArea in triggerEventAreas) {
                triggerArea.Grab(this);
            }
            if(usingHighlight && !grabbing && holdingObj == null && lookingAtObj != null) {
                var newGrabType = this.grabType;
                if(lookingAtObj.grabType != HandGrabType.Default)
                    newGrabType = lookingAtObj.grabType == HandGrabType.GrabbableToHand ? GrabType.GrabbableToHand : GrabType.HandToGrabbable;

                grabRoutine = StartCoroutine(GrabObject(GetHighlightHit(), lookingAtObj, newGrabType));
            }

            else if(!grabbing && holdingObj == null) {
                if(HandClosestHit(out RaycastHit closestHit, out Grabbable grabbable, reachDistance, ~handLayers) != Vector3.zero && grabbable != null) {
                    var newGrabType = this.grabType;
                    if(grabbable.grabType != HandGrabType.Default)
                        newGrabType = grabbable.grabType == HandGrabType.GrabbableToHand ? GrabType.GrabbableToHand : GrabType.HandToGrabbable;
                    if(grabbable != null)
                        grabRoutine = StartCoroutine(GrabObject(closestHit, grabbable, newGrabType));
                }
            }
            else if(holdingObj != null && holdingObj.CanGetComponent(out GrabLock grabLock)) {
                grabLock.OnGrabPressed?.Invoke(this, holdingObj);
            }
        }

        /// <summary>Grabs based on raycast and grab input data</summary>
        public virtual void Grab(RaycastHit hit, Grabbable grab, GrabType grabType = GrabType.InstantGrab) {
            bool objectFree = grab.body.isKinematic != true && grab.body.constraints == RigidbodyConstraints.None;
            if(!grabbing && holdingObj == null && this.CanGrab(grab) && objectFree) {
                grabRoutine = StartCoroutine(GrabObject(hit, grab, grabType));
            }
        }

        /// <summary>Grab a given grabbable</summary>
        public virtual void TryGrab(Grabbable grab) {
            ForceGrab(grab);
        }


        /// <summary>Alwyas grab a given grabbable, only works if grab is possible will automaticlly Instantiate a new copy of the given grabbable if using a prefab reference</summary>
        public virtual void ForceGrab(Grabbable grab, bool createCopy = false) {
            if(createCopy || !grab.gameObject.scene.IsValid())
                grab = Instantiate(grab);

            RaycastHit closestHit = new RaycastHit();
            closestHit.distance = float.MaxValue;
            if(!grabbing && holdingObj == null && this.CanGrab(grab)) {
                var rayPosition = palmTransform.position;
                Ray ray = new Ray();
                RaycastHit hit;
                ray.origin = rayPosition;
                foreach(var collider in grab.grabColliders) {
                    Vector3 closestPoint = collider.ClosestPoint(palmTransform.transform.position);
                    ray.direction = closestPoint - palmTransform.position;
                    ray.direction = ray.direction.normalized;
                    if(ray.direction == Vector3.zero) {
                        ray.direction = collider.bounds.center - palmTransform.position;
                    }
                    if(collider.Raycast(ray, out hit, 10000)) {
                        if(hit.distance < closestHit.distance)
                            closestHit = hit;
                    }
                    //Sometimes the first raycast fails because the closest point is perfectly parallel to the collider, moving the origin towards the center helps prevent this issue
                    else {
                        ray.origin = Vector3.MoveTowards(ray.origin, collider.bounds.center, 0.001f);
                        if(collider.Raycast(ray, out hit, 10000) && hit.distance < closestHit.distance)
                            closestHit = hit;
                    }
                }
            }

            if(closestHit.distance != float.MaxValue) {
                Grab(closestHit, grab, GrabType.InstantGrab);
            }
        }

        /// <summary>Function for controller trigger unpressed (by default this is called by the hand controller link component)</summary>
        public virtual void Release() {
            OnTriggerRelease?.Invoke(this, null);
            foreach(var triggerArea in triggerEventAreas) {
                triggerArea.Release(this);
            }

            if(holdingObj && !holdingObj.wasForceReleased && holdingObj.CanGetComponent<GrabLock>(out _))
                return;

            if(holdingObj != null) {
                OnBeforeReleased?.Invoke(this, holdingObj);
                //Do the holding object calls and sets
                holdingObj?.OnRelease(this);
                OnHeldConnectionBreak?.Invoke(this, holdingObj);
                OnReleased?.Invoke(this, holdingObj);
                ignoreMoveFrame = true;
            }

            BreakGrabConnection();
        }

        /// <summary>This will force release the hand without throwing or calling OnRelease\n like losing grip on something instead of throwing</summary>
        public virtual void ForceReleaseGrab() {
            if(holdingObj != null) {
                OnForcedRelease?.Invoke(this, holdingObj);
                holdingObj?.ForceHandRelease(this);
            }
        }

        /// <summary>Old function left for backward compatability -> Will release grablocks, recommend using ForceReleaseGrab() instead</summary>
        public virtual void ReleaseGrabLock() {
            ForceReleaseGrab();
        }

        /// <summary>Event for controller grip (by default this is called by the hand controller link component)</summary>
        public virtual void Squeeze() {
            OnSqueezed?.Invoke(this, holdingObj);
            holdingObj?.OnSqueeze(this);

            foreach(var triggerArea in triggerEventAreas) {
                triggerArea.Squeeze(this);
            }
            squeezing = true;
        }

        /// <summary>Returns the squeeze value from zero to one, (by default this is set by the hand controller link)</summary>
        public virtual float GetGripAxis() {
            return gripAxis;
        }

        public float GetSqueezeAxis() {
            return squeezeAxis;
        }

        /// <summary>Event for controller ungrip</summary>
        public virtual void Unsqueeze() {
            squeezing = false;
            OnUnsqueezed?.Invoke(this, holdingObj);
            holdingObj?.OnUnsqueeze(this);

            foreach(var triggerArea in triggerEventAreas) {
                triggerArea.Unsqueeze(this);
            }
        }

        /// <summary>Breaks the grab event</summary>
        public virtual void BreakGrabConnection(bool callEvent = true) {

            if(holdingObj != null) {
                if(squeezing)
                    holdingObj.OnUnsqueeze(this);

                if(grabbing) {
                    if (holdingObj.body != null){
                        holdingObj.body.velocity = Vector3.zero;
                        holdingObj.body.angularVelocity = Vector3.zero;
                    }
                }

                foreach(var finger in fingers) {
                    finger.SetCurrentFingerBend(finger.GetLastHitBend());
                }

                if(holdingObj.ignoreReleaseTime == 0) {
                    transform.position = holdingObj.transform.InverseTransformPoint(startHandLocalGrabPosition);
                    body.position = transform.position;
                }

                holdingObj.BreakHandConnection(this);
                lastHoldingObj = holdingObj;
                holdingObj = null;
            }

            velocityTracker.Disable(throwVelocityExpireTime);
            grabPose = null;
            lookingAtObj = null;
            grabPositionOffset = Vector3.zero;
            grabRotationOffset = Quaternion.identity;
            grabRoutine = null;

            if(heldJoint != null) {
                Destroy(heldJoint);
                heldJoint = null;
            }
        }



        /// <summary>Creates the grab connection at the current position of the hand and given grabbable</summary>
        public virtual void CreateGrabConnection(Grabbable grab, bool executeGrabEvents = false) {
            CreateGrabConnection(grab, transform.position, transform.rotation, grab.transform.position, grab.transform.rotation);
        }

        /// <summary>Creates the grab connection</summary>
        public virtual void CreateGrabConnection(Grabbable grab, Vector3 handPos, Quaternion handRot, Vector3 grabPos, Quaternion grabRot, bool executeGrabEvents = false, bool ignorePoses = false) {

            if(executeGrabEvents) {
                OnBeforeGrabbed?.Invoke(this, grab);
                grab.OnBeforeGrab(this);
            }

            transform.position = handPos;
            body.position = handPos;
            transform.rotation = handRot;
            body.rotation = handRot;

            grab.transform.position = grabPos;
            grab.body.position = grabPos;
            grab.transform.rotation = grabRot;
            grab.body.rotation = grabRot;

            handGrabPoint.parent = grab.transform;
            handGrabPoint.transform.position = handPos;
            handGrabPoint.transform.rotation = handRot;


            holdingObj = grab;

            localGrabbablePoint.transform.position = holdingObj.body.transform.position;
            localGrabbablePoint.transform.rotation = holdingObj.body.transform.rotation;

            if(!(holdingObj.grabType == HandGrabType.GrabbableToHand) && !(grabType == GrabType.GrabbableToHand)) {
                grabPositionOffset = transform.position - follow.transform.position;
                grabRotationOffset = Quaternion.Inverse(follow.transform.rotation) * transform.rotation;
            }

            //If it's a predetermined Pose
            if(!ignorePoses && holdingObj.GetSavedPose(out var poseCombiner)) {
                if(poseCombiner.CanSetPose(this, holdingObj)) {
                    grabPose = poseCombiner.GetClosestPose(this, holdingObj);
                    grabPose.SetHandPose(this);
                }
            }

            if(executeGrabEvents) {
                OnGrabbed?.Invoke(this, holdingObj);
                holdingObj.OnGrab(this);
            }

            CreateJoint(holdingObj, holdingObj.jointBreakForce, float.PositiveInfinity);
        }

        public virtual void OnJointBreak(float breakForce) {
            if(heldJoint != null) {
                Destroy(heldJoint);
                heldJoint = null;
            }
            if(holdingObj != null) {
                holdingObj.body.velocity /= 100f;
                holdingObj.body.angularVelocity /= 100f;
                OnGrabJointBreak?.Invoke(this, holdingObj);
                holdingObj?.OnHandJointBreak(this);
            }
        }


        //=============== HIGHLIGHT AND LOOK ASSIST ===================
        //=============================================================
        //=============================================================


        Collider[] highlightCollidersNonAlloc = new Collider[128];
        List<Grabbable> foundGrabbables = new List<Grabbable>();

        /// <summary>Manages the highlighting for grabbables</summary>
        public virtual void UpdateHighlight() {

            if(usingHighlight && highlightLayers != 0 && holdingObj == null && !IsGrabbing()) {
                int grabbingLayer = LayerMask.NameToLayer(grabbingLayerName);
                int gabbingMask = LayerMask.GetMask(grabbingLayerName);
                int overlapCount = Physics.OverlapSphereNonAlloc(palmTransform.position + palmTransform.forward * reachDistance / 2f, reachDistance, highlightCollidersNonAlloc, highlightLayers);
                foundGrabbables.Clear();

                Grabbable grab;
                for(int i = 0; i < overlapCount; i++) {
                    if(highlightCollidersNonAlloc[i].gameObject.HasGrabbable(out grab)) {
                        grab.SetLayerRecursive(grabbingLayer);
                        foundGrabbables.Add(grab);
                    }
                }

                if(foundGrabbables.Count > 0) {
                    Vector3 dir = HandClosestHit(out highlightHit, out Grabbable newLookingAtObj, reachDistance, ~handLayers);

                    //Zero means it didn't hit
                    if(dir != Vector3.zero && (newLookingAtObj != null && newLookingAtObj.CanGrab(this))) {
                        //Changes look target
                        if(newLookingAtObj != lookingAtObj) {
                            //Unhighlights current target if found
                            if(lookingAtObj != null) {
                                OnStopHighlight?.Invoke(this, lookingAtObj);
                                lookingAtObj.Unhighlight(this);
                            }

                            lookingAtObj = newLookingAtObj;

                            //Highlights new target if found
                            OnHighlight?.Invoke(this, lookingAtObj);
                            lookingAtObj.Highlight(this);
                        }
                    }
                    //If it was looking at something but now it's not there anymore
                    else if(newLookingAtObj == null && lookingAtObj != null) {
                        //Just in case the object your hand is looking at is destroyed
                        OnStopHighlight?.Invoke(this, lookingAtObj);
                        lookingAtObj.Unhighlight(this);

                        lookingAtObj = null;
                    }

                    for(int i = 0; i < foundGrabbables.Count; i++) {
                        foundGrabbables[i].ResetOriginalLayers();
                    }
                }
                else if(lookingAtObj != null) {
                    //Just in case the object your hand is looking at is destroyed
                    OnStopHighlight?.Invoke(this, lookingAtObj);
                    lookingAtObj.Unhighlight(this);

                    lookingAtObj = null;
                }
            }
        }

        /// <summary>Returns the closest raycast hit from the hand's highlighting system, if no highlight, returns blank raycasthit</summary>
        public RaycastHit GetHighlightHit() {
            highlightHit.point = handGrabPoint.position;
            highlightHit.normal = handGrabPoint.up;
            return highlightHit; 
        }




        //======================== GETTERS AND SETTERS ====================
        //=================================================================
        //=================================================================

        /// <summary>Takes a raycasthit and grabbable and automatically poses the hand</summary>
        public void AutoPose(RaycastHit hit, Grabbable grabbable) {
            var grabbingLayer = LayerMask.NameToLayer(Hand.grabbingLayerName);
            grabbable.SetLayerRecursive(grabbingLayer);

            Vector3 palmLocalPos = palmTransform.localPosition;
            Quaternion palmLocalRot = palmTransform.localRotation;

            for(int i = 0; i < 10; i++)
                Calculate();

            void Calculate() {
                Align();

                var grabDir = hit.point - palmTransform.position;
                transform.position += grabDir;
                body.position = transform.position;

                palmCollider.enabled = true;
                if(Physics.ComputePenetration(hit.collider, hit.collider.transform.position, hit.collider.transform.rotation,
                    palmCollider, palmCollider.transform.position, palmCollider.transform.rotation, out var dir, out var dist)) {
                    transform.position -= dir * dist / 2f;
                    body.position = transform.position;
                }
                palmCollider.enabled = false;

                Align();

                transform.position -= palmTransform.forward * grabDir.magnitude / 3f;
                body.position = transform.position;
            }

            void Align() {
                palmChild.position = transform.position;
                palmChild.rotation = transform.rotation;

                palmTransform.LookAt(hit.point, palmTransform.up);

                transform.position = palmChild.position;
                transform.rotation = palmChild.rotation;

                palmTransform.localPosition = palmLocalPos;
                palmTransform.localRotation = palmLocalRot;
            }

            foreach(var finger in fingers)
                finger.BendFingerUntilHit(fingerBendSteps, LayerMask.GetMask(Hand.grabbingLayerName));

            grabbable.ResetOriginalLayers();
        }


        /// <summary>Returns the current hand pose, ignoring what is being held - (IF SAVING A HELD POSE USE GetHeldPose())</summary>
        public HandPoseData GetHandPose() {
            return new HandPoseData(this);
        }

        /// <summary>Returns the hand pose relative to what it's holding</summary>
        public HandPoseData GetHeldPose() {
            if(holdingObj)
                return new HandPoseData(this, holdingObj);
            return new HandPoseData(this);
        }

        /// <summary>Sets the hand pose and connects the grabbable</summary>
        public virtual void SetHeldPose(HandPoseData pose, Grabbable grabbable, bool createJoint = true) {
            //Set Pose
            pose.SetPose(this, grabbable.transform);

            if(createJoint) {
                holdingObj = grabbable;
                OnBeforeGrabbed?.Invoke(this, holdingObj);
                holdingObj.body.transform.position = transform.position;

                CreateJoint(holdingObj, holdingObj.jointBreakForce, float.PositiveInfinity);

                handGrabPoint.parent = holdingObj.transform;
                handGrabPoint.transform.position = transform.position;
                handGrabPoint.transform.rotation = transform.rotation;

                OnGrabbed?.Invoke(this, holdingObj);
                holdingObj.OnGrab(this);

                SetHandLocation(moveTo.position, moveTo.rotation);

            }

        }

        /// <summary>Sets the hand pose</summary>
        public void SetHandPose(HandPoseData pose) {
            pose.SetPose(this, null);
        }

        /// <summary>Sets the hand pose</summary>
        public void SetHandPose(GrabbablePose pose) {
            pose.GetHandPoseData(this).SetPose(this, null);
        }

        /// <summary>Takes a new pose and an amount of time and poses the hand</summary>
        public void UpdatePose(HandPoseData pose, float time) {
            if(handAnimateRoutine != null)
                StopCoroutine(handAnimateRoutine);
            if(gameObject.activeInHierarchy)
                handAnimateRoutine = StartCoroutine(LerpHandPose(GetHandPose(), pose, time));
        }

        /// <summary>If the grabbable has a GrabbablePose, this will return it. Null if none</summary>
        public bool GetGrabPose(Grabbable grabbable, out GrabbablePose grabPose) {
            grabPose = null;
            if(grabbable.GetSavedPose(out var poseCombiner) && poseCombiner.CanSetPose(this, grabbable)) {
                grabPose = poseCombiner.GetClosestPose(this, grabbable);
                return true;
            }

            return false;
        }

        /// <summary>If the held grabbable has a GrabbablePose, this will return it. Null if none</summary>
        public bool GetCurrentHeldGrabPose(Transform from, Grabbable grabbable, out GrabbablePose grabPose, out Transform relativeTo) {
            if(grabbable.GetSavedPose(out var poseCombiner) && poseCombiner.CanSetPose(this, grabbable)) {
                grabPose = poseCombiner.GetClosestPose(this, grabbable);
                relativeTo = grabbable.transform;
                return true;
            }
            if(grabbable.GetSavedPose(out var poseCombiner1) && poseCombiner1.CanSetPose(this, grabbable)) {
                grabPose = poseCombiner1.GetClosestPose(this, grabbable);
                relativeTo = from;
                return true;
            }

            grabPose = null;
            relativeTo = from;
            return false;
        }

        /// <summary>Returns the current held object - null if empty (Same as GetHeld())</summary>
        public Grabbable GetHeldGrabbable() {
            return holdingObj;
        }

        /// <summary>Returns the current held object - null if empty (Same as GetHeldGrabbable())</summary>
        public Grabbable GetHeld() {
            return holdingObj;
        }

        /// <summary>Returns true if squeezing has been triggered</summary>
        public bool IsSqueezing() {
            return squeezing;
        }



        //========================= HELPER FUNCTIONS ======================
        //=================================================================
        //=================================================================

        /// <summary>Resets the grab offset created on grab for a smoother hand return</summary>
        public void ResetGrabOffset() {

            grabPositionOffset = transform.position - follow.transform.position;
            grabRotationOffset = Quaternion.Inverse(follow.transform.rotation) * transform.rotation;
        }

        /// <summary>Sets the hands grip 0 is open 1 is closed</summary>
        public void SetGrip(float grip, float squeeze) {
            gripAxis = grip;
            squeezeAxis = squeeze;
        }

        [ContextMenu("Set Pose - Relax Hand")]
        public void RelaxHand() {
            foreach(var finger in fingers)
                finger.SetFingerBend(gripOffset);
        }

        [ContextMenu("Set Pose - Open Hand")]
        public void OpenHand() {
            foreach(var finger in fingers)
                finger.SetFingerBend(0);
        }

        [ContextMenu("Set Pose - Close Hand")]
        public void CloseHand() {
            foreach(var finger in fingers)
                finger.SetFingerBend(1);
        }

        [ContextMenu("Bend Fingers Until Hit")]
        /// <summary>Bends each finger until they hit</summary>
        public void ProceduralFingerBend() {
            ProceduralFingerBend(~LayerMask.GetMask(rightHandLayerName, leftHandLayerName));
        }

        /// <summary>Bends each finger until they hit</summary>
        public void ProceduralFingerBend(int layermask) {
            foreach(var finger in fingers) {
                finger.BendFingerUntilHit(fingerBendSteps, layermask);
            }
        }

        /// <summary>Bends each finger until they hit</summary>
        public void ProceduralFingerBend(RaycastHit hit) {
            foreach(var finger in fingers) {
                finger.BendFingerUntilHit(fingerBendSteps, hit.transform.gameObject.layer);
            }
        }

        /// <summary>Plays haptic vibration on the hand controller if supported by controller link</summary>
        public void PlayHapticVibration() {
            PlayHapticVibration(0.05f, 0.5f);
        }

        /// <summary>Plays haptic vibration on the hand controller if supported by controller link</summary>
        public void PlayHapticVibration(float duration) {
            PlayHapticVibration(duration, 0.5f);
        }

        /// <summary>Plays haptic vibration on the hand controller if supported by controller link</summary>
        public void PlayHapticVibration(float duration, float amp = 0.5f) {
            if(left)
                HandControllerLink.handLeft?.TryHapticImpulse(duration, amp);
            else
                HandControllerLink.handRight?.TryHapticImpulse(duration, amp);
        }


        //========================= SAVING FUNCTIONS ======================
        //=================================================================
        //=================================================================
        public Hand copyFromHand;
            
        [Button("Copy Pose"), ContextMenu("COPY POSE")]
        public void CopyPose()
        {
            if (copyFromHand != null)
            {
                if (copyFromHand.fingers.Length != fingers.Length)
                {
                    Debug.LogError("Cannot copy pose because hand reference does not have the same number of fingers attached as this hand");

                }
                else
                {
                    for (int i = 0; i < copyFromHand.fingers.Length; i++)
                    {
#if UNITY_EDITOR
                        EditorUtility.SetDirty(fingers[i]);
#endif
                        fingers[i].CopyPose(copyFromHand.fingers[i]);
                    }
                    Debug.Log("Auto Hand: Copied Hand Pose!");
                }

            }
            else
            {
                Debug.LogError("Cannot copy pose because hand reference to copy from is not set");
            }
        }


        [Button("Save Open Pose"), ContextMenu("SAVE OPEN")]
        public void SaveOpenPose() {
            foreach(var finger in fingers) {
#if UNITY_EDITOR
                EditorUtility.SetDirty(finger);
#endif
                finger.SetMinPose();
            }
            Debug.Log("Auto Hand: Saved Open Hand Pose!");
        }


        [Button("Save Closed Pose"), ContextMenu("SAVE CLOSED")]
        public void SaveClosedPose() {
            foreach(var finger in fingers) {
#if UNITY_EDITOR
                EditorUtility.SetDirty(finger);
#endif
                finger.SetMaxPose();
            }
            Debug.Log("Auto Hand: Saved Closed Hand Pose!");
        }



        #region INTERNAL FUNCTIONS

        //======================= INTERNAL FUNCTIONS ======================
        //=================================================================
        //=================================================================


        protected virtual void OnCollisionFirstEnter(GameObject collision) {
            if(collision.CanGetComponent(out HandTouchEvent touchEvent)) {
                touchEvent.Touch(this);
            }
        }

        protected virtual void OnCollisionLastExit(GameObject collision) {
            if(collision.CanGetComponent(out HandTouchEvent touchEvent))
                touchEvent.Untouch(this);
        }

        protected virtual void OnTriggerFirstEnter(GameObject other) {
            CheckEnterPoseArea(other);
            if(other.CanGetComponent(out HandTriggerAreaEvents area)) {
                triggerEventAreas.Add(area);
                area.Enter(this);
            }
        }

        protected virtual void OnTriggerLastExit(GameObject other) {
            CheckExitPoseArea(other);
            if(other.CanGetComponent(out HandTriggerAreaEvents area)) {
                triggerEventAreas.Remove(area);
                area.Exit(this);
            }
        }

        //Highlighting doesn't need to be called every update, it can be called every 4th update without causing any noticable differrences 
        IEnumerator HighlightUpdate(float timestep) {
            //This will smooth out the highlight calls to help prevent lag spikes
            if(left)
                yield return new WaitForSecondsRealtime(timestep / 2);

            while(true) {
                if(usingHighlight) {
                    UpdateHighlight();
                }
                yield return new WaitForSecondsRealtime(timestep);
            }
        }

        Vector3 startHandLocalGrabPosition;
        /// <summary>Takes a hit from a grabbable object and moves the hand towards that point, then calculates ideal hand shape</summary>
        protected IEnumerator GrabObject(RaycastHit hit, Grabbable grab, GrabType grabType) {
            /////////////////////////
            ////Initialize values////
            /////////////////////////
            

            if(!CanGrab(grab))
                yield break;

            handGrabPoint.parent = hit.collider.transform;
            handGrabPoint.position = hit.point;
            handGrabPoint.up = hit.normal;

            while(grab.beingGrabbed)
                yield return new WaitForEndOfFrame();

            grab.beforeGrabFrame = true;
            var startHandPosition = transform.position;
            var startHandRotation = transform.rotation;
            var startGrabbablePosition = grab.transform.position;
            var startGrabbableRotation = grab.transform.rotation;
            if(grab.body != null) {
                startGrabbablePosition = grab.body.transform.position;
                startGrabbableRotation = grab.body.transform.rotation;
            }

            grabbing = true;
            grab.beforeGrabFrame = false;

            hit.point = handGrabPoint.position;
            hit.normal = handGrabPoint.up;

            CancelPose();
            ClearPoseArea();

            grabPose = null;
            lookingAtObj = null;
            holdingObj = grab;

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            OnBeforeGrabbed?.Invoke(this, holdingObj);
            holdingObj.OnBeforeGrab(this);

            var instantGrab = holdingObj.instantGrab || grabType == GrabType.InstantGrab;
            var startHoldingObj = holdingObj;
            startGrabDist = Vector3.Distance(palmTransform.position, handGrabPoint.position);
            startHandLocalGrabPosition = holdingObj.transform.InverseTransformPoint(transform.position);

            handGrabPoint.parent = hit.collider.transform;
            handGrabPoint.position = hit.point;
            handGrabPoint.up = hit.normal;

            if(holdingObj == null) {
                CancelGrab();
                yield break;
            }

            if(instantGrab)
                holdingObj.ActivateRigidbody();

            /////////////////
            ////Sets Pose////
            /////////////////
            
            HandPoseData startGrabPose;
            if(GetGrabPose(holdingObj, out var tempGrabPose)) {
                startGrabPose = new HandPoseData(this, tempGrabPose.transform);
                grabPose = tempGrabPose;
                grabPose.SetHandPose(this);
            }
            else {
                startGrabPose = new HandPoseData(this, holdingObj.transform);
                transform.position -= palmTransform.forward * 0.08f;
                body.position = transform.position;
                hit.point = handGrabPoint.position;
                hit.normal = handGrabPoint.up;
                AutoPose(hit, holdingObj);
            }
            HandPoseData postGrabPose = grabPose == null ? new HandPoseData(this, holdingObj.transform) : grabPose.GetHandPoseData(this);


            if(grab.body != null) {
                localGrabbablePoint.position = grab.body.transform.position;
                localGrabbablePoint.rotation = grab.body.transform.rotation;
            }
            else {
                localGrabbablePoint.position = grab.transform.position;
                localGrabbablePoint.rotation = grab.transform.rotation;
            }


            //////////////////////////
            ////Grabbing Animation////
            //////////////////////////


            //Instant Grabbing
            if(instantGrab) {
                if(grabPose != null)
                    grabPose.SetHandPose(this);

                //Hand Swap - One Handed Items
                if(holdingObj.singleHandOnly && holdingObj.HeldCount(false, false, false) > 0) {
                    holdingObj.ForceHandRelease(holdingObj.GetHeldBy()[0]);
                    if(holdingObj.body != null) {
                        holdingObj.body.velocity = Vector3.zero;
                        holdingObj.body.angularVelocity = Vector3.zero;
                    }
                }
            }
            //Smooth Grabbing
            else {
                transform.position = startHandPosition;
                transform.rotation = startHandRotation;
                body.position = startHandPosition;
                body.rotation = startHandRotation;

                var adjustedGrabTime = GetGrabTime();
                instantGrab = instantGrab || adjustedGrabTime == 0;
                Transform grabTarget = grabPose != null ? grabPose.transform : holdingObj.transform;

                foreach(var finger in fingers)
                    finger.SetFingerBend(gripOffset + Mathf.Clamp01(finger.GetCurrentBend() / 4f));

                openHandPose = GetHandPose();



                /////////////////////////
                ////Hand To Grabbable////
                /////////////////////////
                if(grabType == GrabType.HandToGrabbable || (grabType == GrabType.GrabbableToHand && (holdingObj.HeldCount() > 0 || !holdingObj.parentOnGrab))) {
                    //Loop until the hand is at the object
                    for(float i = 0; i < adjustedGrabTime; i += Time.deltaTime) {
                        if(holdingObj != null) {
                            //Will move the hand faster if the controller or object is moving
                            var deltaDist = Vector3.Distance(follow.position, lastFrameFollowPos);
                            i += deltaDist * Time.deltaTime * 120f;
                            i += holdingObj.GetVelocity().magnitude * Time.deltaTime * 10;

                            if(i < adjustedGrabTime) {
                                var point = Mathf.Clamp01(i / adjustedGrabTime);
                                var handOpenTime = 0.5f;
                                var handTargetTime = 1.5f;

                                if(point < handOpenTime)
                                    HandPoseData.LerpPose(startGrabPose, openHandPose, grabCurve.Evaluate(point * 1f / handOpenTime)).SetFingerPose(this, grabTarget);
                                else
                                    HandPoseData.LerpPose(openHandPose, postGrabPose, grabCurve.Evaluate((point - handOpenTime) * (1f / (1 - handOpenTime)))).SetFingerPose(this, grabTarget);

                                HandPoseData.LerpPose(startGrabPose, postGrabPose, point * handTargetTime).SetPosition(this, grabTarget);
                                body.position = transform.position;
                                body.rotation = transform.rotation;

                                if(holdingObj.body != null)
                                    holdingObj.body.angularVelocity *= 0.5f;
                                yield return new WaitForEndOfFrame();
                            }
                        }
                    }

                    //Hand Swap - One Handed Items
                    if(holdingObj != null && holdingObj.singleHandOnly && holdingObj.GetHeldBy().Count > 0)
                        holdingObj.ForceHandRelease(holdingObj.GetHeldBy()[0]);
                }



                /////////////////////////
                ////Grabbable to Hand////
                /////////////////////////
                else if(grabType == GrabType.GrabbableToHand) {
                    holdingObj.ActivateRigidbody();

                    //Hand Swap - One Handed Items
                    if(holdingObj.singleHandOnly && holdingObj.HeldCount() > 0)
                        holdingObj.ForceHandRelease(holdingObj.GetHeldBy()[0]);

                    //Disable grabbable while item is moving towards hand
                    bool useGravity = holdingObj.body.useGravity;
                    holdingObj.body.useGravity = false;
                    
                    //Loop until the object is at the hand
                    for(float i = 0; i < adjustedGrabTime; i += Time.deltaTime) {
                        if(holdingObj != null) {
                            //Will move the hand faster if the controller or object is moving
                            var deltaDist = Vector3.Distance(follow.position, lastFrameFollowPos); 
                            i += deltaDist * Time.deltaTime * 120f;

                            var point = Mathf.Clamp01(i / adjustedGrabTime);
                            var handOpenTime = 0.5f;

                            if(point < handOpenTime)
                                HandPoseData.LerpPose(startGrabPose, openHandPose, grabCurve.Evaluate(point / handOpenTime)).SetFingerPose(this, grabTarget);
                            else
                                HandPoseData.LerpPose(openHandPose, postGrabPose, grabCurve.Evaluate((point - handOpenTime) * (1f / (1 - handOpenTime)))).SetFingerPose(this, grabTarget);


                            if(holdingObj.body != null) {
                                holdingObj.body.transform.position = Vector3.Lerp(startGrabbablePosition, localGrabbablePoint.position, grabCurve.Evaluate(point / handOpenTime));
                                holdingObj.body.transform.rotation = Quaternion.Lerp(startGrabbableRotation, localGrabbablePoint.rotation, grabCurve.Evaluate(point / handOpenTime));
                                holdingObj.body.position = holdingObj.body.transform.position;
                                holdingObj.body.rotation = holdingObj.body.transform.rotation;
                                holdingObj.body.velocity = Vector3.zero;
                                holdingObj.body.angularVelocity = Vector3.zero;
                            }
                            else {
                                holdingObj.transform.position = Vector3.Lerp(startGrabbablePosition, localGrabbablePoint.position, grabCurve.Evaluate(point / handOpenTime));
                                holdingObj.transform.rotation = Quaternion.Lerp(startGrabbableRotation, localGrabbablePoint.rotation, grabCurve.Evaluate(point / handOpenTime));
                            }

                            //SetMoveTo();
                            MoveTo(Time.fixedDeltaTime);
                            TorqueTo(Time.fixedDeltaTime);

                            yield return new WaitForEndOfFrame();

                        }
                    }

                    //Reset Gravity
                    if(holdingObj != null && holdingObj.body != null)
                        holdingObj.body.useGravity = useGravity;
                    else if(startHoldingObj.body != null)
                        startHoldingObj.body.useGravity = useGravity;
                }

                //Ensure final pose
                if(holdingObj != null)
                    postGrabPose.SetPose(this, grabTarget);
            }

            if(holdingObj == null) {
                CancelGrab();
                yield break;
            }




            //////////////////////////////////
            ////Finalize Values and Events////
            //////////////////////////////////

            handGrabPoint.transform.position = transform.position;
            handGrabPoint.transform.rotation = transform.rotation;

            holdingObj.ActivateRigidbody();
            localGrabbablePoint.position = holdingObj.body.transform.position;
            localGrabbablePoint.rotation = holdingObj.body.transform.rotation;

            CreateJoint(holdingObj, holdingObj.jointBreakForce , float.PositiveInfinity);

            OnGrabbed?.Invoke(this, holdingObj);
            holdingObj.OnGrab(this);

            grabTime = Time.time;
            grabReturnRotationDistance = Quaternion.Inverse(moveTo.transform.rotation) * transform.rotation;
            grabReturnPositionDistance = transform.position - moveTo.transform.position;


            if(!instantGrab || !holdingObj.parentOnGrab) {
                grabPositionOffset = transform.position - moveTo.transform.position;
                grabRotationOffset = Quaternion.Inverse(moveTo.transform.rotation) * transform.rotation;
            }


            if(holdingObj == null) {
                CancelGrab();
                yield break;
            }

            void CancelGrab() {
                BreakGrabConnection();
                if(startHoldingObj)
                {
                    if (startHoldingObj.body != null)
                    {
                        startHoldingObj.body.velocity = Vector3.zero;
                        startHoldingObj.body.angularVelocity = Vector3.zero;
                    }
                    startHoldingObj.beingGrabbed = false;
                }
                grabbing = false;
                grabRoutine = null;
            }

            grabbing = false;
            startHoldingObj.beingGrabbed = false;
            grabRoutine = null;

            if(instantGrab && holdingObj.parentOnGrab) {
                SetMoveTo();
                SetHandLocation(moveTo.position, moveTo.rotation);
            }
        }


        /// <summary>Ensures any pose being made is canceled</summary>
        protected void CancelPose() {
            if(handAnimateRoutine != null)
                StopCoroutine(handAnimateRoutine);
            handAnimateRoutine = null;
            grabPose = null;
        }


        /// <summary>Not exactly lerped, uses non-linear sqrt function because it looked better -- planning animation curves options soon</summary>
        protected virtual IEnumerator LerpHandPose(HandPoseData fromPose, HandPoseData toPose, float totalTime) {
            float timePassed = 0;
            while(timePassed < totalTime) {
                SetHandPose(HandPoseData.LerpPose(fromPose, toPose, Mathf.Pow(timePassed / totalTime, 0.5f)));
                yield return new WaitForEndOfFrame();
                timePassed += Time.deltaTime;
            }
            SetHandPose(HandPoseData.LerpPose(fromPose, toPose, 1));
            handAnimateRoutine = null;
        }


        /// <summary>Checks and manages if any of the hands colliders enter a pose area</summary>
        protected virtual void CheckEnterPoseArea(GameObject other) {
            if(holdingObj || !usingPoseAreas || !other.activeInHierarchy)
                return;

            if(other && other.CanGetComponent(out HandPoseArea tempPose)) {
                for(int i = 0; i < tempPose.poseAreas.Length; i++) {
                    if(tempPose.poseIndex == poseIndex) {
                        if(tempPose.HasPose(left) && (handPoseArea == null || handPoseArea != tempPose)) {
                            if(handPoseArea == null)
                                preHandPoseAreaPose = GetHandPose();

                            else if(handPoseArea != null)
                                TryRemoveHandPoseArea(handPoseArea);

                            handPoseArea = tempPose;
                            handPoseArea?.OnHandEnter?.Invoke(this);
                            if(holdingObj == null)
                                UpdatePose(handPoseArea.GetHandPoseData(left), handPoseArea.transitionTime);
                        }
                        break;
                    }
                }
            }
        }


        /// <summary>Checks if manages any of the hands colliders exit a pose area</summary>
        protected virtual void CheckExitPoseArea(GameObject other) {
            if(!usingPoseAreas || !other.gameObject.activeInHierarchy)
                return;

            if(other.CanGetComponent(out HandPoseArea poseArea))
                TryRemoveHandPoseArea(poseArea);
        }


        internal void TryRemoveHandPoseArea(HandPoseArea poseArea) {
            if(handPoseArea != null && handPoseArea.gameObject.Equals(poseArea.gameObject)) {
                try
                {
                    if (holdingObj == null)
                    {
                        if (handPoseArea != null)
                            UpdatePose(preHandPoseAreaPose, handPoseArea.transitionTime);
                        handPoseArea?.OnHandExit?.Invoke(this);
                        handPoseArea = null; 
                    }
                    else if (holdingObj != null)
                    {
                        handPoseArea?.OnHandExit?.Invoke(this);
                        handPoseArea = null;
                    }
                }
                
                catch(MissingReferenceException)
                {
                    handPoseArea = null;
                    SetHandPose(preHandPoseAreaPose);
                }
            }
        }


        private void ClearPoseArea() {
            if(handPoseArea != null)
                handPoseArea.OnHandExit?.Invoke(this);
            handPoseArea = null;
        }


        internal virtual void RemoveHandTriggerArea(HandTriggerAreaEvents handTrigger) {
            handTrigger.Exit(this);
            triggerEventAreas.Remove(handTrigger);
        }

        #endregion


        void OnCollisionFirstEnterEvent(GameObject collision) { OnHandCollisionStart?.Invoke(this, collision); }
        void OnCollisionLastExitEvent(GameObject collision) { OnHandCollisionStop?.Invoke(this, collision); }
        void OnTriggerFirstEnterEvent(GameObject collision) { OnHandTriggerStart?.Invoke(this, collision); }
        void OnTriggeLastExitEvent(GameObject collision) { OnHandTriggerStop?.Invoke(this, collision); }


    }
}