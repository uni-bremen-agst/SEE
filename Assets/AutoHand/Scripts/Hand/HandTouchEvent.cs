using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/hand-touch-trigger")]
    public class HandTouchEvent : MonoBehaviour{
        [Header("For Solid Collision")]
        [Tooltip("Whether or not first hand to enter should take ownership and be the only one to call events")]
        public bool oneHanded = true;
        public HandType handType = HandType.both;

        [Header("Events")]
        public UnityHandEvent HandStartTouch;
        public UnityHandEvent HandStopTouch;
        
        public HandEvent HandStartTouchEvent;
        public HandEvent HandStopTouchEvent;

        private void OnEnable() {
            hands = new List<Hand>();
            HandStartTouchEvent += (hand) => HandStartTouch?.Invoke(hand);
            HandStopTouchEvent += (hand) => HandStopTouch?.Invoke(hand);
        }

        private void OnDisable() {
            HandStartTouchEvent -= (hand) => HandStartTouch?.Invoke(hand);
            HandStopTouchEvent -= (hand) => HandStopTouch?.Invoke(hand);
        }
        
        List<Hand> hands;

        public void Touch(Hand hand) {
            if (enabled == false || handType == HandType.none || (hand.left && handType == HandType.right) || (!hand.left && handType == HandType.left))
                return;

            if(!hands.Contains(hand)) {
                if(oneHanded && hands.Count == 0)
                    HandStartTouchEvent?.Invoke(hand);
                else
                    HandStartTouchEvent?.Invoke(hand);

                hands.Add(hand);
            }
        }
        
        public void Untouch(Hand hand) {
            if (enabled == false || handType == HandType.none || (hand.left && handType == HandType.right) || (!hand.left && handType == HandType.left))
                return;

            if(hands.Contains(hand)) {
                if(oneHanded && hands[0] == hand){
                    HandStopTouchEvent?.Invoke(hand);
                }
                else if(!oneHanded){
                    HandStopTouchEvent?.Invoke(hand);
                }

                hands.Remove(hand);
            }
        }
    }
}
