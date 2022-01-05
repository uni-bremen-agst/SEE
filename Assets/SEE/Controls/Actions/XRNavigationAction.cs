#if UNITY_ANDROID
#else
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls.Actions
{
    /// Controls the interactions with the city in desktop mode regarding the movement
    /// and perspective on a code city (rotating, dragging, zooming, etc.).
    /// 
    /// Note: These are the interactions in a VR environment with head-mounted
    /// display and VR controllers. Similar interactions specific to a desktop 
    /// environment with 2D display, mouse, and keyboard are implemented
    /// in DesktopNavigationAction.
    /// FIXME This need to be abstracted similar to NavigationAction.cs and DesktopNavigationAction.cs (which is already gone) were abstracted into ZoomAction.cs and ZoomActionDesktop.cs (and also MoveAction.cs etc.). Those scripts are attached to the e.g. DesktopPlayer prefab.
    public sealed class XRNavigationAction : NavigationAction
    {
        /// <summary>
        /// The current mode of navigation depents on the number of interacting hands.
        /// </summary>
        private enum XRNavigationMode
        {
            None,
            MoveOnly,
            MoveRotateScale
        }

        /// <summary>
        /// The current interaction mode.
        /// </summary>
        private XRNavigationMode mode;

        /// <summary>
        /// The currently interacting right hand or <code>null</code>, if it currently
        /// does not interact.
        /// </summary>
        private Hand rightHand;

        /// <summary>
        /// The currently interacting left hand or <code>null</code>, if it currently
        /// does not interact.
        /// </summary>
        private Hand leftHand;

        /// <summary>
        /// Distance between the right hand and the city center. Is used to be able to calculate
        /// the current offset vector, independent of the rotation of the city.
        /// </summary>
        private float rightDistance;

        /// <summary>
        /// Distance between the left hand and the city center. Is used to be able to calculate
        /// the current offset vector, independent of the rotation of the city.
        /// </summary>
        private float leftDistance;

        /// <summary>
        /// The angle in degrees between the two vectors counterclockwise:
        /// - Vector from city center to initial grabbed position in city by right hand.
        /// - The vector (1, 0)
        /// 
        /// Is used to be able to calculate the current offset vector, independent of the
        /// rotation of the city.
        /// </summary>
        private float rightAngle;

        /// <summary>
        /// The angle in degrees between the two vectors counterclockwise:
        /// - Vector from city center to initial grabbed position in city by left hand.
        /// - The vector (1, 0)
        /// 
        /// Is used to be able to calculate the current offset vector, independent of the
        /// rotation of the city.
        /// </summary>
        private float leftAngle;

        /// <summary>
        /// The object, the right hand currently interacts with.
        /// </summary>
        private InteractableObject rightInteractable;

        /// <summary>
        /// The object, the left hand currently interacts with.
        /// </summary>
        private InteractableObject leftInteractable;

        /// <summary>
        /// The original scale of the city upon grabbing it.
        /// </summary>
        private Vector3 cityStartGrabScale;

        /// <summary>
        /// The initial distance between both interacting hands. Only used, if both hands
        /// currently interact.
        /// </summary>
        private float startHandDistance;

        /// <summary>
        /// The left grip action for grabbing the whole city.
        /// </summary>
        private SteamVR_Action_Boolean leftGripAction;

        /// <summary>
        /// The right grip action for grabbing the whole city.
        /// </summary>
        private SteamVR_Action_Boolean rightGripAction;

        /// <summary>
        /// The current grab action for grabbing individual parts of the city.
        /// </summary>
        private SteamVR_Action_Single grabAction;

        /// <summary>
        /// The interactable, that is currently attached to grabbing hand. This is not
        /// necessarily the whole city, as parts of the city can be moved individually.
        /// </summary>
        private Interactable attachedInteractable;

        protected sealed override void Awake()
        {
            if (FindObjectOfType<PlayerSettings>().playerInputType != PlayerInputType.VRPlayer)
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

            if (CityTransform != null)
            {
                // Grab, release parts of city
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
                        hoveringInteractable.GetComponent<InteractableObject>().SetGrab(true, true);
                    }
                }
                else if (grabAction.lastAxis >= 0.9f && grabAction.axis < 0.9f && attachedInteractable)
                {
                    Hand theRightHand = Player.instance.rightHand;
                    attachedInteractable.transform.rotation = Quaternion.identity;

                    attachedInteractable.GetComponent<InteractableObject>().SetGrab(false, true);
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
        }

        /// <summary>
        /// Updates data if given hand starts grabbing the given game object.
        /// </summary>
        /// <param name="hand">The grabbing hand.</param>
        /// <param name="go">The grabbed object.</param>
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
                        refInteractable?.SetGrab(true, true);

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

        /// <summary>
        /// Updates data if given hand stops grabbing the given game object.
        /// </summary>
        /// <param name="hand">The releasing hand.</param>
        private void OnStopGrab(Hand hand)
        {
            if (hand == leftHand)
            {
                leftHand = null;
                leftInteractable.SetGrab(false, true);
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
                rightInteractable.SetGrab(false, true);
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
#endif