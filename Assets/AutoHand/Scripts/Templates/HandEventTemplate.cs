using UnityEngine;
using Autohand;

public class HandEventTemplate : MonoBehaviour{
    public Hand hand;

    void OnEnable() {
        hand.OnBeforeGrabbed += OnBeforeGrabbed;
        hand.OnGrabbed += OnGrabbed;
        hand.OnBeforeReleased += OnBeforeReleased;
        hand.OnReleased += OnReleased;
        hand.OnForcedRelease += OnForcedRelease;
        hand.OnGrabJointBreak += OnGrabJointBreak;

        hand.OnHandCollisionStart += OnHandCollisionStart;
        hand.OnHandCollisionStop += OnHandCollisionStop;
        hand.OnHandTriggerStart += OnHandTriggerStart;
        hand.OnHandTriggerStop += OnHandTriggerStop;

        hand.OnHighlight += OnHighlight;
        hand.OnStopHighlight += OnStopHighlight;

        hand.OnSqueezed += OnSqueezed;
        hand.OnUnsqueezed += OnUnsqueezed;

        hand.OnTriggerGrab += OnTriggerGrab;
        hand.OnTriggerRelease += OnTriggerRelease;
    }


    void OnDisable() {
        hand.OnBeforeGrabbed -= OnBeforeGrabbed;
        hand.OnGrabbed -= OnGrabbed;
        hand.OnBeforeReleased -= OnBeforeReleased;
        hand.OnReleased -= OnReleased;
        hand.OnForcedRelease -= OnForcedRelease;
        hand.OnGrabJointBreak -= OnGrabJointBreak;


        hand.OnHighlight -= OnHighlight;
        hand.OnStopHighlight -= OnStopHighlight;

        hand.OnSqueezed -= OnSqueezed;
        hand.OnUnsqueezed -= OnUnsqueezed;

        hand.OnTriggerGrab -= OnTriggerGrab;
        hand.OnTriggerRelease -= OnTriggerRelease;


        hand.OnHandCollisionStart -= OnHandCollisionStart;
        hand.OnHandCollisionStop -= OnHandCollisionStop;
        hand.OnHandTriggerStart -= OnHandTriggerStart;
        hand.OnHandTriggerStop -= OnHandTriggerStop;
    }

    void OnBeforeGrabbed(Hand hand, Grabbable grab) {
        //Called when an object is grabbed before anything else
    }

    void OnGrabbed(Hand hand, Grabbable grab) {
        //Called when an object is grabbed
    }

    void OnBeforeReleased(Hand hand, Grabbable grab) {
        //Called when a held object is released before anything else
    }

    void OnReleased(Hand hand, Grabbable grab) {
        //Called when a held object is released
    }

    void OnForcedRelease(Hand hand, Grabbable grab) {
        //Called when the force release functions is called

    }


    void OnGrabJointBreak(Hand hand, Grabbable grab) {
        //Called when the joint between the hand the grabbable breaks
    }


    void OnHighlight(Hand hand, Grabbable grab) {
        //Called when the hand grab targets a new object
    }
    
    void OnStopHighlight(Hand hand, Grabbable grab) {
        //Called when the hand grab stops targeting an object
    }



    void OnSqueezed(Hand hand, Grabbable grab) {
        //Called when the "Squeeze" event is called, this event is tied to a seconary controller input through the HandControllerLink component on the hand
    }
    void OnUnsqueezed(Hand hand, Grabbable grab) {
        //Called when the "Unsqueeze" event is called, this event is tied to a seconary controller input through the HandControllerLink component on the hand
    }



    void OnTriggerGrab(Hand hand, Grabbable grab) {
        //Called when the "Grab" event is called, regardless of whether something is being grabbed or not
    }
    void OnTriggerRelease(Hand hand, Grabbable grab) {
        //Called when the "Release" event is called, regardless of whether something is being held or released
    }



    void OnHandCollisionStart(Hand hand, GameObject other) {
        //Called when the hand hits an object for the first time and isn't already colliding
    }

    void OnHandCollisionStop(Hand hand, GameObject other) {
        //Called all the hand has zero collisions on the object

    }

    void OnHandTriggerStart(Hand hand, GameObject other) {
        //Called when the hand triggers an object for the first time and isn't already triggering
    }

    void OnHandTriggerStop(Hand hand, GameObject other) {
        //Called when the hand has zero colliders overlapping this trigger
    }


}
