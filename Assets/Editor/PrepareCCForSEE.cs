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
    ///
    /// The following components are already attached to the BaseAvatar and
    /// will be inherited from it (the ones that need configuration are marked
    /// with a *; these cannot be configured in the BaseAvatar because - generally -
    /// they depend upon the bones and bones are added through the CC5 avatar
    /// sceleton; those not needing additional configuration are marked by -):
    ///
    /// - <see cref="UnityEngine.CapsuleCollider"/> (currently disabled)
    /// * <see cref="CrazyMinnow.SALSA.DissonanceLink.SalsaDissonanceLink"/>
    /// - <see cref="Dissonance.Demo.TriggerVisualizer"/> (currently disabled and not used)
    /// - <see cref="Dissonance.VoiceBroadcastTrigger"/>
    /// - <see cref="Dissonance.Integrations.Unity_NFGO.NfgoPlayer"/>
    /// * <see cref="RootMotion.FinalIK.AimIK"/>
    /// * <see cref="RootMotion.FinalIK.FullBodyBipedIK"/>
    /// * <see cref="RootMotion.FinalIK.LookAtIK"/>
    /// - <see cref="SEE.Game.Avatars.ActionUnits"/>
    /// - <see cref="SEE.Game.Avatars.AvatarAdapter"/>
    /// - <see cref="SEE.Game.Avatars.AvatarAimingSystem"/>
    /// - <see cref="SEE.Game.Avatars.AvatarHandAnimationsSync"/>
    /// - <see cref="SEE.Game.Avatars.AvatarMovementAnimator"/>
    /// - <see cref="SEE.Game.Avatars.BodyAnimator"/>
    /// - <see cref="SEE.Net.ClientNetworkTransform"/>
    /// - <see cref="Unity.Netcode.NetworkObject"/>
    /// * <see cref="UnityEngine.Animator"/>
    /// - <see cref="UnityEngine.CharacterController"/>
    ///
    /// The following components should be added to the avatar prefab because they
    /// are not inherited from the BaseAvatar (again because they depend upon bones):
    ///
    /// <see cref="FACSnimator"/> (located in plugin FACSvatar)
    ///
    /// There is a Unity Editor menu entry "GameObject>Grazy Minnow Studio>SALSA LipSync>One-Clicks>Reallusion>CC4."
    /// that allows a user to configure the SALSA components for the selected avatar.
    /// This entry will call <see cref="CrazyMinnow.SALSA.OneClicks.OneClickCCEditor.OneClickSetup_CC4GameReady"/>.
    /// To call this method, we need an assembly reference to
    /// Assets/Plugins/Crazy Minnow Studio/SALSA LipSync/Editor/SALSALipSyncOneClicksEditor.asmdef
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
                GameObject avatar = Selection.activeGameObject;
                CrazyMinnow.SALSA.OneClicks.OneClickCCEditor.OneClickSetup_CC4GameReady();
                PrepareLookAtIK(avatar);
                PrepareAimIK(avatar);
                PrepareFullBodyBipedIK(avatar);
            }
            else
            {
                Debug.LogError("You must select the root of CC5 avatar prefab.\n");
            }
        }

        /// <summary>
        /// Configures the <see cref="LookAtIK"/> component of the <paramref name="avatar"/>.
        /// </summary>
        /// <param name="avatar">The root game object representing the avatar.</param>
        private static void PrepareLookAtIK(GameObject avatar)
        {
            if (avatar.TryGetComponentOrLog(out LookAtIK lookAtIK))
            {
                // Configure component Look At IK by adding child game object
                // CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head
                // to attribute Head.
                Transform head = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head");
                lookAtIK.solver.head.transform = head;
            }
        }

        /// <summary>
        /// Name of the game object representing the aim target for <see cref="AimIK"/>.
        /// </summary>
        private const string aimTargetName = "AimTarget";

        /// <summary>
        /// Configures the <see cref="AimIK"/> component of the <paramref name="avatar"/>.
        /// </summary>
        /// <param name="avatar">The root game object representing the avatar.</param>
        private static void PrepareAimIK(GameObject avatar)
        {
            Transform aimTarget = PrepareAimTarget(avatar);

            if (avatar.TryGetComponentOrLog(out AimIK aimIK))
            {
                aimIK.solver.target = aimTarget;

                // Set the bones. AddBone adds at the end and would leave the first five bones undefined.
                aimIK.solver.bones[0].transform = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01");
                aimIK.solver.bones[1].transform = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02");
                aimIK.solver.bones[2].transform = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle");
                aimIK.solver.bones[3].transform = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm");
                aimIK.solver.bones[4].transform = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm");
                // Set the weight of each bone.
                aimIK.solver.bones[0].weight = 0.3f;
                aimIK.solver.bones[1].weight = 0.5f;
                aimIK.solver.bones[2].weight = 0.8f;
                aimIK.solver.bones[3].weight = 0.846f;
                aimIK.solver.bones[4].weight = 1.0f;
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

        /// <summary>
        /// Configures the <see cref="FullBodyBipedIK"/> component of the <paramref name="avatar"/>.
        /// </summary>
        /// <param name="avatar">The root game object representing the avatar.</param>
        private static void PrepareFullBodyBipedIK(GameObject avatar)
        {
            if (avatar.TryGetComponentOrLog(out FullBodyBipedIK ik))
            {
                ik.references.pelvis = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip");

                ik.references.leftThigh = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Pelvis/CC_Base_L_Thigh");
                ik.references.leftCalf = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Pelvis/CC_Base_L_Thigh/CC_Base_L_Calf");
                ik.references.leftFoot = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Pelvis/CC_Base_L_Thigh/CC_Base_L_Calf/CC_Base_L_Foot");

                ik.references.rightThigh = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Pelvis/CC_Base_R_Thigh");
                ik.references.rightCalf = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Pelvis/CC_Base_R_Thigh/CC_Base_R_Calf");
                ik.references.rightFoot = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Pelvis/CC_Base_R_Thigh/CC_Base_R_Calf/CC_Base_R_Foot");

                ik.references.leftUpperArm = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm");
                ik.references.leftForearm = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm");
                ik.references.leftHand = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_L_Clavicle/CC_Base_L_Upperarm/CC_Base_L_Forearm/CC_Base_L_Hand");

                ik.references.rightUpperArm = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm");
                ik.references.rightForearm = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm");
                ik.references.rightHand = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand");

                ik.references.head = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head");

                ik.references.spine[0] = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist");
                ik.references.spine[1] = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01");

                ik.references.root = MustFind(avatar, "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01");
            }
        }

        /// <summary>
        /// Returns the descendant of <paramref name="avatar"/> under the
        /// given <paramref name="path"/>.
        /// </summary>
        /// <param name="avatar">The root game object representing the avatar.</param>
        /// <param name="path">The hierarchical name of the descendant.</param>
        /// <returns>The resulting descendant.</returns>
        /// <exception cref="Exception">Thrown if there is no such descendant.</exception>
        private static Transform MustFind(GameObject avatar, string path)
        {
            Transform result = avatar.transform.Find(path);
            if (result == null)
            {
                throw new Exception($"Avatar {avatar.name} does not have a descendant named {path}");
            }
            return result;
        }
    }
}

#endif