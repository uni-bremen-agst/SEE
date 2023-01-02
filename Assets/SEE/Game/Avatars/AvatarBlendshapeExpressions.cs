using System.Collections;
using UnityEngine;
using UMA.PoseTools;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// This script should be attached to a gameobject that has an UmaRenderer and <see cref="UMAExpressionPlayer"/>.
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
        /// Starts a coroutine that waits for components to be generated at runtime.
        /// </summary>
        private void Start()
        {
            StartCoroutine(WaitForComponents());
        }

        /// <summary>
        /// Waits for UMARenderer and UMAExpressionPlayer to be created.
        /// </summary>
        IEnumerator WaitForComponents()
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
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Left", 100, junkData, junkData, junkData); // Jaw_Left_Right - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Right", 100, junkData, junkData, junkData); // Jaw_Left_Right - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Forward", 100, junkData, junkData, junkData); //  Jaw_Forward_Back - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Open", 100, junkData, junkData, junkData); // Jaw_Open_Close - range (0 - 1) 

            // Mouth Blendshapes
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Ape_Shape", 100, junkData, junkData, junkData); // Drag whole mouth down FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Left", 100, junkData, junkData, junkData); // Mouth Left_Right - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Right", 100, junkData, junkData, junkData); // Mouth Left_Right - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Left", 100, junkData, junkData, junkData); // Mouth Left_Right - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Right", 100, junkData, junkData, junkData); // Mouth Left_Right - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Overturn", 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Overturn", 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Pout", 100, junkData, junkData, junkData); // Mouth Narrow_Pucker (but only (0-1)) FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Smile_Left", 100, junkData, junkData, junkData); // Left Mouth Smile_Frown (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Smile_Right", 100, junkData, junkData, junkData); // Right Mouth Smile_Frown (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Sad_Left", 100, junkData, junkData, junkData); // Left Mouth Smile_Frown (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Sad_Right", 100, junkData, junkData, junkData); // Left Mouth Smile_Frown (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_UpLeft", 100, junkData, junkData, junkData); // Left Upper Lip Up_Down - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_UpRight", 100, junkData, junkData, junkData); // Right Upper Lip Up_Down - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_DownLeft", 100, junkData, junkData, junkData); // Left Lower Lip Up_Down - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_DownRight", 100, junkData, junkData, junkData); // Right Lower Lip Up_Down - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Inside", 100, junkData, junkData, junkData); // Left Upper Lip Up_Down && Right Upper Lip Up_Down (-1 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Inside", 100, junkData, junkData, junkData); // Left Lower Lip Up_Down && Right Lower Lip Up_Down (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Overlay", 100, junkData, junkData, junkData); // Maybe Jaw Close with range (-1 - 0) FIXME

            // Tongue Blendshapes
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_LongStep1", 100, junkData, junkData, junkData); // Lift tongue slightly FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_LongStep2", 100, junkData, junkData, junkData); // Tongue Out - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Left", 100, junkData, junkData, junkData); // Tongue Left_Right - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Right", 100, junkData, junkData, junkData); // Tongue Left_Right - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Up", 100, junkData, junkData, junkData); // Tongue Up_Down - range (-1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Down", 100, junkData, junkData, junkData); // Tongue Up_Down - range (1 - 0)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Roll", 100, junkData, junkData, junkData); // Tongue Curl - range (0-1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_UpLeft_Morph", 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_UpRight_Morph", 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_DownLeft_Morph", 100, junkData, junkData, junkData); // FIXME
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_DownRight_Morph", 100, junkData, junkData, junkData); // FIXME

            // Cheek Blendshapes
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Cheek_Puff_Left", 100, junkData, junkData, junkData); // Left Cheek Puff_Squint - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Cheek_Puff_Right", 100, junkData, junkData, junkData); // Right Cheek Puff_Squint - range (0 - 1)
            TargetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Cheek_Suck", 100, junkData, junkData, junkData); // Maybe "Left Cheek Puff_Squint && Right Cheek Puff_Squint" - range (-1 - 0) FIXME
        }

        /// <summary>
        /// This method transfers the converted values of the fake Blendshapes to the UMAExpressionPlayer.
        /// </summary>
        private void Update()
        {
            if (ExpressionPlayer != null)
            {
                // JAW
                float jawOpenClose = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Jaw_Open"));
                ExpressionPlayer.jawOpen_Close = ValueConverter(jawOpenClose, true);

                float jawLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Jaw_Left"));
                float jawRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Jaw_Right"));
                if (jawLeft >= jawRight)
                {
                    ExpressionPlayer.jawLeft_Right = ValueConverter(jawLeft - jawRight, true);
                }
                else
                {
                    ExpressionPlayer.jawLeft_Right = ValueConverter(jawRight - jawLeft, false);
                }

                // JAW & MOUTH
                float jawForward = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Jaw_Forward"));
                float mouthOverlay = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_Overlay"));
                if (jawForward >= mouthOverlay)
                {
                    ExpressionPlayer.jawForward_Back = ValueConverter(jawForward, true);
                }
                else
                {
                    ExpressionPlayer.jawForward_Back = ValueConverter(mouthOverlay, false); //FIXME: maybe not representable with UMA.
                }
                
                // Mouth
                float mouthApeShape = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Ape_Shape"));
                ExpressionPlayer.mouthUp_Down = ValueConverter(mouthApeShape, false);

                float mouthUpperLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_Left"));
                float mouthLowerLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_Left"));
                float mouthUpperRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_Right"));
                float mouthLowerRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_Right"));

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
                //float mouthUpperOverturn = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_Overturn"));
                //float mouthLowerOverturn = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_Overturn"));

                float mouthPout = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Pout"));
                ExpressionPlayer.mouthNarrow_Pucker = ValueConverter(mouthPout, true);
                
                float mouthSmileLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Smile_Left"));
                float mouthSadLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Sad_Left"));
                if (mouthSmileLeft >= mouthSadLeft)
                {
                    ExpressionPlayer.leftMouthSmile_Frown = ValueConverter(mouthSmileLeft - mouthSadLeft, true);
                }
                else
                {
                    ExpressionPlayer.leftMouthSmile_Frown = ValueConverter(mouthSadLeft - mouthSmileLeft, false);
                }
                
                float mouthSmileRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Smile_Right"));
                float mouthSadRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Sad_Right"));
                if (mouthSmileRight >= mouthSadRight)
                {
                    ExpressionPlayer.rightMouthSmile_Frown = ValueConverter(mouthSmileRight - mouthSadRight, true);
                }
                else
                {
                    ExpressionPlayer.rightMouthSmile_Frown = ValueConverter(mouthSadRight - mouthSmileRight, false);
                }
                
                float mouthUpperUpLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_UpLeft"));
                ExpressionPlayer.leftUpperLipUp_Down = ValueConverter(mouthUpperUpLeft, true);

                float mouthUpperUpRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_UpRight"));
                ExpressionPlayer.rightUpperLipUp_Down = ValueConverter(mouthUpperUpRight, true);

                float mouthLowerDownLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_DownLeft"));
                ExpressionPlayer.leftLowerLipUp_Down = ValueConverter(mouthLowerDownLeft, false);

                float mouthLowerDownRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_DownRight"));
                ExpressionPlayer.rightLowerLipUp_Down = ValueConverter(mouthLowerDownRight, false);
                
                // FIXME: maybe not representable with UMA.
                //float mouthUpperInside = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_Inside"));
                //float mouthLowerInside = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_Inside"));
                
                // Tongue
                float tongueLongStep1 = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_LongStep1"));
                float tongueLongStep2 = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_LongStep2"));
                if (tongueLongStep1 >= tongueLongStep2)
                {
                    ExpressionPlayer.tongueOut = ValueConverter(tongueLongStep1, false); //Is not representable in UMA
                }
                else
                {
                    ExpressionPlayer.tongueOut = ValueConverter(tongueLongStep2, true);
                }
                
                float tongueLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Left"));
                float tongueRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Right"));
                if (tongueLeft >= tongueRight)
                {
                    ExpressionPlayer.tongueLeft_Right = ValueConverter(tongueLeft - tongueRight, true);
                }
                else
                {
                    ExpressionPlayer.tongueLeft_Right = ValueConverter(tongueRight - tongueLeft, false);
                }
                
                float tongueUp = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Up"));
                float tongueDown = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Down"));
                if (tongueUp >= tongueDown)
                {
                    ExpressionPlayer.tongueUp_Down = ValueConverter(tongueUp - tongueDown, true);
                }
                else
                {
                    ExpressionPlayer.tongueUp_Down = ValueConverter(tongueDown - tongueUp, false);
                }
                
                float tongueRoll = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Roll"));
                ExpressionPlayer.tongueCurl = ValueConverter(tongueRoll, true);
                
                // FIXME: maybe not representable with UMA.
                //float tongueUpLeftMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_UpLeft_Morph"));
                //float tongueUpRightMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_UpRight_Morph"));
                //float tongueDownLeftMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_DownLeft_Morph"));
                //float tongueDownRightMorph = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_DownRight_Morph"));
                
                // Cheeks
                float cheekPuffLeft = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Cheek_Puff_Left"));
                float cheekPuffRight = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Cheek_Puff_Right"));
                float cheeckSuck = TargetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Cheek_Suck"));
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