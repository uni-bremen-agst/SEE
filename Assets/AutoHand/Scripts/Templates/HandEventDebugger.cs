using Autohand;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Autohand.Demo {
    [RequireComponent(typeof(Hand))]
    public class HandEventDebugger : MonoBehaviour
    {
        public bool showSqueezeEvents = true;
        public bool showHighlightEvents = true;

        private void OnEnable()
        {
            var hand1 = GetComponent<Hand>();
            hand1.OnBeforeGrabbed += (hand, grabbable) => { Debug.Log(hand.name + " BEFORE GRAB EVENT", this); };
            hand1.OnGrabbed += (hand, grabbable) => { Debug.Log(hand.name + " GRAB EVENT", this); };
            hand1.OnReleased += (hand, grabbable) => { Debug.Log(hand.name + " RELEASE EVENT", this); };
            hand1.OnGrabJointBreak += (hand, grabbable) => { Debug.Log(hand.name + " JOINT BREAK EVENT", this); };
            
            if(showSqueezeEvents) hand1.OnSqueezed += (hand, grabbable) => { Debug.Log(hand.name + " SQUEEZE EVENT", this); };
            if (showSqueezeEvents) hand1.OnUnsqueezed += (hand, grabbable) => { Debug.Log(hand.name + " UNSQUEEZE EVENT", this); };
            if (showHighlightEvents) hand1.OnHighlight += (hand, grabbable) => { Debug.Log(hand.name + " HIGHLIGHT EVENT", this); };
            if (showHighlightEvents) hand1.OnStopHighlight += (hand, grabbable) => { Debug.Log(hand.name + " UNHIGHLIGHT EVENT", this); };
        }

        private void OnDisable()
        {
            var hand1 = GetComponent<Hand>();
            hand1.OnBeforeGrabbed -= (hand, grabbable) => { Debug.Log(hand.name + " BEFORE GRAB EVENT", this); };
            hand1.OnGrabbed -= (hand, grabbable) => { Debug.Log(hand.name + " GRAB EVENT", this); };
            hand1.OnReleased -= (hand, grabbable) => { Debug.Log(hand.name + " RELEASE EVENT", this); };
            hand1.OnGrabJointBreak -= (hand, grabbable) => { Debug.Log(hand.name + " CONNECTION BREAK EVENT", this); };
            
            if (showSqueezeEvents) hand1.OnSqueezed -= (hand, grabbable) => { Debug.Log(hand.name + " SQUEEZE EVENT", this); };
            if (showSqueezeEvents) hand1.OnUnsqueezed -= (hand, grabbable) => { Debug.Log(hand.name + " UNSQUEEZE EVENT", this); };
            if (showHighlightEvents) hand1.OnHighlight -= (hand, grabbable) => { Debug.Log(hand.name + " HIGHLIGHT EVENT", this); };
            if (showHighlightEvents) hand1.OnStopHighlight -= (hand, grabbable) => { Debug.Log(hand.name + " UNHIGHLIGHT EVENT", this); };
        }
    }
}