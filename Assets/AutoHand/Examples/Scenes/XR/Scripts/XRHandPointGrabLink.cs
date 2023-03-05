using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
    public class XRHandPointGrabLink : MonoBehaviour{
        public HandDistanceGrabber pointGrab;
        public XRHandControllerLink link;

        [Header("Input")]
        public CommonButton pointInput;
        public CommonButton selectInput;

        bool pointing;
        bool selecting;

        void Update(){
            if (link.ButtonPressed(pointInput) && !pointing) {
                pointing = true;
                pointGrab.StartPointing();
            }

            if (!link.ButtonPressed(pointInput) && pointing){
                pointing = false;
                pointGrab.StopPointing();
            }

            
            if (link.ButtonPressed(selectInput) && !selecting) {
                selecting = true;
                pointGrab.SelectTarget();
            }
            
            if (!link.ButtonPressed(selectInput) && selecting){
                selecting = false;
                pointGrab.CancelSelect();
            }
        }
    }
}
