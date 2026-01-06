#if UNITY_EDITOR

using RootMotion.FinalIK;
using SEE.GO;
using SEE.Net;
using System;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// Adds a menu item "SEE/Prepare CC5 Avatar" to the Unity Editor to
    /// prepare an avatar imported from Reallusion Character Creator (CC)
    /// to be used in SEE. It adds and wires the necessary components.
    /// </summary>
    public static class PrepareCCForSEE
    {
        /// <summary>
        /// The menu entry to add and wire the necessary components
        /// to an imported CC avatar to be used in SEE. Applies to the
        /// currently selected game object (the root of an avatar).
        /// </summary>
        [MenuItem("SEE/Prepare CC5 Avatar")]
        public static void PrintWorldSpaceTransform()
        {
            if (Selection.activeGameObject != null)
            {
                GameObject go = Selection.activeGameObject;
                PrepareLookAtIK(go);
                PrepareAimIK(go);
            }
            else
            {
                Debug.LogError("You must select the root of CC5 avatar prefab.\n");
            }
        }

        private static void PrepareLookAtIK(GameObject go)
        {
            if (go.TryGetComponentOrLog(out LookAtIK lookAtIK))
            {
                // Configure component Look At IK by adding child game object
                // CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head
                // to attribute Head.
                Transform head = MustFind(go, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head");
                lookAtIK.solver.head.transform = head;
            }
        }

        private const string aimTargetName = "AimTarget";

        private static void PrepareAimIK(GameObject go)
        {
            Transform aimTarget = PrepareAimTarget(go);

            if (go.TryGetComponentOrLog(out AimIK aimIK))
            {
                aimIK.solver.target = aimTarget;
            }

            // Prepares and returns AimTarget.
            static Transform PrepareAimTarget(GameObject go)
            {
                Transform aimTarget = go.transform.Find(aimTargetName);
                if (aimTarget == null)
                {
                    // None found. We create one.
                    GameObject goAimTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    aimTarget = goAimTarget.transform;
                    aimTarget.name = aimTargetName;
                }
                aimTarget.position = new(0.0153701f, 0.1400991f, 0.03830782f);
                aimTarget.eulerAngles = new(-86.34f, 51.421f, -51.463f);
                aimTarget.localScale = new Vector3(0.008820007f, 0.008820003f, 0.008820006f);
                // should not be visible
                Renderer renderer = aimTarget.GetComponent<Renderer>();
                renderer.enabled = false;
                // should not collide
                Collider collider = aimTarget.GetComponent<Collider>();
                collider.enabled = false;
                aimTarget.gameObject.AddOrGetComponent<ClientNetworkTransform>();
                return aimTarget;
            }
        }

        private static Transform MustFind(GameObject go, string path)
        {
            Transform result = go.transform.Find(path);
            if (result == null)
            {
                throw new Exception($"Avatar {go.name} does not have a descendant named {path}");
            }
            return result;
        }
    }
}

#endif