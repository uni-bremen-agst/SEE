using Autohand;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Autohand {
    [CustomEditor(typeof(Finger))]
    public class FingerEditor : Editor {
        Finger finger;
        float lastOffset;

        private void OnEnable() {
            finger = target as Finger;
            lastOffset = finger.bendOffset;
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            if(lastOffset != finger.bendOffset){
                lastOffset = finger.bendOffset;
                finger.SetFingerBend(lastOffset);
            }
        }
    }
}
