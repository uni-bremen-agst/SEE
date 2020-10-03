using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{

    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Plane))]
    public class XRNavigationAction : NavigationAction
    {
        private enum XRNavigationMode
        {
            None,
            MoveOnly,
            MoveRotateScale
        }

        private XRNavigationMode mode;
        private Hand rightHand;
        private Hand leftHand;
        private float rightDistance;
        private float leftDistance;
        private float rightAngle;
        private float leftAngle;
        private Vector3 cityStartGrabScale;
        private float startHandDistance;

        private SteamVR_Action_Boolean leftGripAction;
        private SteamVR_Action_Boolean rightGripAction;



        protected sealed override void Start()
        {
            if (FindObjectOfType<PlayerSettings>().playerInputType != PlayerSettings.PlayerInputType.VR)
            {
                Destroy(this);
                return;
            }

            base.Start();

            SteamVR_Input.GetActionSet("default").Activate();
            leftGripAction = SteamVR_Input.GetBooleanAction("default", "LGrip");
            rightGripAction = SteamVR_Input.GetBooleanAction("default", "RGrip");

            Debug.LogFormat("XRNavigationAction controls {0}.\n", CityTransform.name);
        }

        private void Update()
        {
            // release
            if (!leftGripAction.state && leftHand)
            {
                leftHand = null;
                mode -= 1;
                if (mode == XRNavigationMode.MoveOnly)
                {
                    rightDistance = (rightHand.transform.position.XZ() - CityTransform.position.XZ()).magnitude;
                }
            }
            if (!rightGripAction.state && rightHand)
            {
                rightHand = null;
                mode -= 1;
                if (mode == XRNavigationMode.MoveOnly)
                {
                    leftDistance = (leftHand.transform.position.XZ() - CityTransform.position.XZ()).magnitude;
                }
            }

            if (!UpdateZoom())
            {
                // move, rotate, scale
                if (mode != XRNavigationMode.None)
                {
                    float scaleFactor = 1.0f;
                    if (mode == XRNavigationMode.MoveRotateScale)
                    {
                        float currentDistance = (leftHand.transform.position.XZ() - rightHand.transform.position.XZ()).magnitude;
                        scaleFactor = currentDistance / startHandDistance;
                    }

                    // align with first hand
                    Hand fstHand = rightHand;
                    float fstDistance = rightDistance;
                    float fstAngle = rightAngle;
                    if (!fstHand)
                    {
                        fstHand = leftHand;
                        fstDistance = leftDistance;
                        fstAngle = leftAngle;
                    }

                    fstDistance *= scaleFactor;
                    float cityAngleOffset = Mathf.Deg2Rad * CityTransform.rotation.eulerAngles.y;
                    fstAngle -= cityAngleOffset;

                    Vector2 fstOffsetV2 = new Vector2(Mathf.Cos(fstAngle), Mathf.Sin(fstAngle)) * fstDistance;
                    Vector2 fstHandPositionV2 = fstHand.transform.position.XZ();

                    Vector2 fstNewCityPositionV2 = fstHandPositionV2 - fstOffsetV2;
                    CityTransform.position = new Vector3(fstNewCityPositionV2.x, CityTransform.position.y, fstNewCityPositionV2.y);

                    // align with second hand
                    if (mode == XRNavigationMode.MoveRotateScale)
                    {
                        // move, rotate
                        float sndAngle = leftAngle - cityAngleOffset;

                        Vector2 sndOffsetV2 = new Vector2(Mathf.Cos(sndAngle), Mathf.Sin(sndAngle)) * (leftDistance * scaleFactor);
                        Vector2 sndHandPositionV2 = leftHand.transform.position.XZ();

                        Vector2 v0 = sndHandPositionV2 - fstHandPositionV2;
                        Vector2 v1 = (fstNewCityPositionV2 + sndOffsetV2) - fstHandPositionV2;
                        float a = Vector2.SignedAngle(v0, v1);

                        CityTransform.RotateAround(fstHand.transform.position, Vector3.up, a);

                        // scale
                        CityTransform.localScale = cityStartGrabScale * scaleFactor;
                    }

                    zoomState.currentZoomFactor = zoomState.originalScale.x / CityTransform.localScale.x;
                    float zoomSteps = ConvertZoomFactorToZoomSteps(zoomState.currentZoomFactor);
                    zoomState.currentTargetZoomSteps = zoomSteps;
                    new Net.SyncCitiesAction(this).Execute();
                }
            }
        }

        public void OnStartGrab(Hand hand)
        {
            if (hand.handType == SteamVR_Input_Sources.LeftHand && leftHand == null
                || hand.handType == SteamVR_Input_Sources.RightHand && rightHand == null)
            {
                GrabTypes grabType = hand.GetGrabStarting();
                if (grabType == GrabTypes.Grip)
                {
                    ref Hand refHand = ref rightHand;
                    ref float refDistance = ref rightDistance;
                    ref float refAngle = ref rightAngle;

                    if (hand.handType == SteamVR_Input_Sources.LeftHand)
                    {
                        refHand = ref leftHand;
                        refDistance = ref leftDistance;
                        refAngle = ref leftAngle;
                    }

                    if (!refHand)
                    {
                        refHand = hand;

                        Vector2 toHandV2 = hand.transform.position.XZ() - CityTransform.position.XZ();
                        refDistance = toHandV2.magnitude;

                        toHandV2.Normalize();
                        refAngle = Mathf.Atan2(toHandV2.y, toHandV2.x) + Mathf.Deg2Rad * CityTransform.rotation.eulerAngles.y;

                        mode += 1;
                    }

                    if (mode == XRNavigationMode.MoveRotateScale)
                    {
                        cityStartGrabScale = CityTransform.localScale;
                        startHandDistance = (leftHand.transform.position.XZ() - rightHand.transform.position.XZ()).magnitude;
                    }
                }
            }
        }
    }

}
