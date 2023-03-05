using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    public delegate void HandEvent(Hand hand);

    public class HandTriggerAreaEvents : MonoBehaviour{
        [Header("Trigger Events Settings")]
        [Tooltip("Whether or not first hand to enter should take ownership and be the only one to call events")]
        public bool oneHanded = true;
        public HandType handType = HandType.both;
        [Tooltip("Whether or not to call the release event if exiting while grab event activated")]
        public bool exitTriggerRelease = true;
        [Tooltip("Whether or not to call the release event if exiting while grab event activated")]
        public bool exitTriggerUnsqueeze = true;

        [Header("Events")]
        public UnityHandEvent HandEnter;
        public UnityHandEvent HandExit;
        public UnityHandEvent HandGrab;
        public UnityHandEvent HandRelease;
        public UnityHandEvent HandSqueeze;
        public UnityHandEvent HandUnsqueeze;

        //For Programmers <3
        public HandEvent HandEnterEvent;
        public HandEvent HandExitEvent;
        public HandEvent HandGrabEvent;
        public HandEvent HandReleaseEvent;
        public HandEvent HandSqueezeEvent;
        public HandEvent HandUnsqueezeEvent;

        List<Hand> hands;
        bool grabbing;
        bool squeezing;

        protected virtual void OnEnable() {
            hands = new List<Hand>();
            HandEnterEvent += (hand) => HandEnter?.Invoke(hand);
            HandExitEvent += (hand) => HandExit?.Invoke(hand);
            HandGrabEvent += (hand) => HandGrab?.Invoke(hand);
            HandReleaseEvent += (hand) => HandRelease?.Invoke(hand);
            HandSqueezeEvent += (hand) => HandSqueeze?.Invoke(hand);
            HandUnsqueezeEvent += (hand) => HandUnsqueeze?.Invoke(hand);
        }

        protected virtual void OnDisable() {
            HandEnterEvent -= (hand) => HandEnter?.Invoke(hand);
            HandExitEvent -= (hand) => HandExit?.Invoke(hand);
            HandGrabEvent -= (hand) => HandGrab?.Invoke(hand);
            HandReleaseEvent -= (hand) => HandRelease?.Invoke(hand);
            HandSqueezeEvent -= (hand) => HandSqueeze?.Invoke(hand);
            HandUnsqueezeEvent -= (hand) => HandUnsqueeze?.Invoke(hand);
            for (int i = hands.Count - 1; i >= 0; i--){
                hands[i].RemoveHandTriggerArea(this);
            }
        }

        protected virtual void Update(){
            foreach (var hand in hands){
                if (!hand.enabled) {
                    Exit(hand);
                    Release(hand);
                }
            }
        }

        public virtual void Enter(Hand hand) {
            if (enabled == false || handType == HandType.none || (hand.left && handType == HandType.right) || (!hand.left && handType == HandType.left))
                return;

            if(!hands.Contains(hand)) {
                hands.Add(hand);
                if(oneHanded && hands.Count == 1)
                    HandEnterEvent?.Invoke(hand);
                else
                    HandEnterEvent?.Invoke(hand);
            }
        }

        public virtual void Exit(Hand hand) {
            if (enabled == false || handType == HandType.none || (hand.left && handType == HandType.right) || (!hand.left && handType == HandType.left))
                return;

            if(hands.Contains(hand)) {
                if(oneHanded && hands[0] == hand){
                    HandExit?.Invoke(hand);

                    if(grabbing && exitTriggerRelease){
                        HandReleaseEvent?.Invoke(hand);
                        grabbing = false;
                    }
                    if(squeezing && exitTriggerUnsqueeze){
                        HandUnsqueezeEvent?.Invoke(hand);
                        squeezing = false;
                    }

                    //If there is another hand, it enters
                    if(hands.Count > 1)
                        HandEnterEvent?.Invoke(hands[1]);

                }
                else if(!oneHanded){
                    HandExitEvent?.Invoke(hand);
                    if(grabbing && exitTriggerRelease){
                        HandReleaseEvent?.Invoke(hand);
                        grabbing = false;
                    }
                    if(squeezing && exitTriggerUnsqueeze){
                        HandUnsqueezeEvent?.Invoke(hand);
                        squeezing = false;
                    }

                }

                hands.Remove(hand);
            }
        }


        public virtual void Grab(Hand hand) {
            if (enabled == false || handType == HandType.none || (hand.left && handType == HandType.right) || (!hand.left && handType == HandType.left))
                return;

            if(grabbing)
                return;

            if(oneHanded && hands[0] == hand){
                HandGrabEvent?.Invoke(hand);
                grabbing = true;
            }
            else if(!oneHanded){
                HandGrabEvent?.Invoke(hand);
                grabbing = true;
            }
        }

        public virtual void Release(Hand hand) {
            if (enabled == false || handType == HandType.none || (hand.left && handType == HandType.right) || (!hand.left && handType == HandType.left))
                return;

            if(!grabbing)
                return;

            if(oneHanded && hands[0] == hand){
                HandReleaseEvent?.Invoke(hand);
                grabbing = false;
            }
            else if(!oneHanded){
                HandReleaseEvent?.Invoke(hand);
                grabbing = false;
            }
        }


        public virtual void Squeeze(Hand hand) {
            if (enabled == false || handType == HandType.none || (hand.left && handType == HandType.right) || (!hand.left && handType == HandType.left))
                return;

            if(squeezing)
                return;

            if(oneHanded && hands[0] == hand){
                HandSqueezeEvent?.Invoke(hand);
                squeezing = true;
            }
            else if(!oneHanded){
                squeezing = true;
                HandSqueezeEvent?.Invoke(hand);
            }
        }

        public virtual void Unsqueeze(Hand hand) {
            if (enabled == false || handType == HandType.none || (hand.left && handType == HandType.right) || (!hand.left && handType == HandType.left))
                return;

            if(!squeezing)
                return;

            if(oneHanded && hands[0] == hand){
                HandUnsqueezeEvent?.Invoke(hand);
                squeezing = false;
            }
            else if(!oneHanded){
                squeezing = false;
                HandUnsqueezeEvent?.Invoke(hand);
            }
        }
}
}
