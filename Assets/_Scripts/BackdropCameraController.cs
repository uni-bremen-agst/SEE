using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE
{

    public class BackdropCameraController : MonoBehaviour
    {
        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;
        }
        
        void Update()
        {
            transform.position = mainCamera.transform.position;
            transform.rotation = mainCamera.transform.rotation;
        }
    }

}// namespace SEE
