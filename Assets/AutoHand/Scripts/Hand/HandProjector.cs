using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    public class HandProjector : MonoBehaviour {
        [Header("References")]
        public Hand hand;
        [Tooltip("This should be a copy of the hand with the desired visual setup for your projection hand")]
        public Hand handProjection;
        [Tooltip("The Object(s) under your Hand that contain the MeshRenderer Component(s)")]
        public Transform[] handProjectionVisuals;

        [Tooltip("Smoothing speed, turning too high could cause jitters")]
        public float speed = 15f;

        [Tooltip("If true everything in the hand Vvisuals will be disabled/hidden when projection hand is showing")]
        public bool hideHand;
        [ShowIf("hideHand")]
        [Tooltip("The Object(s) under your main hand (not the projection hand) that contain the MeshRenderer Component(s)")]
        public Transform[] handVisuals;

        [Tooltip("Should the projection interpolate between the hand pose and the projected grab pose based on the grip input axis")]
        public bool useGrabTransition;
        [EnableIf("useGrabTransition")]
        [Tooltip("This offsets the grab transistion by this percent when active [0-1 range]")]
        public float grabTransitionOffset = 0;
        [EnableIf("useGrabTransition")]
        [Tooltip("This sets the position of the hand based on its [(gripAxis + grabTransitionOffset) * grabDistanceMultiplyer] -> gripAxis is set on the HandControllerLink component on the main hand")]
        public float grabDistanceMultiplyer = 2f;
        [Tooltip("This sets the pose of the hand based on its [(gripAxis + grabTransitionOffset) * grabDistanceMultiplyer] -> gripAxis is set on the HandControllerLink component on the main hand")]
        [EnableIf("useGrabTransition")]
        public float grabTransitionMultiplyer = 2f;
        [DisableIf("useGrabTransition")]
        [Tooltip("This offsets the highlight by this percent when active [0-1 range]")]
        public float grabPercent = 1f;


        [Header("Events")]
        public UnityHandGrabEvent OnStartProjection;
        public UnityHandGrabEvent OnEndProjection;




        HandPoseData lastProjectionPose;
        HandPoseData newProjectionPose;
        Vector3 lastProjectionPosition;
        Quaternion lastProjectionRotation;

        Grabbable target;
        float startMass = 0;
        float minGrabTime = 0;
        float currAmount;
        bool tryingGrab = false;

        void OnEnable() {
            if(handProjection.body == null)
                handProjection.body = handProjection.GetComponent<Rigidbody>();
            if(hand.body == null)
                hand.body = hand.GetComponent<Rigidbody>();
            handProjection.body.detectCollisions = false;
            handProjection.body.mass = 0;

            handProjection.enableMovement = false;
            handProjection.usingHighlight = false;
            handProjection.disableIK = true;

            handProjection.followPositionStrength = 0;
            handProjection.followRotationStrength = 0;
            handProjection.swayStrength = 0;
            handProjection.disableIK = true;
            handProjection.usingHighlight = false;
            handProjection.usingPoseAreas = false;
            startMass = hand.body.mass;
            minGrabTime = hand.minGrabTime;

            lastProjectionPosition = hand.transform.localPosition;
            lastProjectionRotation = hand.transform.localRotation;
            lastProjectionPose = hand.GetHandPose();

            hand.OnBeforeGrabbed += OnBeforeGrab;
            hand.OnGrabbed += OnGrab;
            hand.OnBeforeReleased += OnRelease;
            hand.OnTriggerGrab += OnTriggerGrab;
        }

        void OnDisable() {
            ShowProjection(false);
            hand.OnBeforeGrabbed -= OnBeforeGrab;
            hand.OnGrabbed -= OnGrab;
            hand.OnBeforeReleased -= OnRelease;
            hand.OnTriggerGrab -= OnTriggerGrab;
        }

        void OnTriggerGrab(Hand hand, Grabbable grab) {
            tryingGrab = true;
        }

        void OnBeforeGrab(Hand hand, Grabbable grab) {

            if(hideHand) {
                lastProjectionPose.SetFingerPose(hand);
                hand.transform.position = handProjection.transform.position;
                hand.transform.rotation = handProjection.transform.rotation;
                hand.body.position = hand.transform.position;
                hand.body.rotation = hand.transform.rotation;

                hand.minGrabTime = 0f;
            }

            ShowProjection(false);
        }

        void OnGrab(Hand hand, Grabbable grab) {

            if(useGrabTransition) {
                hand.minGrabTime = minGrabTime;
            }
        }

        void OnRelease(Hand hand, Grabbable grab) {
            lastProjectionPose = hand.GetHandPose();
            lastProjectionPose.SetFingerPose(handProjection);
            lastProjectionPosition = hand.transform.localPosition;
            lastProjectionRotation = hand.transform.localRotation;
            handProjection.transform.position = hand.transform.position;
            handProjection.transform.rotation = hand.transform.rotation;
            handProjection.body.position = handProjection.transform.position;
            handProjection.body.rotation = handProjection.transform.rotation;

            if(hideHand) {
                hand.minGrabTime = minGrabTime;
                for(int i = 0; i < handProjection.fingers.Length; i++)
                    handProjection.fingers[i].SetCurrentFingerBend(hand.fingers[i].GetLastHitBend());
            }
        }

        void LateUpdate() {
            if(tryingGrab && hand.GetTriggerAxis() < 0.35f)
                tryingGrab = false;
            

            SetTarget(hand.lookingAtObj);
            ShowProjection(IsProjectionActive());
        }


        void OnProjectionStart(Hand projectionHand, Grabbable target) {
            OnStartProjection?.Invoke(projectionHand, target);
        }


        void OnProjectionEnd(Hand projectionHand, Grabbable target) {
            OnEndProjection?.Invoke(projectionHand, target);
        }


        void ShowProjection(bool show) {
            for(int i = 0; i < handProjectionVisuals.Length; i++)
                handProjectionVisuals[i].gameObject.SetActive(show);

            if(hideHand) {
                for(int i = 0; i < handVisuals.Length; i++)
                    handVisuals[i].gameObject.SetActive(!show);
                if(show)
                    hand.body.mass = 0;
                else
                    hand.body.mass = startMass;
            }

            if(show) {
                var targetHit = hand.GetHighlightHit();
                if(targetHit.collider != null) {

                    if(!hand.CanGrab(target)) {
                        ShowProjection(false);
                        return;
                    }

                    var amount = useGrabTransition ? hand.grabCurve.Evaluate(hand.GetTriggerAxis() * grabTransitionMultiplyer + grabTransitionOffset) : grabPercent;
                    currAmount = Mathf.MoveTowards(currAmount, amount, Time.deltaTime * speed);
                    var newSpeed = Mathf.Lerp(speed, speed / 4f, hand.GetTriggerAxis());
                    if(hideHand)
                        hand.body.mass = Mathf.Lerp(startMass, 0, Mathf.Pow(amount * 2, 2));

                    //Do new pose
                    GrabbablePose grabPose;
                    handProjection.transform.localPosition = hand.transform.localPosition;
                    handProjection.transform.localRotation = hand.transform.localRotation;
                    if(handProjection.GetGrabPose(target, out grabPose)) {
                        grabPose.SetHandPose(handProjection, true);
                    }
                    else {
                        handProjection.transform.position -= handProjection.palmTransform.forward * 0.08f;
                        handProjection.body.position = handProjection.transform.position;
                        handProjection.AutoPose(targetHit, target);
                    }
                    newProjectionPose = handProjection.GetHandPose();
                    Vector3 targetPos;
                    Quaternion targetRot;

                    if(useGrabTransition && (target.grabType == HandGrabType.GrabbableToHand || (target.grabType == HandGrabType.Default && hand.grabType == GrabType.GrabbableToHand))) {
                        targetPos = hand.transform.localPosition;
                        targetRot = hand.transform.localRotation;
                    }
                    else {
                        targetPos = Vector3.Lerp(hand.transform.localPosition, handProjection.transform.localPosition, currAmount * grabDistanceMultiplyer);
                        targetRot = Quaternion.Lerp(hand.transform.localRotation, handProjection.transform.localRotation, currAmount * grabDistanceMultiplyer);
                    }


                    //Visual Adjustments
                    if(grabPose == null)
                        foreach(var finger in handProjection.fingers)
                            finger.SetFingerBend(Mathf.Clamp01(finger.GetLastHitBend() - 0.1f));
                    else {
                        foreach(var finger in handProjection.fingers)
                            finger.SetFingerBend(handProjection.gripOffset);
                    }


                    //Interpolate Fingers
                    var postGrabPose = HandPoseData.LerpPose(hand.GetHandPose(), newProjectionPose, Mathf.Clamp01(currAmount - 0.33f)*1.5f);
                    HandPoseData.LerpPose(lastProjectionPose, postGrabPose, speed * Time.deltaTime).SetFingerPose(handProjection);

                    if(hand.GetTriggerAxis() > 0.1f || !hideHand) {
                        //Interpolate Position
                        handProjection.transform.localPosition = Vector3.Lerp(lastProjectionPosition, targetPos, newSpeed * Time.deltaTime);
                        handProjection.transform.localRotation = Quaternion.Lerp(lastProjectionRotation, targetRot, newSpeed * Time.deltaTime);
                    }
                    else{
                        handProjection.transform.localPosition = hand.transform.localPosition;
                        handProjection.transform.localRotation = hand.transform.localRotation;
                        lastProjectionPose = hand.GetHandPose();
                        lastProjectionPose.SetFingerPose(handProjection);
                    }
                    handProjection.body.position = handProjection.transform.position;
                    handProjection.body.rotation = handProjection.transform.rotation;

                    lastProjectionPosition = handProjection.transform.localPosition;
                    lastProjectionRotation = handProjection.transform.localRotation;
                    lastProjectionPose = handProjection.GetHandPose();
                }
                else if(!hand.IsGrabbing()){
                    handProjection.transform.localPosition = hand.transform.localPosition;
                    handProjection.transform.localRotation = hand.transform.localRotation;
                    lastProjectionPosition = hand.transform.localPosition;
                    lastProjectionRotation = hand.transform.localRotation;
                    lastProjectionPose = hand.GetHandPose();
                    lastProjectionPose.SetFingerPose(handProjection);
                }
            }
            else if(useGrabTransition){
                handProjection.transform.localPosition = hand.transform.localPosition;
                handProjection.transform.localRotation = hand.transform.localRotation;
            }
        }

        void SetTarget(Grabbable newTarget) {
            if(newTarget != null && !hand.CanGrab(newTarget))
                newTarget = null;

            if(hand.holdingObj != null || newTarget == null) {
                if(target != null) {
                    OnProjectionEnd(handProjection, target);

                    lastProjectionPosition = hand.transform.localPosition;
                    lastProjectionRotation = hand.transform.localRotation;
                    handProjection.transform.position = hand.transform.position;
                    handProjection.transform.rotation = hand.transform.rotation;
                    handProjection.body.position = handProjection.transform.position;
                    handProjection.body.rotation = handProjection.transform.rotation;
                }

                target = null;
            }

            if(newTarget != target) {
                if(target != null)
                    OnProjectionEnd(handProjection, target);
                target = newTarget;
                OnProjectionStart(handProjection, target);
            }
        }



        bool IsProjectionActive() {
            return target != null && hand.holdingObj == null && !hand.IsGrabbing() && !tryingGrab;
        }
    }
}