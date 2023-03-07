using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Autohand{
    /// <summary>
    /// THIS SCRIPT CAN BE ATTACHED TO A COLLIDER OBJECT TO REFERENCE A GRABBABLE BODY
    /// </summary>
    [DefaultExecutionOrder(1)]
    public class GrabbableChild : MonoBehaviour{
        public Grabbable grabParent;

        private void Start() {
            grabParent.SetGrabbableChild(this);
            if(gameObject.layer == LayerMask.NameToLayer("Default") || LayerMask.LayerToName(gameObject.layer) == "")
                gameObject.layer = LayerMask.NameToLayer(Hand.grabbableLayerNameDefault);
        }
    }
}
