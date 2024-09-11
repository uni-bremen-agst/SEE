using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// This component should be attached to a gameobject that contains a CC4 avatar with a SkinnedMeshRenderer.
    /// The function of the script is to set up a series of fake Blendshapes, which then can be
    /// addressed by the facial tracker. The values of the Blendshapes will be transferred to
    /// the original blendshapes of the CC4 avatar.
    /// </summary>
    internal class AvatarBlendshapeExpressions : MonoBehaviour
    {
        /// <summary>
        /// The SkinnedMeshRenderer of CC_Base_Body.
        /// </summary>
        private SkinnedMeshRenderer targetSkinnedRenderer;

        /// <summary>
        /// Mesh for baking.
        /// </summary>
        private Mesh bakedMesh;

        /// <summary>
        /// Transform of the CC Base Body.
        /// </summary>
        private Transform ccBaseBody;

        /// <summary>
        /// Constant string literals for the fake Blendshapes and also string literals of the orignal blendshapes
        /// of the CC4 Avatar (cc as prefix).
        /// </summary>
        private const string jawOpen = "Jaw_Open"; // Same for CC Avatar
        private const string ccJawOpen = "Merged_Open_Mouth";

        // Mouth
        private const string mouthPout = "Mouth_Pout";
        private const string ccMouthPuckerUpL = "Mouth_Pucker_Up_L";
        private const string ccMouthPuckerUpR = "Mouth_Pucker_Up_R";
        private const string ccMouthPuckerDownL = "Mouth_Pucker_Down_L";
        private const string ccMouthPuckerDownR = "Mouth_Pucker_Down_R";
        private const string mouthUpperInside = "Mouth_Upper_Inside";
        private const string ccMouthUpperInsideLeft = "Mouth_Roll_In_Upper_L";
        private const string ccMouthUpperInsideRight = "Mouth_Roll_In_Upper_R";
        private const string mouthLowerInside = "Mouth_Lower_Inside";
        private const string ccMouthLowerInsideLeft = "Mouth_Roll_In_Lower_L";
        private const string ccMouthLowerInsideRight = "Mouth_Roll_In_Lower_R";

        // Tongue
        private const string ccTongueUp = "Tongue_Tip_Up";
        private const string ccTongueDown = "Tongue_Tip_Down";
        private const string tongueUpLeftMorph = "Tongue_UpLeft_Morph";
        private const string ccTongueBulgeLeft = "Tongue_Bulge_L"; // + Tongue UP
        private const string tongueUpRightMorph = "Tongue_UpRight_Morph";

        private const string ccTongueBulgeRight = "Tongue_Bulge_R"; // + Tongue UP
        private const string tongueDownLeftMorph = "Tongue_DownLeft_Morph"; // + BulgeLeft && Tongue Down
        private const string tongueDownRightMorph = "Tongue_DownRight_Morph"; // + BulgeRight && Tongue Down

        // Cheeks
        private const string cheekSuck = "Cheek_Suck";
        private const string ccCheekSuckLeft = "Cheek_Suck_L";
        private const string ccCheekSuckRight = "Cheek_Suck_R";

        /// <summary>
        /// Initially set to true so that the code within the update() method is only executed after all necessary
        /// components have been initialised.
        /// </summary>
        private bool waitForInit = true;

        /// <summary>
        /// Dictionary containing HTC Vive Facetracker Blendshape names as keys
        /// and corresponding CC4 Blendshape names as values.
        /// </summary>
        private readonly static Dictionary<string, string> blendshapeMappings = new Dictionary<string, string>
        {
            // Jaw
            { "Jaw_Left", "Jaw_L" },
            { "Jaw_Right", "Jaw_R" },
            { "Jaw_Forward", "Jaw_Forward" }, // Same for CC Avatar
            { "Jaw_Open", "Jaw_Open" },

            // Mouth
            { "Mouth_Ape_Shape", "Mouth_Ape_Shape" },
            { "Mouth_Upper_Left", "Mouth_Upper_L" },
            { "Mouth_Upper_Right", "Mouth_Upper_R" },
            { "Mouth_Lower_Left", "Mouth_Lower_L" },
            { "Mouth_Lower_Right", "Mouth_Lower_R" },
            { "Mouth_Upper_Overturn", "Mouth_Upper_Overturn" },
            { "Mouth_Lower_Overturn", "Mouth_Lower_Overturn" },
            { "Mouth_Pout", "Mouth_Pout" },
            { "Mouth_Smile_Left", "Mouth_Smile_L" },
            { "Mouth_Smile_Right", "Mouth_Smile_R" },
            { "Mouth_Sad_Left", "Mouth_Frown_L" },
            { "Mouth_Sad_Right", "Mouth_Frown_R" },
            { "Mouth_Upper_UpLeft", "Mouth_Up_Upper_L" },
            { "Mouth_Upper_UpRight", "Mouth_Up_Upper_R" },
            { "Mouth_Lower_DownLeft", "Mouth_Down_Lower_L" },
            { "Mouth_Lower_DownRight", "Mouth_Down_Lower_R" },
            { "Mouth_Upper_Inside", "Mouth_Upper_Inside" },
            { "Mouth_Lower_Inside", "Mouth_Lower_Inside" },
            { "Mouth_Lower_Overlay", "Mouth_Lower_Overlay" },

            // Tongue
            { "Tongue_LongStep1", "Tongue_Up" },
            { "Tongue_LongStep2", "Tongue_Out" },
            { "Tongue_Left", "Tongue_L" },
            { "Tongue_Right", "Tongue_R" },
            { "Tongue_Up", "Tongue_Tip_Up" }, // Same for CC Avatar
            { "Tongue_Down", "Tongue_Tip_Down" }, // Same for CC Avatar
            { "Tongue_Roll", "Tongue_Roll" }, // Same for CC Avatar
            { "Tongue_UpLeft_Morph", "Tongue_UpLeft_Morph" },
            { "Tongue_UpRight_Morph", "Tongue_UpRight_Morph" },
            { "Tongue_DownLeft_Morph", "Tongue_DownLeft_Morph" },
            { "Tongue_DownRight_Morph", "Tongue_DownRight_Morph" },

            // Cheeks
            { "Cheek_Puff_Left", "Cheek_Puff_L" },
            { "Cheek_Puff_Right", "Cheek_Puff_R" },
            { "Cheek_Suck", "Cheek_Suck" },

            // Eyes
            { "Eye_Left_Blink", "Eye_Blink_L" },
            { "Eye_Left_Wide", "Eye_Wide_L" },
            { "Eye_Left_Right", "Eye_L_Look_R" },
            { "Eye_Left_Left", "Eye_L_Look_L" },
            { "Eye_Left_Up", "Eye_L_Look_Up" },
            { "Eye_Left_Down", "Eye_L_Look_Down" },
            { "Eye_Right_Blink", "Eye_Blink_R" },
            { "Eye_Right_Wide", "Eye_Wide_R" },
            { "Eye_Right_Right", "Eye_R_Look_R" },
            { "Eye_Right_Left", "Eye_R_Look_L" },
            { "Eye_Right_Up", "Eye_R_Look_Up" },
            { "Eye_Right_Down", "Eye_R_Look_Down" },
            { "Eye_Frown", "Eye_Frown" },
            { "Eye_Left_squeeze", "Eye_Left_squeeze" },
            { "Eye_Right_squeeze", "Eye_Right_squeeze" }
        };

        /// <summary>
        /// Searches for and initialises the SkinnedMeshRenderer. Also adds the necessary components to the
        /// gameobject so that the HTC Facetracker works.
        /// </summary>
        private void Start()
        {
            ccBaseBody = gameObject.transform.Find("CC_Base_Body");
            SkinnedMeshRenderer skinnedMeshRenderer = ccBaseBody.GetComponent<SkinnedMeshRenderer>();
            ccBaseBody.gameObject.AddComponent<AvatarSRanipalLipV2>();
            InitializeBlendshapes(skinnedMeshRenderer);
        }

        /// <summary>
        /// This function creates the fake Blendshapes necessary for the HTC Facial Tracker.
        /// </summary>
        private void InitializeBlendshapes(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            targetSkinnedRenderer = skinnedMeshRenderer;
            bakedMesh = new Mesh();
            targetSkinnedRenderer.BakeMesh(bakedMesh);

            Vector3[] junkData = new Vector3[bakedMesh.vertices.Length];

            foreach (KeyValuePair<string, string> blendShapeEntry in blendshapeMappings)
            {
                if (targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(blendShapeEntry.Key) == -1)
                {
                    targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(blendShapeEntry.Key, 100, junkData, junkData,
                        junkData);
                }
            }
            waitForInit = false;
        }

        /// <summary>
        /// Assigns the values of the fake Blendshapes to the original Blendshapes of the CC4 avatar.
        /// Some keys have multiple values in the composition of the blendshapes and
        /// need special treatment outside the foreach loop.
        /// </summary>
        private void Update()
        {
            if (!waitForInit)
            {
                foreach (KeyValuePair<string, string> blendShapeEntry in blendshapeMappings)
                {
                    targetSkinnedRenderer.SetBlendShapeWeight(
                        targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(blendShapeEntry.Value),
                        targetSkinnedRenderer.GetBlendShapeWeight(
                            targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(blendShapeEntry.Key))
                    );
                }

                float jawOpenValue = targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(jawOpen));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(jawOpen), 0f);
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccJawOpen), jawOpenValue);

                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerUpL),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(mouthPout)));

                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerUpR),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(mouthPout)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerDownL),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(mouthPout)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerDownR),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(mouthPout)));

                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpperInsideLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(mouthUpperInside)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpperInsideRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(mouthUpperInside)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccMouthLowerInsideLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(mouthLowerInside)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccMouthLowerInsideRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(mouthLowerInside)));

                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccTongueBulgeLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(tongueUpLeftMorph)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccTongueUp),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(tongueUpLeftMorph)));

                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccTongueBulgeRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(tongueUpRightMorph)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccTongueUp),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(tongueUpRightMorph)));

                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccTongueBulgeLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(tongueDownLeftMorph)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccTongueDown),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(tongueDownLeftMorph)));

                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccTongueBulgeRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(tongueDownRightMorph)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccTongueDown),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(tongueDownRightMorph)));

                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccCheekSuckLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(cheekSuck)));
                targetSkinnedRenderer.SetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(ccCheekSuckRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(targetSkinnedRenderer.sharedMesh.GetBlendShapeIndex(cheekSuck)));
            }
        }
    }
}