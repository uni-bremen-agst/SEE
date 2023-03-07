using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Autohand.Demo{
    public enum CommonButton {
        gripButton,
        menuButton,
        primaryButton,
        secondaryButton,
        triggerButton,
        primary2DAxisClick,
        primary2DAxisTouch,
#if UNITY_2019_2_OR_NEWER
        secondary2DAxisClick,
        secondary2DAxisTouch,
#endif
        primaryTouch,
        secondaryTouch,
        none
    }
    
    public enum CommonAxis {
        trigger,
        grip,
        none
    }

    public enum Common2DAxis {
        primaryAxis,
        secondaryAxis,
        none
    }

    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/controller-input")]
    public class XRHandControllerLink : HandControllerLink {
        public CommonButton grabButton = CommonButton.triggerButton;
        [Tooltip("This axis will bend all the fingers on the hand -> replaced with finger bender scripts")]
        public CommonAxis grabAxis = CommonAxis.trigger;
        public CommonAxis squeezeAxis = CommonAxis.grip;
        public CommonButton squeezeButton = CommonButton.gripButton;

        XRNode role;
        bool squeezing;
        bool grabbing;
        InputDevice device;
        List<InputDevice> devices = new List<InputDevice>();

        private void Start(){
            if(grabButton == squeezeButton) {
                Debug.LogError("AUTOHAND: You are using the same button for grab and squeeze on HAND CONTROLLER LINK, this may create conflict or errors", this);
            }

            if(hand.left)
                role = XRNode.LeftHand;
            else
                role = XRNode.RightHand;

            if(hand.left)
                handLeft = this;
            else
                handRight = this;
        }

        void Update(){

            InputDevices.GetDevicesAtXRNode(role, devices);
            if(devices.Count > 0)
                device = devices[0];

            if(device != null && device.isValid){
                //Sets hand fingers wrap
                hand.SetGrip(GetAxis(grabAxis), GetAxis(squeezeAxis));

                //Grab input
                if(device.TryGetFeatureValue(GetCommonButton(grabButton), out bool grip)) {
                    if(grabbing && !grip){
                        hand.Release();
                        grabbing = false;
                    }
                    else if(!grabbing && grip){
                        hand.Grab();
                        grabbing = true;
                    }
                }
                //Squeeze input
                if(device.TryGetFeatureValue(GetCommonButton(squeezeButton), out bool squeeze)) {
                    if(squeezing && !squeeze){
                        hand.Unsqueeze();
                        squeezing = false;
                    }
                    else if(!squeezing && squeeze){
                        hand.Squeeze();
                        squeezing = true;
                    }
                }
            }
        }

        public List<InputDevice> Devices() { return devices; }


        public bool ButtonPressed(CommonButton button) {
            if (button == CommonButton.none)
                return false;

            if(device.TryGetFeatureValue(GetCommonButton(button), out bool pressed)) {
                return pressed;
            }

            return false;
        }


        public float GetAxis(CommonAxis axis){
            if (axis == CommonAxis.none)
                return 0;

            if(device.TryGetFeatureValue(GetCommonAxis(axis), out float axisValue)) {
                return axisValue;
            }
            return 0;
        }


        public Vector2 GetAxis2D(Common2DAxis axis) {
            if (axis == Common2DAxis.none)
                return Vector2.zero;

            if(device.TryGetFeatureValue(GetCommon2DAxis(axis), out Vector2 axisValue)) {
                return axisValue;
            }
            return Vector2.zero;
        }

        /// <param name="freq">not supported on XR?</param>
        public override void TryHapticImpulse(float duration, float amp, float freq = 0) {
            foreach(var device in Devices()) {
                if(device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse) {
                    device.SendHapticImpulse(0u, amp, duration);
                }
            }
        }


        public static InputFeatureUsage<bool> GetCommonButton(CommonButton button) {
            if(button == CommonButton.gripButton)
                return CommonUsages.gripButton;
            if(button == CommonButton.menuButton)
                return CommonUsages.menuButton;
            if(button == CommonButton.primary2DAxisClick)
                return CommonUsages.primary2DAxisClick;
            if(button == CommonButton.primary2DAxisTouch)
                return CommonUsages.primary2DAxisTouch;
            if(button == CommonButton.primaryButton)
                return CommonUsages.primaryButton;
            if(button == CommonButton.primaryTouch)
                return CommonUsages.primaryTouch;
#if UNITY_2019_2_OR_NEWER
            if (button == CommonButton.secondary2DAxisClick)
                return CommonUsages.secondary2DAxisClick;
            if(button == CommonButton.secondary2DAxisTouch)
                return CommonUsages.secondary2DAxisTouch;
#endif
            if(button == CommonButton.secondaryButton)
                return CommonUsages.secondaryButton;
            if(button == CommonButton.secondaryTouch)
                return CommonUsages.secondaryTouch;
            
            return CommonUsages.triggerButton;
        }

        public static InputFeatureUsage<float> GetCommonAxis(CommonAxis axis) {
            if(axis == CommonAxis.grip)
                return CommonUsages.grip;
            else
                return CommonUsages.trigger;
        }

        public static InputFeatureUsage<Vector2> GetCommon2DAxis(Common2DAxis axis) {
            if(axis == Common2DAxis.primaryAxis)
                return CommonUsages.primary2DAxis;
            else
                return CommonUsages.secondary2DAxis;
        }
    }
}
