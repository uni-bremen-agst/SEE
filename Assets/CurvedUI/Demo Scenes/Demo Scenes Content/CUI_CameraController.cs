using UnityEngine;
using System.Collections;

namespace CurvedUI
{
    public class CUI_CameraController : MonoBehaviour
    {
        //settings
#pragma warning disable 0649
        [SerializeField]
        Transform CameraObject;

		[SerializeField]
        float rotationMargin = 25;

        [SerializeField]
        bool runInEditorOnly = true;
#pragma warning restore 0649
        
        //variables
        public static CUI_CameraController Instance;

        
        // Use this for initialization
        void Awake()
        {
            Instance = this;
        }
        
        #if UNITY_EDITOR
        // Update is called once per frame
        void Update()
        {
            if((Application.isEditor || !runInEditorOnly) && !UnityEngine.XR.XRSettings.enabled)
            {
                var mouse = CurvedUIInputModule.MousePosition;
                CameraObject.localEulerAngles 
                    = new Vector3(mouse.y.Remap(0, Screen.height, rotationMargin, -rotationMargin),
                    mouse.x.Remap(0, Screen.width, -rotationMargin, rotationMargin), 0);
            }
        }
        #endif
    }
}
