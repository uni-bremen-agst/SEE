#if UNITY_EDITOR

using CrazyMinnow.SALSA;
using RootMotion.FinalIK;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using System;
using System.Linq;
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
    /// An assembly reference to FACSvatar.asmdef is needed.
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
                PrepareSALSA(avatar);
                PrepareLookAtIK(avatar);
                PrepareAimIK(avatar);
                PrepareFullBodyBipedIK(avatar);
                TurnOffIsHead(avatar);
                PrepareAnimator(avatar);
                PrepareFACSnimator(avatar);

                // Mark the scene as dirty so the change is saved.
                // EditorUtility.SetDirty(avatar);
                SavePrefabChanges(avatar);
            }
            else
            {
                Debug.LogError("You must select the root of CC5 avatar prefab.\n");
            }

            // Saves the changes made to the prefab.
            static void SavePrefabChanges(GameObject avatar)
            {
                // 1. Tell Unity that the object has changed
                EditorUtility.SetDirty(avatar);

                // 2. Specifically tell the Prefab system to record the change
                // This is the modern replacement for older "ReplacePrefab" methods
                PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);

                // 3. (Optional) Force an immediate save to disk
                // Use this if you want the *.prefab file to update immediately
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Prepares SALSA for <paramref name="avatar"/>.
        /// </summary>
        /// <param name="avatar">The root game object representing the avatar.</param>
        private static void PrepareSALSA(GameObject avatar)
        {
            CrazyMinnow.SALSA.OneClicks.OneClickCCEditor.OneClickSetup_CC4();

            // After the one-click setup of SALSA, we need to make the following further
            // adjustments.

            if (avatar.TryGetComponentOrLog(out Salsa salsa))
            {
                // In component SALSA under References, toggle on Use External Analysis.
                salsa.useExternalAnalysis = true;

                // In component SALSA under References, set Audio Source to None.
                salsa.audioSrc = null;

                // If this is off, the mouth of a CC5 avatar will be open all the time.
                salsa.scaleExternalAnalysis = true;

                Debug.Log($"[{nameof(PrepareCCForSEE)}({avatar.name})] Configured SALSA for external analysis.\n");
            }
            // Remove component Audio Source from the avatar.
            if (avatar.TryGetComponent(out AudioSource audioSource))
            {
                Destroyer.Destroy(audioSource);
            }

            Debug.Log($"[{nameof(PrepareCCForSEE)}({avatar.name})] Configured SALSA\n");
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
                lookAtIK.solver.head.transform = MustFind(avatar, headName);
                Debug.Log($"[{nameof(PrepareCCForSEE)}({avatar.name})] Configured {nameof(LookAtIK)}\n");
            }
        }

        /// <summary>
        /// Name of the game object representing the aim target for <see cref="AimIK"/>.
        /// </summary>
        private const string aimTargetName = "AimTarget";
        /// <summary>
        /// Name of the game object representing the aim transform for <see cref="AimIK"/>.
        /// </summary>
        private const string aimTransformName = "AimTransform";

        /// <summary>
        /// Name of the base body game object of the avatar. It holds the <see cref="SkinnedMeshRenderer"/>,
        /// and <see cref="FACSnimator"/> among other CC5 components. It is an immediate child of the
        /// avatar root.
        /// </summary>
        private const string baseBodyName = "CC_Base_Body";

        /// <summary>
        /// Name of the bone root of the avatar. It is an immediate child of the avatar root.
        /// </summary>
        private const string boneRootName = "CC_Base_BoneRoot";

        /// <summary>
        /// Name of the hip bone of the avatar.
        /// </summary>
        private const string hipName = boneRootName + "/CC_Base_Hip";

        /// <summary>
        /// Name of the waist of the avatar.
        /// </summary>
        private const string waistName = hipName + "/CC_Base_Waist";

        /// <summary>
        /// Name of the lower spine of the avatar.
        /// </summary>
        private const string spine1Name = waistName + "/CC_Base_Spine01";

        /// <summary>
        /// Name of the upper spine of the avatar.
        /// </summary>
        private const string spine2Name = spine1Name + "/CC_Base_Spine02";

        /// <summary>
        /// Name of the second neck twist bone of the avatar.
        /// </summary>
        private const string neckTwist02Name = spine2Name + "/CC_Base_NeckTwist01/CC_Base_NeckTwist02";

        /// <summary>
        /// Name of the head bone of the avatar.
        /// </summary>
        private const string headName = neckTwist02Name + "/CC_Base_Head";

        /// <summary>
        /// Name of left clavicle bone of the avatar.
        /// </summary>
        private const string leftClavicleName = spine2Name + "/CC_Base_L_Clavicle";

        /// <summary>
        /// Name of left upperarm bone of the avatar.
        /// </summary>
        private const string leftUpperArmName = leftClavicleName + "/CC_Base_L_Upperarm";

        /// <summary>
        /// Name of left forearm bone of the avatar.
        /// </summary>
        private const string leftForeArmName = leftUpperArmName + "/CC_Base_L_Forearm";

        /// <summary>
        /// Name of the left hand bone of the avatar.
        /// </summary>
        private const string leftHandName = leftForeArmName + "/CC_Base_L_Hand";

        /// <summary>
        /// Name of right clavicle bone of the avatar.
        /// </summary>
        private const string rightClavicleName = spine2Name + "/CC_Base_R_Clavicle";

        /// <summary>
        /// Name of right upperarm bone of the avatar.
        /// </summary>
        private const string rightUpperArmName = rightClavicleName + "/CC_Base_R_Upperarm";

        /// <summary>
        /// Name of right forearm bone of the avatar.
        /// </summary>
        private const string rightForeArmName = rightUpperArmName + "/CC_Base_R_Forearm";

        /// <summary>
        /// Name of the right hand bone of the avatar.
        /// </summary>
        private const string rightHandName = rightForeArmName + "/CC_Base_R_Hand";

        /// <summary>
        /// Name of the pelvis of the avatar.
        /// </summary>
        private const string pelvisName = hipName + "/CC_Base_Pelvis";

        /// <summary>
        /// Name of the left thigh of the avatar.
        /// </summary>
        private const string leftTighName = pelvisName + "/CC_Base_L_Thigh";

        /// <summary>
        /// Name of the left calf of the avatar.
        /// </summary>
        private const string leftCalfName = leftTighName + "/CC_Base_L_Calf";

        /// <summary>
        /// Name of the left foot of the avatar.
        /// </summary>
        private const string leftFootName = leftCalfName + "/CC_Base_L_Foot";

        /// <summary>
        /// Name of the right thigh of the avatar.
        /// </summary>
        private const string rightTighName = pelvisName + "/CC_Base_R_Thigh";

        /// <summary>
        /// Name of the right calf of the avatar.
        /// </summary>
        private const string rightCalfName = rightTighName + "/CC_Base_R_Calf";

        /// <summary>
        /// Name of the right foot of the avatar.
        /// </summary>
        private const string rightFootName = rightCalfName + "/CC_Base_R_Foot";

        /// <summary>
        /// Configures the <see cref="AimIK"/> component of the <paramref name="avatar"/>.
        /// </summary>
        /// <param name="avatar">The root game object representing the avatar.</param>
        private static void PrepareAimIK(GameObject avatar)
        {
            if (avatar.TryGetComponentOrLog(out AimIK aimIK))
            {
                aimIK.solver.target = PrepareAimTarget(avatar);
                aimIK.solver.transform = PrepareAimTransform(avatar);

                // Set the bones. AddBone adds at the end and would leave the first five bones undefined.
                // All bones need to be direct ancestors of the Aim Transform and sorted in descending order.
                // You can skip bones in the hierarchy and the Aim Transform itself can also be included.
                // The bone hierarchy can not be branched, meaning you cannot assing bones from both hands.
                //aimIK.solver.bones[0].transform = MustFind(avatar, spine1Name);
                //aimIK.solver.bones[1].transform = MustFind(avatar, spine2Name);
                //aimIK.solver.bones[2].transform = MustFind(avatar, clavicleName);
                //aimIK.solver.bones[3].transform = MustFind(avatar, upperArmName);
                //aimIK.solver.bones[4].transform = MustFind(avatar, foreArm);
                aimIK.solver.bones[0].transform = MustFind(avatar, spine1Name);
                aimIK.solver.bones[1].transform = MustFind(avatar, spine2Name);
                aimIK.solver.bones[2].transform = MustFind(avatar, rightUpperArmName);
                aimIK.solver.bones[3].transform = MustFind(avatar, rightForeArmName);
                aimIK.solver.bones[4].transform = MustFind(avatar, rightHandName);
                // Set the weight of each bone.
                // Bone weight determines how strongly it is used in bending the hierarchy
                aimIK.solver.bones[0].weight = 0.3f;
                aimIK.solver.bones[1].weight = 0.5f;
                aimIK.solver.bones[2].weight = 0.8f;
                aimIK.solver.bones[3].weight = 0.846f;
                aimIK.solver.bones[4].weight = 1.0f;

                Debug.Log($"[{nameof(PrepareCCForSEE)}({avatar.name})] Configured {nameof(AimIK)}\n");
            }

            // Prepares and returns AimTarget.
            static Transform PrepareAimTarget(GameObject avatar)
            {
                Transform aimTarget = avatar.transform.Find(aimTargetName);
                if (aimTarget == null)
                {
                    // None found. We create one.
                    GameObject goAimTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    aimTarget = goAimTarget.transform;
                    aimTarget.name = aimTargetName;
                    aimTarget.transform.SetParent(avatar.transform);
                }
                aimTarget.position = new(1.873f, 1.604f, -0.125f);
                aimTarget.eulerAngles = Vector3.zero;
                aimTarget.localScale = 0.02f * Vector3.one;
                // should not be visible
                Renderer renderer = aimTarget.GetComponent<Renderer>();
                renderer.enabled = false;
                // should not collide
                Collider collider = aimTarget.GetComponent<Collider>();
                collider.enabled = false;
                aimTarget.gameObject.AddOrGetComponent<ClientNetworkTransform>();
                return aimTarget;
            }

            // Adds and returns a new empty game object AimTransform to the game-object hierarchy for
            // the avatar under the path CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/
            // CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/
            // CC_Base_R_Forearm/CC_Base_R_Hand/AimTransform with (all local units)
            // local position = (0.0153701, 0.1400991, 0.03830782),
            // local scale = (0.008820007, 0.008820003, 0.008820006),
            // local rotation = (-86.34, 51.421, -51.463)`.
            static Transform PrepareAimTransform(GameObject avatar)
            {

                Transform hand = MustFind(avatar, rightHandName);

                // If we already have an AimTransform, we will re-use that.
                Transform aimTransform = hand.Find(aimTransformName);
                if (aimTransform == null)
                {
                    aimTransform = new GameObject() { name = aimTransformName }.transform;
                }

                aimTransform.transform.SetParent(hand);
                // The following are all local space units.
                aimTransform.SetLocalPositionAndRotation(new(0.0153701f, 0.1400991f, 0.03830782f), Quaternion.Euler(-86.34f, 51.421f, -51.463f));
                aimTransform.localScale = new Vector3(0.008820007f, 0.008820003f, 0.008820006f);

                return aimTransform;
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
                ik.references.root = avatar.transform;

                ik.references.pelvis = MustFind(avatar, hipName);

                ik.references.leftThigh = MustFind(avatar, leftTighName);
                ik.references.leftCalf = MustFind(avatar, leftCalfName);
                ik.references.leftFoot = MustFind(avatar, leftFootName);

                ik.references.rightThigh = MustFind(avatar, rightTighName);
                ik.references.rightCalf = MustFind(avatar, rightCalfName);
                ik.references.rightFoot = MustFind(avatar, rightFootName);

                ik.references.leftUpperArm = MustFind(avatar, leftUpperArmName);
                ik.references.leftForearm = MustFind(avatar, leftForeArmName);
                ik.references.leftHand = MustFind(avatar, leftHandName);

                ik.references.rightUpperArm = MustFind(avatar, rightUpperArmName);
                ik.references.rightForearm = MustFind(avatar, rightForeArmName);
                ik.references.rightHand = MustFind(avatar, rightHandName);

                ik.references.head = MustFind(avatar, headName);

                if (ik.references.spine.Count() != 2)
                {
                    ik.references.spine = new Transform[2];
                }
                ik.references.spine[0] = MustFind(avatar, waistName);
                ik.references.spine[1] = MustFind(avatar, spine1Name);

                ik.solver.rootNode = MustFind(avatar, spine1Name);

                // It is not sufficient to just assign ik.references. The solver needs an update, too,
                // which is achieved calling ik.SetReferences.
                ik.SetReferences(ik.references, null);

                Debug.Log($"[{nameof(PrepareCCForSEE)}({avatar.name})] Configured {nameof(FullBodyBipedIK)}\n");
            }
        }

        /// <summary>
        /// Turns off the toggle IsHead in material Std_Skin_Head of the game object CC_Base_Body
        /// of the <paramref name="avatar"/>.
        /// Otherwise the face skin will not be rendered in the Unity editor play mode under Linux.
        /// </summary>
        /// <param name="avatar">The root game object representing the avatar.</param>
        private static void TurnOffIsHead(GameObject avatar)
        {
            const string headMaterial = "Std_Skin_Head";
            const string isHead = "BOOLEAN_IS_HEAD";

            Transform baseBody = MustFind(avatar, baseBodyName);
            if (baseBody.gameObject.TryGetComponentOrLog(out SkinnedMeshRenderer renderer))
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    // Unity appends " (Instance)" to names if you use renderer.materials,
                    // so sharedMaterials is often cleaner for checking names.
                    if (mat.name == headMaterial)
                    {
                        if (mat.HasProperty(isHead))
                        {
                            // Set to False
                            mat.SetInt(isHead, 0);
                        }
                        else
                        {
                            throw new MissingMemberException($"The material {mat.name} on {avatar.name} is missing the required property {isHead}.");
                        }
                        break;
                    }
                }
                Debug.Log($"[{nameof(PrepareCCForSEE)}({avatar.name})] Configured IsHead\n");
            }
        }

        /// <summary>
        /// The Avatar attribute in the <see cref="Animator"/> component must be <Name>Avatar,
        /// which is contained in Assets/Resources/Materials/CC4/<Name>/Prefabs/<Name>.Fbx,
        /// where <name> is <see cref="avatar"/>.name.
        /// </summary>
        /// <param name="avatar">The root game object representing the avatar.</param>
        private static void PrepareAnimator(GameObject avatar)
        {
            if (avatar.TryGetComponentOrLog(out Animator animator))
            {
                // Path to the FBX.
                string fbxPath = $"Assets/Resources/Materials/CC4/{avatar.name}/{avatar.name}.Fbx";

                // Load all assets at that path (FBX files contain Meshes, Materials, and the Avatar).
                UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

                // Find the Avatar asset among the sub-assets.
                Avatar fbxAvatar = allAssets.OfType<Avatar>().FirstOrDefault();

                if (fbxAvatar != null)
                {
                    // Record the object for Undo (standard Editor practice)
                    Undo.RecordObject(animator, "Assign Avatar");

                    // Assign the avatar.
                    animator.avatar = fbxAvatar;
                    Debug.Log($"[{nameof(PrepareCCForSEE)}({avatar.name})] Configured {nameof(Animator)}\n");
                }
                else
                {
                    Debug.LogError($"No Avatar found inside the FBX at {fbxPath}\n");
                }
            }
        }

        /// <summary>
        /// A <see cref="FACSnimator"/> must be added to the avatar's CC_BaseBody game object.
        /// The script is located under Assets/Plugins/FACSvatar/FACSnimator.cs.
        ///
        /// A <see cref="HeadRotatorBone"/> must be added to the avatar's CC_Base_BoneRoot
        /// game object.
        /// The script is located under Assets/Plugins/FACSvatar/HeadRotatorBone.cs.
        /// The following GameObjects must be referenced:
        /// Joint Obj_head: CC_Base_Head
        /// Joint Obj_neck: CC_Base_NeckTwist02
        /// </summary>
        /// <param name="avatar">The root game object representing the avatar.</param>
        private static void PrepareFACSnimator(GameObject avatar)
        {
            MustFind(avatar, baseBodyName).gameObject.AddOrGetComponent<FACSnimator>();

            HeadRotatorBone headRotatorBone = MustFind(avatar, boneRootName).gameObject.AddOrGetComponent<HeadRotatorBone>();
            headRotatorBone.jointObj_head = MustFind(avatar, headName);
            headRotatorBone.jointObj_neck = MustFind(avatar, neckTwist02Name);
            Debug.Log($"[{nameof(PrepareCCForSEE)}({avatar.name})] Configured {nameof(FACSnimator)}\n");
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