using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    [RequireComponent(typeof(Grabbable))]
    public class GrabLock : MonoBehaviour{
        [Header("Hand.Released() must be called elsewhere")]
        [Header("Use this script to prevent grabbable release")]
        
        //THIS SCRIPT ALLOWS YOU TO HOLD AN OBJECT AFTER TRIGGER RELEASE AND CALL THIS EVENT WITH TRIGGER PRESS
        public UnityHandGrabEvent OnGrabPressed;

    }
}
