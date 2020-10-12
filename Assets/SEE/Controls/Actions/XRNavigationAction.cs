using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{

    public sealed class XRNavigationAction : NavigationAction
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
        private InteractableObject leftInteractable;
        private InteractableObject rightInteractable;
        private Vector3 cityStartGrabScale;
        private float startHandDistance;

        private SteamVR_Action_Boolean leftGripAction;
        private SteamVR_Action_Boolean rightGripAction;

        private Interactable attachedInteractable;
        private SteamVR_Action_Single grabAction;



        protected sealed override void Awake()
        {
            if (FindObjectOfType<PlayerSettings>().playerInputType != PlayerSettings.PlayerInputType.VR)
            {
                Destroy(this);
                return;
            }

            SteamVR_Input.GetActionSet(XRInput.DefaultActionSetName).Activate();
            leftGripAction = SteamVR_Input.GetBooleanAction(XRInput.DefaultActionSetName, "LGrip");
            rightGripAction = SteamVR_Input.GetBooleanAction(XRInput.DefaultActionSetName, "RGrip");
            grabAction = SteamVR_Input.GetSingleAction(XRInput.DefaultActionSetName, "Grab");

            base.Awake();
        }

        public sealed override void Update()
        {
            base.Update();

            if (!CityAvailable)
            {
                return;
            }

            if (grabAction.lastAxis < 0.9f && grabAction.axis >= 0.9f)
            {
                Hand theRightHand = Player.instance.rightHand;
                Interactable hoveringInteractable = theRightHand.hoveringInteractable;
                if (hoveringInteractable && hoveringInteractable.GetComponentInParent<XRNavigationAction>() == this)
                {
                    const Hand.AttachmentFlags AttachmentFlags
                        = Hand.defaultAttachmentFlags
                        & (~Hand.AttachmentFlags.SnapOnAttach)
                        & (~Hand.AttachmentFlags.DetachOthers)
                        & (~Hand.AttachmentFlags.VelocityMovement);

                    attachedInteractable = hoveringInteractable;
                    theRightHand.HoverLock(hoveringInteractable);
                    theRightHand.AttachObject(hoveringInteractable.gameObject, GrabTypes.Pinch, AttachmentFlags);
                }
            }
            else if (grabAction.lastAxis >= 0.9f && grabAction.axis < 0.9f && attachedInteractable)
            {
                Hand theRightHand = Player.instance.rightHand;
                attachedInteractable.transform.rotation = Quaternion.identity;

                theRightHand.DetachObject(attachedInteractable.gameObject);
                theRightHand.HoverUnlock(attachedInteractable);
                attachedInteractable = null;
            }

            if (leftGripAction.state && !leftHand
                && Player.instance.leftHand.hoveringInteractable
                && Player.instance.leftHand.hoveringInteractable.GetComponentInParent<XRNavigationAction>() == this)
            {
                OnStartGrab(Player.instance.leftHand, Player.instance.leftHand.hoveringInteractable.gameObject);
            }
            else if (!leftGripAction.state && leftHand)
            {
                OnStopGrab(Player.instance.leftHand);
            }

            if (rightGripAction.state && !rightHand
                && Player.instance.rightHand.hoveringInteractable
                && Player.instance.rightHand.hoveringInteractable.GetComponentInParent<XRNavigationAction>() == this)
            {
                OnStartGrab(Player.instance.rightHand, Player.instance.rightHand.hoveringInteractable.gameObject);
            }
            else if (!rightGripAction.state && rightHand)
            {
                OnStopGrab(Player.instance.rightHand);
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

                    zoomState.currentZoomFactor = CityTransform.localScale.x / zoomState.originalScale.x;
                    float zoomSteps = ConvertZoomFactorToZoomSteps(zoomState.currentZoomFactor);
                    zoomState.currentTargetZoomSteps = zoomSteps;
                    new Net.SyncCitiesAction(this).Execute();
                }
            }
        }

        private void OnStartGrab(Hand hand, GameObject go)
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
                    ref InteractableObject refInteractable = ref rightInteractable;

                    if (hand.handType == SteamVR_Input_Sources.LeftHand)
                    {
                        refHand = ref leftHand;
                        refDistance = ref leftDistance;
                        refAngle = ref leftAngle;
                        refInteractable = ref leftInteractable;
                    }

                    if (!refHand)
                    {
                        refHand = hand;

                        Vector2 toHandV2 = hand.transform.position.XZ() - CityTransform.position.XZ();
                        refDistance = toHandV2.magnitude;

                        toHandV2.Normalize();
                        refAngle = Mathf.Atan2(toHandV2.y, toHandV2.x) + Mathf.Deg2Rad * CityTransform.rotation.eulerAngles.y;

                        refInteractable = go.GetComponent<InteractableObject>();
                        refInteractable?.SetSelect(true, true);

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

        private void OnStopGrab(Hand hand)
        {
            if (hand == leftHand)
            {
                leftHand = null;
                leftInteractable.SetSelect(false, true);
                leftInteractable = null;
                mode -= 1;
                if (mode == XRNavigationMode.MoveOnly)
                {
                    rightDistance = (rightHand.transform.position.XZ() - CityTransform.position.XZ()).magnitude;
                }
            }
            else if (hand == rightHand)
            {
                rightHand = null;
                rightInteractable.SetSelect(false, true);
                rightInteractable = null;
                mode -= 1;
                if (mode == XRNavigationMode.MoveOnly)
                {
                    leftDistance = (leftHand.transform.position.XZ() - CityTransform.position.XZ()).magnitude;
                }
            }
        }
    }

}
