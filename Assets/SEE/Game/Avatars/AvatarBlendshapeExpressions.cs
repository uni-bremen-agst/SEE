using System.Collections;
using UnityEngine;
using UMA.PoseTools;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// This component should be attached to a game object that has a UMARenderer and <see cref="UMAExpressionPlayer"/>.
    /// The function of the script is to set up a series of fake Blendshapes for the UMARenderer, which then can be
    /// addressed by the facial tracker. The values of the Blendshapes will be converted by <see cref="ValueConverter"/>
    /// and transferred to the <see cref="UMAExpressionPlayer"/>.
    /// </summary>
    internal class AvatarBlendshapeExpressions : MonoBehaviour
    {
        /// <summary>
        /// The SkinnedMeshRenderer of the UMARenderer.
        /// </summary>
        private SkinnedMeshRenderer targetSkinnedRenderer;

        /// <summary>
        /// Mesh for baking.
        /// </summary>
        private Mesh bakedMesh;

        /// <summary>
        /// This UMA ExpressionPlayer is controlled by the fake Blendshapes.
        /// </summary>
        private UMAExpressionPlayer expressionPlayer;

        /// <summary>
        /// UMA Renderer.
        /// </summary>
        private Transform umaRenderer;
        
        /// <summary>
        /// Constant string literals for the fake Blendshapes.
        /// </summary>
        // Jaw
        private const string jawLeft = "Jaw_Left";
        private const string jawRight = "Jaw_Right";
        private const string jawForward = "Jaw_Forward";
        private const string jawOpen = "Jaw_Open";

        // Mouth
        private const string mouthApeShape = "Mouth_Ape_Shape";
        private const string mouthUpperLeft = "Mouth_Upper_Left";
        private const string mouthUpperRight = "Mouth_Upper_Right";
        private const string mouthLowerLeft = "Mouth_Lower_Left";
        private const string mouthLowerRight = "Mouth_Lower_Right";
        private const string mouthUpperOverturn = "Mouth_Upper_Overturn";
        private const string mouthLowerOverturn = "Mouth_Lower_Overturn";
        private const string mouthPout = "Mouth_Pout";
        private const string mouthSmileLeft = "Mouth_Smile_Left";
        private const string mouthSmileRight = "Mouth_Smile_Right";
        private const string mouthSadLeft = "Mouth_Sad_Left";
        private const string mouthSadRight = "Mouth_Sad_Right";
        private const string mouthUpperUpLeft = "Mouth_Upper_UpLeft";
        private const string mouthUpperUpRight = "Mouth_Upper_UpRight";
        private const string mouthLowerDownLeft = "Mouth_Lower_DownLeft";
        private const string mouthLowerDownRight = "Mouth_Lower_DownRight";
        private const string mouthUpperInside = "Mouth_Upper_Inside";
        private const string mouthLowerInside = "Mouth_Lower_Inside";
        private const string mouthLowerOverlay = "Mouth_Lower_Overlay";
        
        // Tongue
        private const string tongueLongStep1 = "Tongue_LongStep1";
        private const string tongueLongStep2 = "Tongue_LongStep2";
        private const string tongueLeft = "Tongue_Left";
        private const string tongueRight = "Tongue_Right";
        private const string tongueUp = "Tongue_Up";
        private const string tongueDown = "Tongue_Down";
        private const string tongueRoll = "Tongue_Roll";
        private const string tongueUpLeftMorph = "Tongue_UpLeft_Morph";
        private const string tongueUpRightMorph = "Tongue_UpRight_Morph";
        private const string tongueDownLeftMorph = "Tongue_DownLeft_Morph";
        private const string tongueDownRightMorph = "Tongue_DownRight_Morph";
        
        // Cheeks
        private const string cheekPuffLeft = "Cheek_Puff_Left";
        private const string cheekPuffRight = "Cheek_Puff_Right";
        private const string cheekSuck = "Cheek_Suck";
        
        /// <summary>
        /// Starts a coroutine that waits for components to be generated at runtime.
        /// </summary>
        private void Start()
        {
            StartCoroutine(WaitForComponents());
        }

        /// <summary>
        /// Waits for UMARenderer and UMAExpressionPlayer to be created. Once the UMARenderer and
        /// ExpressionPlayer are found, a script (<see cref="AvatarSRanipalLipV2"/>) for the HTC Facial Tracker
        /// is added to the UMARenderer.
        /// After that, the fake blendshapes will be initialised.
        /// </summary>
        private IEnumerator WaitForComponents()
        {
            yield return new WaitUntil(() => gameObject.transform.Find("UMARenderer") != null &&
                                             gameObject.GetComponent<UMAExpressionPlayer>() != null);
            umaRenderer = gameObject.transform.Find("UMARenderer");
            umaRenderer.gameObject.AddComponent<AvatarSRanipalLipV2>();

            InitializeBlendshapes();
        }

        /// <summary>
        /// This function creates the fake Blendshapes necessary for the HTC Facial Tracker.
        /// </summary>
        private void InitializeBlendshapes()
        {
            expressionPlayer = gameObject.GetComponent<UMAExpressionPlayer>();

            // Assure that Jaw can be used by UMAExpressionPlayer
            expressionPlayer.overrideMecanimJaw = true;
            targetSkinnedRenderer = umaRenderer.GetComponent<SkinnedMeshRenderer>();
            bakedMesh = new Mesh();
            targetSkinnedRenderer.BakeMesh(bakedMesh);

            Vector3[] junkData = new Vector3[bakedMesh.vertices.Length];

            // Setup fake Blendshapes
            // Jaw Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(jawLeft, 100, junkData, junkData, junkData); // Jaw_Left_Right - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(jawRight, 100, junkData, junkData, junkData); // Jaw_Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(jawForward, 100, junkData, junkData, junkData); //  Jaw_Forward_Back - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(jawOpen, 100, junkData, junkData, junkData); // Jaw_Open_Close - range (0 - 1)

            // Mouth Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthApeShape, 100, junkData, junkData, junkData); // Drag whole mouth down FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperLeft, 100, junkData, junkData, junkData); // Mouth Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperRight, 100, junkData, junkData, junkData); // Mouth Left_Right - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerLeft, 100, junkData, junkData, junkData); // Mouth Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerRight, 100, junkData, junkData, junkData); // Mouth Left_Right - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperOverturn, 100, junkData, junkData, junkData); // FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerOverturn, 100, junkData, junkData, junkData); // FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthPout, 100, junkData, junkData, junkData); // Mouth Narrow_Pucker (but only (0-1)) FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthSmileLeft, 100, junkData, junkData, junkData); // Left Mouth Smile_Frown (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthSmileRight, 100, junkData, junkData, junkData); // Right Mouth Smile_Frown (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthSadLeft, 100, junkData, junkData, junkData); // Left Mouth Smile_Frown (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthSadRight, 100, junkData, junkData, junkData); // Left Mouth Smile_Frown (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperUpLeft, 100, junkData, junkData, junkData); // Left Upper Lip Up_Down - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperUpRight, 100, junkData, junkData, junkData); // Right Upper Lip Up_Down - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerDownLeft, 100, junkData, junkData, junkData); // Left Lower Lip Up_Down - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerDownRight, 100, junkData, junkData, junkData); // Right Lower Lip Up_Down - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperInside, 100, junkData, junkData, junkData); // Left Upper Lip Up_Down && Right Upper Lip Up_Down (-1 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerInside, 100, junkData, junkData, junkData); // Left Lower Lip Up_Down && Right Lower Lip Up_Down (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerOverlay, 100, junkData, junkData, junkData); // Maybe Jaw Close with range (-1 - 0) FIXME

            // Tongue Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueLongStep1, 100, junkData, junkData, junkData); // Lift tongue slightly FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueLongStep2, 100, junkData, junkData, junkData); // Tongue Out - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueLeft, 100, junkData, junkData, junkData); // Tongue Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueRight, 100, junkData, junkData, junkData); // Tongue Left_Right - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueUp, 100, junkData, junkData, junkData); // Tongue Up_Down - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueDown, 100, junkData, junkData, junkData); // Tongue Up_Down - range (1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueRoll, 100, junkData, junkData, junkData); // Tongue Curl - range (0-1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueUpLeftMorph, 100, junkData, junkData, junkData); // FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueUpRightMorph, 100, junkData, junkData, junkData); // FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueDownLeftMorph, 100, junkData, junkData, junkData); // FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueDownRightMorph, 100, junkData, junkData, junkData); // FIXME

            // Cheek Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(cheekPuffLeft, 100, junkData, junkData, junkData); // Left Cheek Puff_Squint - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(cheekPuffRight, 100, junkData, junkData, junkData); // Right Cheek Puff_Squint - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(cheekSuck, 100, junkData, junkData, junkData); // Maybe "Left Cheek Puff_Squint && Right Cheek Puff_Squint" - range (-1 - 0) FIXME
        }

        /// <summary>
        /// This method transfers the converted values of the fake Blendshapes to the UMAExpressionPlayer.
        /// </summary>
        private void Update()
        {
            if (expressionPlayer != null)
            {
                // JAW
                float jawOpenClose = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(jawOpen));
                expressionPlayer.jawOpen_Close = ValueConverter(jawOpenClose, true);

                float jawLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.jawLeft));
                float jawRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.jawRight));
                if (jawLeft >= jawRight)
                {
                    expressionPlayer.jawLeft_Right = ValueConverter(jawLeft - jawRight, true);
                }
                else
                {
                    expressionPlayer.jawLeft_Right = ValueConverter(jawRight - jawLeft, false);
                }

                // JAW & MOUTH
                float jawForward = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.jawForward));
                float mouthOverlay = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthLowerOverlay));
                if (jawForward >= mouthOverlay)
                {
                    expressionPlayer.jawForward_Back = ValueConverter(jawForward, true);
                }
                else
                {
                    expressionPlayer.jawForward_Back = ValueConverter(mouthOverlay, false); //FIXME: maybe not representable with UMA.
                }

                // Mouth
                float mouthApeShape = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthApeShape));
                expressionPlayer.mouthUp_Down = ValueConverter(mouthApeShape, false);

                float mouthUpperLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthUpperLeft));
                float mouthLowerLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthLowerLeft));
                float mouthUpperRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthUpperRight));
                float mouthLowerRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthLowerRight));

                const int divideByTwo = 2;
                if (mouthUpperLeft >= mouthLowerLeft && mouthUpperLeft >= mouthUpperRight && mouthUpperLeft >= mouthLowerRight)
                {
                    expressionPlayer.mouthLeft_Right = ValueConverter(mouthUpperLeft - (mouthUpperRight + mouthLowerRight) / divideByTwo, true);
                }
                else if (mouthLowerLeft >= mouthUpperLeft && mouthLowerLeft >= mouthUpperRight && mouthLowerLeft >= mouthLowerRight)
                {
                    expressionPlayer.mouthLeft_Right = ValueConverter(mouthLowerLeft - (mouthUpperRight + mouthLowerRight) / divideByTwo, true);
                }
                else if (mouthUpperRight >= mouthUpperLeft && mouthUpperRight >= mouthLowerLeft && mouthUpperRight >= mouthLowerRight)
                {
                    expressionPlayer.mouthLeft_Right = ValueConverter(mouthUpperRight - (mouthUpperLeft + mouthLowerLeft) / divideByTwo, false);
                }
                else
                {
                    expressionPlayer.mouthLeft_Right = ValueConverter(mouthLowerRight - (mouthUpperLeft + mouthLowerLeft) / divideByTwo, false);
                }

                // FIXME: maybe not representable with UMA.
                //float mouthUpperOverturn = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperOverturn));
                //float mouthLowerOverturn = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerOverturn));

                float mouthPout = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthPout));
                expressionPlayer.mouthNarrow_Pucker = ValueConverter(mouthPout, true);

                float mouthSmileLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthSmileLeft));
                float mouthSadLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthSadLeft));
                if (mouthSmileLeft >= mouthSadLeft)
                {
                    expressionPlayer.leftMouthSmile_Frown = ValueConverter(mouthSmileLeft - mouthSadLeft, true);
                }
                else
                {
                    expressionPlayer.leftMouthSmile_Frown = ValueConverter(mouthSadLeft - mouthSmileLeft, false);
                }

                float mouthSmileRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthSmileRight));
                float mouthSadRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthSadRight));
                if (mouthSmileRight >= mouthSadRight)
                {
                    expressionPlayer.rightMouthSmile_Frown = ValueConverter(mouthSmileRight - mouthSadRight, true);
                }
                else
                {
                    expressionPlayer.rightMouthSmile_Frown = ValueConverter(mouthSadRight - mouthSmileRight, false);
                }

                float mouthUpperUpLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthUpperUpLeft));
                expressionPlayer.leftUpperLipUp_Down = ValueConverter(mouthUpperUpLeft, true);

                float mouthUpperUpRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthUpperUpRight));
                expressionPlayer.rightUpperLipUp_Down = ValueConverter(mouthUpperUpRight, true);

                float mouthLowerDownLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthLowerDownLeft));
                expressionPlayer.leftLowerLipUp_Down = ValueConverter(mouthLowerDownLeft, false);

                float mouthLowerDownRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.mouthLowerDownRight));
                expressionPlayer.rightLowerLipUp_Down = ValueConverter(mouthLowerDownRight, false);

                // FIXME: maybe not representable with UMA.
                //float mouthUpperInside = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperInside));
                //float mouthLowerInside = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerInside));

                // Tongue
                float tongueLongStep1 = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.tongueLongStep1));
                float tongueLongStep2 = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.tongueLongStep2));
                if (tongueLongStep1 >= tongueLongStep2)
                {
                    expressionPlayer.tongueOut = ValueConverter(tongueLongStep1, false); //Is not representable in UMA
                }
                else
                {
                    expressionPlayer.tongueOut = ValueConverter(tongueLongStep2, true);
                }

                float tongueLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.tongueLeft));
                float tongueRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.tongueRight));
                if (tongueLeft >= tongueRight)
                {
                    expressionPlayer.tongueLeft_Right = ValueConverter(tongueLeft - tongueRight, true);
                }
                else
                {
                    expressionPlayer.tongueLeft_Right = ValueConverter(tongueRight - tongueLeft, false);
                }

                float tongueUp = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.tongueUp));
                float tongueDown = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.tongueDown));
                if (tongueUp >= tongueDown)
                {
                    expressionPlayer.tongueUp_Down = ValueConverter(tongueUp - tongueDown, true);
                }
                else
                {
                    expressionPlayer.tongueUp_Down = ValueConverter(tongueDown - tongueUp, false);
                }

                float tongueRoll = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.tongueRoll));
                expressionPlayer.tongueCurl = ValueConverter(tongueRoll, true);

                // FIXME: maybe not representable with UMA.
                //float tongueUpLeftMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueUpLeftMorph));
                //float tongueUpRightMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueUpRightMorph));
                //float tongueDownLeftMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueDownLeftMorph));
                //float tongueDownRightMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueDownRightMorph));

                // Cheeks
                float cheekPuffLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.cheekPuffLeft));
                float cheekPuffRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(AvatarBlendshapeExpressions.cheekPuffRight));
                float cheeckSuck = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(cheekSuck));
                if (cheekPuffLeft >= cheeckSuck && cheekPuffRight >= cheeckSuck)
                {
                    expressionPlayer.leftCheekPuff_Squint = ValueConverter(cheekPuffLeft, true);
                    expressionPlayer.rightCheekPuff_Squint = ValueConverter(cheekPuffRight, true);
                }
                else
                {
                    expressionPlayer.leftCheekPuff_Squint = ValueConverter(cheeckSuck, false);
                    expressionPlayer.rightCheekPuff_Squint = ValueConverter(cheeckSuck, false);
                }
            }
        }

        /// <summary>
        /// Converts a number to a different range. Either positive or negative.
        /// Blendshapes have usually a range from [0 - 100] and need to be converted to match the range of the
        /// UMAExpressionPlayer that is [-1 - 1]. Therefore the conversion factor of 0.01f.
        /// </summary>
        /// <param name="param">Value to be converted.</param>
        /// <param name="negOrPos">Negative or positive conversion.</param>
        /// <returns>The converted number.</returns>
        private static float ValueConverter(float param, bool negOrPos)
        {
            const float conversion = 0.01f;
            return negOrPos ? param * conversion : -param * conversion;
        }

        /// <summary>
        /// Searches for a blendshape by name and returns the index of it.
        /// </summary>
        /// <param name="blendShapeName"></param>
        /// <returns>The index of Blendshape</returns>
        public int BlendShapeByString(string blendShapeName)
        {
            for (int i = 0; i < targetSkinnedRenderer.sharedMesh.blendShapeCount; i++)
            {
                if(targetSkinnedRenderer.sharedMesh.GetBlendShapeName(i) == blendShapeName)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}