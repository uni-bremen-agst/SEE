using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace SEE.Game
{
    /// <summary>
    /// Provides answers on the VR status. Allows to enable and disable VR.
    /// </summary>
    public static class VRStatus
    {
        /// <summary>
        /// Enables/disables the VR subsystem depending upon <paramref name="enable"/>.
        /// </summary>
        /// <param name="enable">if true, VR will be enabled</param>
        public static void Enable(bool enable)
        {
            if (!enable)
            {
                // Note: For some reason, disabling an XRDisplaySubsystem will also
                // disable the camera of the desktop player. For this reason,
                // we will bail out here.
                // FIXME: This needs further investigation and better handling.
                return; // nothing to done.
            }

            List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(xrDisplays);
            foreach (XRDisplaySubsystem display in xrDisplays)
            {
                if (enable)
                {
                    Debug.Log($"Starting VR display {display}\n");
                    display.Start();
                }
                else
                {
                    Debug.Log($"Stopping VR display {display}\n");
                    display.Stop();
                }
            }
        }

        /// <summary>
        /// True if VR is enabled.
        /// </summary>
        /// <returns>true if VR is enabled</returns>
        public static bool IsActive()
        {
            List<XRDisplaySubsystemDescriptor> displaysDescs = new List<XRDisplaySubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(displaysDescs);

            // If there are registered display descriptors that is a good indication that VR is most likely "enabled"
            return displaysDescs.Count > 0;
        }

        /// <summary>
        /// True if VR is running.
        /// </summary>
        /// <returns>true if VR is running</returns>
        public static bool IsVRRunning()
        {
            List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(displays);
            return displays.Any(display => display.running);
        }
    }
}
