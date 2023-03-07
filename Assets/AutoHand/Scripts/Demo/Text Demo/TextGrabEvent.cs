using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;

namespace Autohand.Demo{
    public class TextGrabEvent : MonoBehaviour{
        public TextChanger changer;
        public Grabbable grab;
        [TextArea]
        public string message;

        private void Start() {
            if(grab == null && GetComponent<Grabbable>() != null)
                grab = GetComponent<Grabbable>();

            if(grab == null || changer == null)
                Destroy(this);

            grab.OnGrabEvent += OnGrab;
        }
        
        void OnGrab(Hand hand, Grabbable grab) {
            changer?.UpdateText(message);
        }
    }
}
