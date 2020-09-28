using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{

    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Plane))]
    public class XRNavigationAction : CityAction
    {
        private enum XRNavigationMode
        {
            None,
            MoveOnly,
            MoveRotateZoom
        }

        private Transform cityTransform;

        private XRNavigationMode mode;
        private Hand rightHand;
        private Hand leftHand;
        private GrabbableObject rightGrabbableObject;
        private GrabbableObject leftGrabbableObject;
        private float leftAngle;
        private float leftDistance;
        private float rightAngle;
        private float rightDistance;

        private SteamVR_Action_Boolean leftGripAction;
        private SteamVR_Action_Boolean rightGripAction;

        [Tooltip("The area in which to draw the code city")]
        [SerializeField] private Plane portalPlane;

        [Tooltip("The unique ID used for network synchronization. This must be set via inspector to ensure that every client will have the correct ID assigned to the appropriate NavigationAction!")]
        [SerializeField] private int id;

        private static readonly Dictionary<int, XRNavigationAction> navigationActionDict = new Dictionary<int, XRNavigationAction>(2);
        public static XRNavigationAction Get(int id)
        {
            bool result = navigationActionDict.TryGetValue(id, out XRNavigationAction value);
            if (result)
            {
                return value;
            }
            else
            {
                Debug.LogWarning("ID does not match any NavigationAction!");
                return null;
            }
        }



        private void Start()
        {
            if (FindObjectOfType<PlayerSettings>().playerInputType != PlayerSettings.PlayerInputType.VR)
            {
                Destroy(this);
                return;
            }

            UnityEngine.Assertions.Assert.IsNotNull(portalPlane, "The culling plane must not be null!");
            UnityEngine.Assertions.Assert.IsTrue(!navigationActionDict.ContainsKey(id), "A unique ID must be assigned to every NavigationAction!");
            navigationActionDict.Add(id, this);

            cityTransform = GetCityRootNode(gameObject);
            UnityEngine.Assertions.Assert.IsNotNull(cityTransform, "This XRNavigationAction is not attached to a code city!");
            Debug.LogFormat("NavigationAction controls {0}.\n", cityTransform.name);

            SteamVR_Input.GetActionSet("default").Activate();
            leftGripAction = SteamVR_Input.GetBooleanAction("default", "LGrip");
            rightGripAction = SteamVR_Input.GetBooleanAction("default", "RGrip");

            mode = XRNavigationMode.None;
            leftHand = null;
            rightHand = null;
            leftGrabbableObject = null;
            rightGrabbableObject = null;
        }

        private void Update()
        {
            if (leftGripAction.stateUp && leftHand)
            {
                leftHand = null;
                leftGrabbableObject = null;
                mode -= 1;
            }
            if (rightGripAction.stateUp && rightHand)
            {
                rightHand = null;
                rightGrabbableObject = null;
                mode -= 1;
            }

            if (mode != XRNavigationMode.None)
            {
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

                float cityAngleOffset = Mathf.Deg2Rad * cityTransform.rotation.eulerAngles.y;
                fstAngle -= cityAngleOffset;
                Vector2 fstOffsetV2 = new Vector2(Mathf.Cos(fstAngle), Mathf.Sin(fstAngle)) * fstDistance;
                Vector2 fstHandPositionV2 = fstHand.transform.position.XZ();
                Vector2 fstNewCityPositionV2 = fstHandPositionV2 - fstOffsetV2;
                cityTransform.position = new Vector3(fstNewCityPositionV2.x, cityTransform.position.y, fstNewCityPositionV2.y);

                // align with second hand

                if (mode == XRNavigationMode.MoveRotateZoom)
                {
                    if (leftHand.transform.position.x > rightHand.transform.position.x)
                    {
                        int bh = 0;
                    }
                    float sndAngle = leftAngle - cityAngleOffset;
                    Vector2 sndOffsetV2 = new Vector2(Mathf.Cos(sndAngle), Mathf.Sin(sndAngle)) * leftDistance;
                    Vector2 estimatedSndHandPositionV2 = fstNewCityPositionV2 + sndOffsetV2;
                    Vector2 actualSndHandPositionV2 = leftHand.transform.position.XZ();
                    Vector2 v0 = actualSndHandPositionV2 - fstHandPositionV2;
                    Vector2 v1 = estimatedSndHandPositionV2 - fstHandPositionV2;
                    float a = Vector2.SignedAngle(v0, v1);
                    cityTransform.RotateAround(fstHand.transform.position, Vector3.up, a);
                }
            }
        }

        public void OnStartGrab(Hand hand, GrabbableObject grabbableObject)
        {
            GrabTypes grabType = hand.GetGrabStarting();
            if (grabType == GrabTypes.Grip)
            {
                ref Hand refHand = ref rightHand;
                ref GrabbableObject refGrabbableObject = ref rightGrabbableObject;
                ref float refDistance = ref rightDistance;
                ref float refAngle = ref rightAngle;

                if (hand.handType == SteamVR_Input_Sources.LeftHand)
                {
                    refHand = ref leftHand;
                    refGrabbableObject = ref leftGrabbableObject;
                    refDistance = ref leftDistance;
                    refAngle = ref leftAngle;
                }

                if (!refHand && !refGrabbableObject)
                {
                    refHand = hand;
                    refGrabbableObject = grabbableObject;

                    Vector2 toHandV2 = hand.transform.position.XZ() - cityTransform.position.XZ();
                    refDistance = toHandV2.magnitude;

                    toHandV2.Normalize();
                    refAngle = Mathf.Atan2(toHandV2.y, toHandV2.x) + Mathf.Deg2Rad * cityTransform.rotation.eulerAngles.y;

                    mode += 1;
                }
            }
        }
    }

}
