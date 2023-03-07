using Autohand.Demo;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Autohand {
    [CustomEditor(typeof(XRAutoHandAxisFingerBender))]
    public class XRAutoHandAxisFingerBenderEditor : Editor{
        XRAutoHandAxisFingerBender bender;

        void OnEnable() {
            bender = target as XRAutoHandAxisFingerBender;
        }

        public override void OnInspectorGUI() {
            EditorUtility.SetDirty(bender);

            DrawDefaultInspector();
            EditorGUILayout.Space();
            if(bender.controller != null) {
                if(bender.bendOffsets.Length != bender.controller.hand.fingers.Length)
                    bender.bendOffsets = new float[bender.controller.hand.fingers.Length];
                for(int i = 0; i < bender.controller.hand.fingers.Length; i++) {
                    var layout = EditorGUILayout.GetControlRect();
                    layout.width /= 2;
                    var text = new GUIContent(bender.controller.hand.fingers[i].name + " Offset", "0 is no bend, 0.5 is half bend, 1 is full bend, -1 to stiffen finger from sway");
                    EditorGUI.LabelField(layout, text);
                    layout.x += layout.width;
                    bender.bendOffsets[i] = EditorGUI.FloatField(layout, bender.bendOffsets[i]);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
