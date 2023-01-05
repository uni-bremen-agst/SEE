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
        private SkinnedMeshRenderer TargetSkinnedRenderer;

        /// <summary>
        /// Mesh for baking.
        /// </summary>
        private Mesh BakedMesh;

        /// <summary>
        /// This UMA ExpressionPlayer is controlled by the fake Blendshapes.
        /// </summary>
        private UMAExpressionPlayer ExpressionPlayer;

        /// <summary>
        /// UMA Renderer.
        /// </summary>
        private Transform UmaRenderer;
        
        /// <summary>
        /// Constant string literals for the fake Blendshapes.
        /// </summary>
        // Jaw
        private const string JawLeft = "Jaw_Left";
        private const string JawRight = "Jaw_Right";
        private const string JawForward = "Jaw_Forward";
        private const string JawOpen = "Jaw_Open";

        // Mouth
        private const string MouthApeShape = "Mouth_Ape_Shape";
        private const string MouthUpperLeft = "Mouth_Upper_Left";
        private const string MouthUpperRight = "Mouth_Upper_Right";
        private const string MouthLowerLeft = "Mouth_Lower_Left";
        private const string MouthLowerRight = "Mouth_Lower_Right";
        private const string MouthUpperOverturn = "Mouth_Upper_Overturn";
        private const string MouthLowerOverturn = "Mouth_Lower_Overturn";
        private const string MouthPout = "Mouth_Pout";
        private const string MouthSmileLeft = "Mouth_Smile_Left";
        private const string MouthSmileRight = "Mouth_Smile_Right";
        private const string MouthSadLeft = "Mouth_Sad_Left";
        private const string MouthSadRight = "Mouth_Sad_Right";
        private const string MouthUpperUpLeft = "Mouth_Upper_UpLeft";
        private const string MouthUpperUpRight = "Mouth_Upper_UpRight";
        private const string MouthLowerDownLeft = "Mouth_Lower_DownLeft";
        private const string MouthLowerDownRight = "Mouth_Lower_DownRight";
        private const string MouthUpperInside = "Mouth_Upper_Inside";
        private const string MouthLowerInside = "Mouth_Lower_Inside";
        private const string MouthLowerOverlay = "Mouth_Lower_Overlay";
        
        // Tongue
        private const string TongueLongStep1 = "Tongue_LongStep1";
        private const string TongueLongStep2 = "Tongue_LongStep2";
        private const string TongueLeft = "Tongue_Left";
        private const string TongueRight = "Tongue_Right";
        private const string TongueUp = "Tongue_Up";
        private const string TongueDown = "Tongue_Down";
        private const string TongueRoll = "Tongue_Roll";
        private const string TongueUpLeftMorph = "Tongue_UpLeft_Morph";
        private const string TongueUpRightMorph = "Tongue_UpRight_Morph";
        private const string TongueDownLeftMorph = "Tongue_DownLeft_Morph";
        private const string TongueDownRightMorph = "Tongue_DownRight_Morph";
        
        // Cheeks
        private const string CheekPuffLeft = "Cheek_Puff_Left";
        private const string CheekPuffRight = "Cheek_Puff_Right";
        private const string CheekSuck = "Cheek_Suck";
        
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
            UmaRenderer = gameObject.transform.Find("UMARenderer");
            UmaRenderer.gameObject.AddComponent<AvatarSRanipalLipV2>();

            InitializeBlendshapes();
        }

        /// <summary>
        /// This function creates the fake Blendshapes necessary for the HTC Facial Tracker.
        /// </summary>
        private void InitializeBlendshapes()
        {
            ExpressionPlayer = gameObject.GetComponent<UMAExpressionPlayer>();

            // Assure that Jaw can be used by UMAExpressionPlayer
            ExpressionPlayer.overrideMecanimJaw = true;
            TargetSkinnedRenderer = UmaRenderer.GetComponent<SkinnedMeshRenderer>();
            BakedMesh = new Mesh();
            TargetSkinnedRenderer.BakeMesh(BakedMesh);

            Vector3[] junkData = new Vector3[BakedMesh.vertices.Length];

            // Setup fake Blendshapes
            // Jaw Blendshapes
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(JawLeft, 100, junkData, junkData, junkData); // Jaw_Left_Right - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(JawRight, 100, junkData, junkData, junkData); // Jaw_Left_Right - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(JawForward, 100, junkData, junkData, junkData); //  Jaw_Forward_Back - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(JawOpen, 100, junkData, junkData, junkData); // Jaw_Open_Close - range (0 - 1)

            // Mouth Blendshapes
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthApeShape, 100, junkData, junkData, junkData); // Drag whole mouth down FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthUpperLeft, 100, junkData, junkData, junkData); // Mouth Left_Right - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthUpperRight, 100, junkData, junkData, junkData); // Mouth Left_Right - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthLowerLeft, 100, junkData, junkData, junkData); // Mouth Left_Right - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthLowerRight, 100, junkData, junkData, junkData); // Mouth Left_Right - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthUpperOverturn, 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthLowerOverturn, 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthPout, 100, junkData, junkData, junkData); // Mouth Narrow_Pucker (but only (0-1)) FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthSmileLeft, 100, junkData, junkData, junkData); // Left Mouth Smile_Frown (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthSmileRight, 100, junkData, junkData, junkData); // Right Mouth Smile_Frown (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthSadLeft, 100, junkData, junkData, junkData); // Left Mouth Smile_Frown (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthSadRight, 100, junkData, junkData, junkData); // Left Mouth Smile_Frown (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthUpperUpLeft, 100, junkData, junkData, junkData); // Left Upper Lip Up_Down - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthUpperUpRight, 100, junkData, junkData, junkData); // Right Upper Lip Up_Down - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthLowerDownLeft, 100, junkData, junkData, junkData); // Left Lower Lip Up_Down - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthLowerDownRight, 100, junkData, junkData, junkData); // Right Lower Lip Up_Down - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthUpperInside, 100, junkData, junkData, junkData); // Left Upper Lip Up_Down && Right Upper Lip Up_Down (-1 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthLowerInside, 100, junkData, junkData, junkData); // Left Lower Lip Up_Down && Right Lower Lip Up_Down (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(MouthLowerOverlay, 100, junkData, junkData, junkData); // Maybe Jaw Close with range (-1 - 0) FIXME

            // Tongue Blendshapes
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueLongStep1, 100, junkData, junkData, junkData); // Lift tongue slightly FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueLongStep2, 100, junkData, junkData, junkData); // Tongue Out - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueLeft, 100, junkData, junkData, junkData); // Tongue Left_Right - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueRight, 100, junkData, junkData, junkData); // Tongue Left_Right - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueUp, 100, junkData, junkData, junkData); // Tongue Up_Down - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueDown, 100, junkData, junkData, junkData); // Tongue Up_Down - range (1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueRoll, 100, junkData, junkData, junkData); // Tongue Curl - range (0-1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueUpLeftMorph, 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueUpRightMorph, 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueDownLeftMorph, 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(TongueDownRightMorph, 100, junkData, junkData, junkData); // FIXME

            // Cheek Blendshapes
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(CheekPuffLeft, 100, junkData, junkData, junkData); // Left Cheek Puff_Squint - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(CheekPuffRight, 100, junkData, junkData, junkData); // Right Cheek Puff_Squint - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(CheekSuck, 100, junkData, junkData, junkData); // Maybe "Left Cheek Puff_Squint && Right Cheek Puff_Squint" - range (-1 - 0) FIXME
        }

        /// <summary>
        /// This method transfers the converted values of the fake Blendshapes to the UMAExpressionPlayer.
        /// </summary>
        private void Update()
        {
            if (ExpressionPlayer != null)
            {
                // JAW
                float jawOpenClose = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(JawOpen));
                ExpressionPlayer.jawOpen_Close = ValueConverter(jawOpenClose, true);

                float jawLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(JawLeft));
                float jawRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(JawRight));
                if (jawLeft >= jawRight)
                {
                    ExpressionPlayer.jawLeft_Right = ValueConverter(jawLeft - jawRight, true);
                }
                else
                {
                    ExpressionPlayer.jawLeft_Right = ValueConverter(jawRight - jawLeft, false);
                }

                // JAW & MOUTH
                float jawForward = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(JawForward));
                float mouthOverlay = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerOverlay));
                if (jawForward >= mouthOverlay)
                {
                    ExpressionPlayer.jawForward_Back = ValueConverter(jawForward, true);
                }
                else
                {
                    ExpressionPlayer.jawForward_Back = ValueConverter(mouthOverlay, false); //FIXME: maybe not representable with UMA.
                }

                // Mouth
                float mouthApeShape = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthApeShape));
                ExpressionPlayer.mouthUp_Down = ValueConverter(mouthApeShape, false);

                float mouthUpperLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperLeft));
                float mouthLowerLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerLeft));
                float mouthUpperRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperRight));
                float mouthLowerRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerRight));

                const int divideByTwo = 2;
                if (mouthUpperLeft >= mouthLowerLeft && mouthUpperLeft >= mouthUpperRight && mouthUpperLeft >= mouthLowerRight)
                {
                    ExpressionPlayer.mouthLeft_Right = ValueConverter(mouthUpperLeft - (mouthUpperRight + mouthLowerRight) / divideByTwo, true);
                }
                else if (mouthLowerLeft >= mouthUpperLeft && mouthLowerLeft >= mouthUpperRight && mouthLowerLeft >= mouthLowerRight)
                {
                    ExpressionPlayer.mouthLeft_Right = ValueConverter(mouthLowerLeft - (mouthUpperRight + mouthLowerRight) / divideByTwo, true);
                }
                else if (mouthUpperRight >= mouthUpperLeft && mouthUpperRight >= mouthLowerLeft && mouthUpperRight >= mouthLowerRight)
                {
                    ExpressionPlayer.mouthLeft_Right = ValueConverter(mouthUpperRight - (mouthUpperLeft + mouthLowerLeft) / divideByTwo, false);
                }
                else
                {
                    ExpressionPlayer.mouthLeft_Right = ValueConverter(mouthLowerRight - (mouthUpperLeft + mouthLowerLeft) / divideByTwo, false);
                }

                // FIXME: maybe not representable with UMA.
                //float mouthUpperOverturn = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperOverturn));
                //float mouthLowerOverturn = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerOverturn));

                float mouthPout = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthPout));
                ExpressionPlayer.mouthNarrow_Pucker = ValueConverter(mouthPout, true);

                float mouthSmileLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthSmileLeft));
                float mouthSadLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthSadLeft));
                if (mouthSmileLeft >= mouthSadLeft)
                {
                    ExpressionPlayer.leftMouthSmile_Frown = ValueConverter(mouthSmileLeft - mouthSadLeft, true);
                }
                else
                {
                    ExpressionPlayer.leftMouthSmile_Frown = ValueConverter(mouthSadLeft - mouthSmileLeft, false);
                }

                float mouthSmileRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthSmileRight));
                float mouthSadRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthSadRight));
                if (mouthSmileRight >= mouthSadRight)
                {
                    ExpressionPlayer.rightMouthSmile_Frown = ValueConverter(mouthSmileRight - mouthSadRight, true);
                }
                else
                {
                    ExpressionPlayer.rightMouthSmile_Frown = ValueConverter(mouthSadRight - mouthSmileRight, false);
                }

                float mouthUpperUpLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperUpLeft));
                ExpressionPlayer.leftUpperLipUp_Down = ValueConverter(mouthUpperUpLeft, true);

                float mouthUpperUpRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperUpRight));
                ExpressionPlayer.rightUpperLipUp_Down = ValueConverter(mouthUpperUpRight, true);

                float mouthLowerDownLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerDownLeft));
                ExpressionPlayer.leftLowerLipUp_Down = ValueConverter(mouthLowerDownLeft, false);

                float mouthLowerDownRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerDownRight));
                ExpressionPlayer.rightLowerLipUp_Down = ValueConverter(mouthLowerDownRight, false);

                // FIXME: maybe not representable with UMA.
                //float mouthUpperInside = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperInside));
                //float mouthLowerInside = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerInside));

                // Tongue
                float tongueLongStep1 = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueLongStep1));
                float tongueLongStep2 = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueLongStep2));
                if (tongueLongStep1 >= tongueLongStep2)
                {
                    ExpressionPlayer.tongueOut = ValueConverter(tongueLongStep1, false); //Is not representable in UMA
                }
                else
                {
                    ExpressionPlayer.tongueOut = ValueConverter(tongueLongStep2, true);
                }

                float tongueLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueLeft));
                float tongueRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueRight));
                if (tongueLeft >= tongueRight)
                {
                    ExpressionPlayer.tongueLeft_Right = ValueConverter(tongueLeft - tongueRight, true);
                }
                else
                {
                    ExpressionPlayer.tongueLeft_Right = ValueConverter(tongueRight - tongueLeft, false);
                }

                float tongueUp = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueUp));
                float tongueDown = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueDown));
                if (tongueUp >= tongueDown)
                {
                    ExpressionPlayer.tongueUp_Down = ValueConverter(tongueUp - tongueDown, true);
                }
                else
                {
                    ExpressionPlayer.tongueUp_Down = ValueConverter(tongueDown - tongueUp, false);
                }

                float tongueRoll = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueRoll));
                ExpressionPlayer.tongueCurl = ValueConverter(tongueRoll, true);

                // FIXME: maybe not representable with UMA.
                //float tongueUpLeftMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueUpLeftMorph));
                //float tongueUpRightMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueUpRightMorph));
                //float tongueDownLeftMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueDownLeftMorph));
                //float tongueDownRightMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(TongueDownRightMorph));

                // Cheeks
                float cheekPuffLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(CheekPuffLeft));
                float cheekPuffRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(CheekPuffRight));
                float cheeckSuck = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(CheekSuck));
                if (cheekPuffLeft >= cheeckSuck && cheekPuffRight >= cheeckSuck)
                {
                    ExpressionPlayer.leftCheekPuff_Squint = ValueConverter(cheekPuffLeft, true);
                    ExpressionPlayer.rightCheekPuff_Squint = ValueConverter(cheekPuffRight, true);
                }
                else
                {
                    ExpressionPlayer.leftCheekPuff_Squint = ValueConverter(cheeckSuck, false);
                    ExpressionPlayer.rightCheekPuff_Squint = ValueConverter(cheeckSuck, false);
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
        private float ValueConverter(float param, bool negOrPos)
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
            for (int i = 0; i < TargetSkinnedRenderer.sharedMesh.blendShapeCount; i++)
            {
                if(TargetSkinnedRenderer.sharedMesh.GetBlendShapeName(i) == blendShapeName)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}