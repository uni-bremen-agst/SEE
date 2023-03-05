using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
    public class XRAutoHandAxisFingerBender : MonoBehaviour{
        public XRHandControllerLink controller;
        public CommonAxis axis;
        
        [HideInInspector]
        public float[] bendOffsets;
        float lastAxis;

        void LateUpdate(){
            var currAxis = controller.GetAxis(axis);
            for(int i = 0; i < controller.hand.fingers.Length; i++) {
                controller.hand.fingers[i].bendOffset += (currAxis-lastAxis)*bendOffsets[i];
            }
            lastAxis = currAxis;
        }
        
        
        private void OnDrawGizmosSelected() {
            if(controller == null && GetComponent<XRHandControllerLink>()){
                controller = GetComponent<XRHandControllerLink>();
                bendOffsets = new float[controller.hand.fingers.Length];
            }
        }
    }
}
