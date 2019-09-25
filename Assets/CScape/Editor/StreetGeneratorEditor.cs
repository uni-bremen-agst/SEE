using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
using UnityEditor;

namespace CScape
{
    [CustomEditor(typeof(StreetGenerator))]
    // [CanEditMultipleObjects]

    public class StreetGeneratorEditor : Editor
    {

        void OnEnable()
        {
            StreetGenerator bm = (StreetGenerator)target;


        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            StreetGenerator sg = (StreetGenerator)target;

            if (GUILayout.Button("Add Street Details"))
            {
                sg.UpdateMe();
            }
        }
    }
}
