using UnityEngine;
using UnityEngine.XR;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Implements actions that can be triggered by the laser pointer in XR.
    /// Currently, the label of the hit game object is shown if the game object
    /// has a <see cref="ShowLabel"/>.
    /// </summary>
    /// <remarks>This component is attached to the XR rig prefab.</remarks>
    public class VRPointerAction : MonoBehaviour
    {
        /// <summary>
        /// The last shown label. May be null.
        /// </summary>
        private ShowLabel showLabel;

        /// <summary>
        /// The length of the ray used to hit anything with a label.
        /// </summary>
        private const float rayLength = 3.0f;

        /// <summary>
        /// If a game object is hit by a ray cast from the right hand-held XR controller
        /// that has a <see cref="ShowLabel"/> component attachted to it, the label
        /// will be shown. If a label was shown previously, that label will be turned
        /// off before the newly selected label is shown.
        /// </summary>
        void Update()
        {
            UnityEngine.XR.InputDevice handRDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            handRDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 posR);
            Vector3 vPosition = transform.TransformPoint(posR); //to world coords
            handRDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rotR);
            Vector3 vGazeDirection = rotR * Vector3.forward;
            vGazeDirection = transform.TransformDirection(vGazeDirection);
            if (Physics.Raycast(vPosition, vGazeDirection, out RaycastHit hit, rayLength))
            {
                showLabel?.Off();
                showLabel = null;
                if (hit.transform.TryGetComponent(out showLabel))
                {
                    showLabel?.On();
                }
            }
            else
            {
                showLabel?.Off();
                showLabel = null;
            }
        }
    }
}