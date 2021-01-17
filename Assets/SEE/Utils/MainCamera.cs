﻿using SEE.DataModel;
using UnityEngine;

namespace SEE.Utils
{
    public static class MainCamera
    {
        /// <summary>
        /// The main camera in the scene. The main camera is the first enabled
        /// camera tagged "MainCamera". An exception will be thrown when there 
        /// is no such camera.
        /// 
        /// Unlike the equivalent Camera.main property, the main camera will be
        /// cached and re-used upon every other access. This helps to avoid the
        /// relatively expensive lookup of Camera.main.
        /// 
        /// If there are multiple cameras in the scene, a warning will be logged
        /// and all cameras will be enumerated in the log.
        /// </summary>
        public static Camera Camera
        {
            get
            {
                if (camera == null)
                {
                    switch (Camera.allCameras.Length)
                    {
                        case 0:
                            Debug.LogError("There is no camera in the scene.\n");
                            throw new System.Exception("There is no camera in the scene");
                        case 1:
                            camera = Camera.main;
                            break;
                        default:
                            Debug.LogWarningFormat("There are {0} cameras in the scene. Expect unexpected visual results.\n",
                                                   Camera.allCameras.Length);
                            foreach (Camera c in Camera.allCameras)
                            {
                                // Analogous to Camera.main we are returning the first enabled camera tagged "MainCamera".
                                if (camera == null && c.CompareTag(Tags.MainCamera))
                                {
                                    camera = c;
                                }
                                Debug.LogWarningFormat("Camera: {0}\n", c.name);                                
                            }
                            camera = Camera.main;
                            break;
                    }
                    Debug.LogFormat("Selected main camera: {0}.\n", camera.name);
                }
                return camera;
            }
        }

        /// <summary>
        /// The main camera in the scene. May be null if there is no such camera.
        /// </summary>
        private static Camera camera;
    }
}