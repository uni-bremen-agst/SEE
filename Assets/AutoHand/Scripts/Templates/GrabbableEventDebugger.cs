using Autohand;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Autohand.Demo {
    [RequireComponent(typeof(Grabbable))]public class GrabbableEventDebugger : MonoBehaviour
    {

        private void OnEnable()
        {
            var grab = GetComponent<Grabbable>();
            grab.OnBeforeGrabEvent += (hand, grabbable) => { Debug.Log(grabbable.name + " BEFORE GRAB EVENT"); };
            grab.OnGrabEvent += (hand, grabbable) => { Debug.Log(grabbable.name + " GRAB EVENT"); };
            grab.OnReleaseEvent += (hand, grabbable) => { Debug.Log(grabbable.name + " RELEASE EVENT"); };
            grab.OnJointBreakEvent += (hand, grabbable) => { Debug.Log(grabbable.name + " JOINT BREAK EVENT"); };
            grab.OnSqueezeEvent += (hand, grabbable) => { Debug.Log(grabbable.name + " SQUEEZE EVENT"); };
            grab.OnUnsqueezeEvent += (hand, grabbable) => { Debug.Log(grabbable.name + " UNSQUEEZE EVENT"); };
            grab.OnHighlightEvent += (hand, grabbable) => { Debug.Log(grabbable.name + " HIGHLIGHT EVENT"); };
            grab.OnUnhighlightEvent += (hand, grabbable) => { Debug.Log(grabbable.name + " UNHIGHLIGHT EVENT"); };
        }

        private void OnDisable()
        {
            var grab = GetComponent<Grabbable>();
            grab.OnBeforeGrabEvent -= (hand, grabbable) => { Debug.Log(grabbable.name + " BEFORE GRAB EVENT"); };
            grab.OnGrabEvent -= (hand, grabbable) => { Debug.Log(grabbable.name + " GRAB EVENT"); };
            grab.OnReleaseEvent -= (hand, grabbable) => { Debug.Log(grabbable.name + " RELEASE EVENT"); };
            grab.OnJointBreakEvent -= (hand, grabbable) => { Debug.Log(grabbable.name + " JOINT BREAK EVENT"); };
            grab.OnSqueezeEvent -= (hand, grabbable) => { Debug.Log(grabbable.name + " SQUEEZE EVENT"); };
            grab.OnUnsqueezeEvent -= (hand, grabbable) => { Debug.Log(grabbable.name + " UNSQUEEZE EVENT"); };
            grab.OnHighlightEvent -= (hand, grabbable) => { Debug.Log(grabbable.name + " HIGHLIGHT EVENT"); };
            grab.OnUnhighlightEvent -= (hand, grabbable) => { Debug.Log(grabbable.name + " UNHIGHLIGHT EVENT"); };
        }
    }
}