using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
    public class XRAutoHandFingerBender : MonoBehaviour{
        public XRHandControllerLink controller;
        public CommonButton button;
        
        [HideInInspector]
        public float[] bendOffsets;

        bool pressed;
        
        void Update(){
            if(!pressed && controller.ButtonPressed(button)) {
                pressed = true;
                for(int i = 0; i < controller.hand.fingers.Length; i++) {
                    controller.hand.fingers[i].bendOffset += bendOffsets[i];
                }
            }
            else if(pressed && !controller.ButtonPressed(button)) {
                pressed = false;
                for(int i = 0; i < controller.hand.fingers.Length; i++) {
                    controller.hand.fingers[i].bendOffset -= bendOffsets[i];
                }
            }
        }


        private void OnDrawGizmosSelected() {
            if(controller == null && GetComponent<XRHandControllerLink>()){
                controller = GetComponent<XRHandControllerLink>();
                bendOffsets = new float[controller.hand.fingers.Length];
            }
        }
    }
}
