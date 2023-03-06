using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    public enum HeldAnimationDriver {
        squeeze,
        grip,
        custom
    }

    public class GrabbablePoseAnimaion : MonoBehaviour {
        public AnimationCurve animationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Tooltip("Determines the default hand value to activate this pose while it's being held")]
        public HeldAnimationDriver animationDriver = HeldAnimationDriver.squeeze;

        [NaughtyAttributes.ShowIf("animatorDriver", HeldAnimationDriver.custom)]
        public float customValue;
        [Space]
        [Tooltip("The pose the hand will have by default")]
        public GrabbablePose fromPose;
        [Tooltip("The pose the hand will move to match given the animation driver value")]
        public GrabbablePose toPose;
        [Tooltip("Additional animations to run alongside the given driver value (good for things like a gun trigger that is separate from the hand but still needs to move with the hand during the animation)")]
        public AutoAnimation[] additionalAnimations;
        [Space]

        HandPoseData fromPoseData, toPoseData;
        int lastPosingHandsCount;


        public void Update() {
            var posingHandCount = fromPose.posingHands.Count + toPose.posingHands.Count;

            foreach(var hand in fromPose.posingHands)
                Animate(hand);
            foreach(var hand in toPose.posingHands)
                Animate(hand);

            if(lastPosingHandsCount != 0 && posingHandCount == 0)
                foreach(var autoAnim in additionalAnimations)
                    autoAnim.SetAnimation(0);

            lastPosingHandsCount = posingHandCount;

        }   

        public void Animate(Hand hand) {
            fromPoseData = fromPose.GetHandPoseData(hand);
            toPoseData = toPose.GetHandPoseData(hand);
            var animationValue = GetAnimationValue(hand);
            HandPoseData.LerpPose(fromPoseData, toPoseData, animationValue).SetPose(hand);
            foreach(var autoAnim in additionalAnimations)
                autoAnim.SetAnimation(animationCurve.Evaluate(animationValue));


            float GetAnimationValue(Hand hand1) {
                if(animationDriver == HeldAnimationDriver.squeeze)
                    return hand1.GetSqueezeAxis();
                else if(animationDriver == HeldAnimationDriver.grip)
                    return hand1.GetGripAxis();
                else if(animationDriver == HeldAnimationDriver.custom)
                    return customValue;

                return 0;
            }

        }



        public void Animate(Hand hand, float value) {
            fromPoseData = fromPose.GetHandPoseData(hand);
            toPoseData = toPose.GetHandPoseData(hand);
            HandPoseData.LerpPose(fromPoseData, toPoseData, value).SetPose(hand);
        }
    }

}