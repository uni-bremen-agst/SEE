using System.Collections.Generic;
using UnityEngine;

namespace SEE
{
    public class Cameras
    {
        /// <summary>
        /// Returns all main cameras (name equals "Main Camera" and tag equals "MainCamera"
        /// no matter whether they are activated or not.
        /// </summary>
        /// <returns></returns>
        public static IList<GameObject> AllMainCameras()
        {
            IList<GameObject> result = new List<GameObject>();
            // FindObjectsOfTypeAll returns also inactive game objects
            foreach (GameObject o in Resources.FindObjectsOfTypeAll(typeof(UnityEngine.GameObject)))
            {
                if (o.name == "Main Camera" && o.tag == "MainCamera")
                {
                    result.Add(o);
                }
            }
            return result;
        }

        /// <summary>
        /// Adjusts the spead of the camera according to the space unit. If we use simple
        /// cubes for the buildings, the unit is the normal Unity unit. If we use CScape
        /// buildings, the unit is larger than the normal Unity unit and, hence, camera
        /// speed must be adjusted accordingly.
        /// </summary>
        /// <param name="unit">the factor by which to multiply the camera speed</param>
        public static void AdjustCameraSpeed(float unit)
        {
            foreach (GameObject camera in AllMainCameras())
            {
                FlyCamera flightControl = camera.GetComponent<FlyCamera>();
                if (flightControl != null)
                {
                    flightControl.SetDefaults();
                    flightControl.AdjustSettings(unit);
                }
                // TODO: Adjust speed setting for Leap Rig camera
            }
        }
    }
}