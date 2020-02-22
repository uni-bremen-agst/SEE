using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net
{

    public class ViveController : MonoBehaviour
    {
        public enum ViveControllerHand
        {
            LeftHand,
            RightHand
        }

        public const string NAME_L = "/Player Rig/Interaction Manager/VR Vive-style Controller (Left)";
        public const string NAME_R = "/Player Rig/Interaction Manager/VR Vive-style Controller (Right)";

        [SerializeField] private ViveControllerHand hand;
        public ViveControllerHand Hand { get => hand; }

        private Transform controllerTransform;

        void Start()
        {
            if (!GetComponent<ViewContainer>().IsOwner())
            {
                Destroy(this);
                return;
            }

            if (Hand == ViveControllerHand.LeftHand)
            {
                GameObject leftController = GameObject.Find(NAME_L);
                if (leftController)
                {
                    controllerTransform = leftController.transform;
                }
                else
                {
                    Debug.LogError("Left controller could not be found! Is it enabled?");
                }
            }
            else
            {
                GameObject rightController = GameObject.Find(NAME_R);
                if (rightController)
                {
                    controllerTransform = rightController.transform;
                }
                else
                {
                }
            }
#if UNITY_EDITOR
            if (controllerTransform == null)
            {
                Debug.LogError((hand == ViveControllerHand.LeftHand ? "Left" : "Right") + " controller could not be found! Is it enabled?");
                Destroy(this);
            }
#endif
        }

        void Update()
        {
            transform.position = controllerTransform.position;
            transform.rotation = controllerTransform.rotation;
        }
    }

}
