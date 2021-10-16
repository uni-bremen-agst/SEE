using UnityEngine;
using System.Collections;

namespace CurvedUI
{
    public class CUI_CameraRotationOnButtonHeld : MonoBehaviour {

#pragma warning disable 414
        //references
        [SerializeField]
        float Sensitivity = 0.5f;
#pragma warning restore 414
        
        //variables
        private Vector2 _oldMousePos;

        void Start () {
            _oldMousePos = CurvedUIInputModule.MousePosition;
        }

#if UNITY_EDITOR && !CURVEDUI_UNITY_XR
        void Update() {
        
            if (Input.GetButton("Fire2"))
            {
                var mouseDelta = CurvedUIInputModule.MousePosition - _oldMousePos;
                this.transform.eulerAngles += new Vector3(mouseDelta.y, -mouseDelta.x, 0) * Sensitivity;
            }

            _oldMousePos = CurvedUIInputModule.MousePosition;
        }
#endif
    }  
}

