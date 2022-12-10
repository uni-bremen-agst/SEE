using UMA.PoseTools;
using UnityEngine;

// FIMXE: This class was merged from the Facial Tracker branch.
// Changes necessary to conform to our coding styles should be
// made there and then merged. I do not want to make the changes
// here because that may result in conflicts.

namespace SEE.Game.Avatars
{
    internal class AvatarBlendshapeExpressions : MonoBehaviour
    {
        public SkinnedMeshRenderer targetSkinnedRenderer;
        public Mesh bakedMesh;
        public UMAExpressionPlayer expressionPlayer;


        private void Start()
        {
            if (this.transform.parent.GetComponent<UMAExpressionPlayer>() == null)
            {
                Debug.Log(
                    "Please ensure an expression player has been added to the parent UMA GameObject - this script will now stop running");
                return;
            }
            else
            {
                expressionPlayer = this.transform.parent.GetComponent<UMAExpressionPlayer>();
            }


            targetSkinnedRenderer = this.GetComponent<SkinnedMeshRenderer>();
            bakedMesh = new Mesh();
            Debug.Log("UMA skinned mesh found - now baking");
            targetSkinnedRenderer.BakeMesh(bakedMesh);

            Vector3[] junkData = new Vector3[bakedMesh.vertices.Length];


            // Setup fake blendshapes

            // Jaw Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Left", 100, junkData, junkData,
                junkData); // Jaw_Left_Right - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Right", 100, junkData, junkData,
                junkData); // Jaw_Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Forward", 100, junkData, junkData,
                junkData); //  Jaw_Forward_Back - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Open", 100, junkData, junkData,
                junkData); // Jaw_Open_Close - range (0 - 1)

            // Mouth
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Ape_Shape", 100, junkData, junkData,
                junkData); // Stretch mouth down

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Left", 100, junkData, junkData,
                junkData); // Mouth Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Right", 100, junkData, junkData,
                junkData); // Mouth Left_Right - range (-1 - 0)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Left", 100, junkData, junkData,
                junkData); // Mouth Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Right", 100, junkData, junkData,
                junkData); // Mouth Left_Right - range (-1 - 0)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Overturn", 100, junkData, junkData,
                junkData); // Push Top Lip out //Not representable in UMA
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Overturn", 100, junkData, junkData,
                junkData); // Push Bottom Lip out //Not representable in UMA

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Pout", 100, junkData, junkData,
                junkData); // Mouth Narrow_Pucker (but only (0-1) ??)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Smile_Left", 100, junkData, junkData,
                junkData); // Left Mouth Smile_Frown (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Smile_Right", 100, junkData, junkData,
                junkData); // Right Mouth Smile_Frown (0 - 1)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Sad_Left", 100, junkData, junkData,
                junkData); // Left Mouth Smile_Frown (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Sad_Right", 100, junkData, junkData,
                junkData); // Left Mouth Smile_Frown (-1 - 0)


            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_UpLeft", 100, junkData, junkData,
                junkData); // Left Upper Lip Up_Down - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_UpRight", 100, junkData, junkData,
                junkData); // Right Upper Lip Up_Down - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_DownLeft", 100, junkData, junkData,
                junkData); // Left Lower Lip Up_Down - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_DownRight", 100, junkData, junkData,
                junkData); // Right Lower Lip Up_Down - range (-1 - 0)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Inside", 100, junkData, junkData,
                junkData); // Left Upper Lip Up_Down && Right Upper Lip Up_Down (-1 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Inside", 100, junkData, junkData,
                junkData); // Left Lower Lip Up_Down && Right Lower Lip Up_Down (0 - 1)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Overlay", 100, junkData, junkData,
                junkData); // Jaw Close with range (-1 - 0)

            // Tongue
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_LongStep1", 100, junkData, junkData,
                junkData); // Tongue in (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_LongStep2", 100, junkData, junkData,
                junkData); // Tongue Out (0 - 1)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Left", 100, junkData, junkData,
                junkData); // Tongue Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Right", 100, junkData, junkData,
                junkData); // Tongue Left_Right - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Up", 100, junkData, junkData,
                junkData); // Tongue Up_Down - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Down", 100, junkData, junkData,
                junkData); // Tongue Up_Down - range (1 - 0)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Roll", 100, junkData, junkData,
                junkData); // Tongue Curl - range (0-1)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_UpLeft_Morph", 100, junkData, junkData,
                junkData); // Zunge leicht raus oben links
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_UpRight_Morph", 100, junkData, junkData,
                junkData); // Zunge leich raus oben rechts

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_DownLeft_Morph", 100, junkData, junkData,
                junkData); // Zunge leicht raus unten links
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_DownRight_Morph", 100, junkData, junkData,
                junkData); // Zunge leicht raus unten rechts

            // Cheeks
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Cheek_Puff_Left", 100, junkData, junkData,
                junkData); // Left Cheek Puff_Squint - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Cheek_Puff_Right", 100, junkData, junkData,
                junkData); // Right Cheek Puff_Squint - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Cheek_Suck", 100, junkData, junkData,
                junkData); // Maybe "Left Cheek Puff_Squint && Right Cheek Puff_Squint" - range (-1 - 0)
        }

        private void Update()
        {
            // JAW
            float jawOpenClose = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Jaw_Open"));
            expressionPlayer.jawOpen_Close = ValueConverter(jawOpenClose, true);

            float jawLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Jaw_Left"));
            float jawRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Jaw_Right"));
            if (jawLeft >= jawRight)
            {
                expressionPlayer.jawLeft_Right = ValueConverter(jawLeft - jawRight, true);
            }
            else
            {
                expressionPlayer.jawLeft_Right = ValueConverter(jawRight - jawLeft, false);
            }

            // JAW & MOUTH
            float jawForward = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Jaw_Forward"));
            float mouthOverlay = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_Overlay"));
            if (jawForward >= mouthOverlay)
            {
                expressionPlayer.jawForward_Back = ValueConverter(jawForward, true);
            }
            else
            {
                expressionPlayer.jawForward_Back = ValueConverter(mouthOverlay, false); //Is not representable in UMA
            }
            // Mouth

            float mouthApeShape = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Ape_Shape"));
            expressionPlayer.mouthUp_Down = ValueConverter(mouthApeShape, false);

            float mouthUpperLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_Left"));
            float mouthLowerLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_Left"));
            float mouthUpperRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_Right"));
            float mouthLowerRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_Right"));

            if (mouthUpperLeft >= mouthLowerLeft && mouthUpperLeft >= mouthUpperRight &&
                mouthUpperLeft >= mouthLowerRight)
            {
                expressionPlayer.mouthLeft_Right =
                    ValueConverter(mouthUpperLeft - (mouthUpperRight + mouthLowerRight) / 2, true);
            }
            else if (mouthLowerLeft >= mouthUpperLeft && mouthLowerLeft >= mouthUpperRight &&
                     mouthLowerLeft >= mouthLowerRight)
            {
                expressionPlayer.mouthLeft_Right =
                    ValueConverter(mouthLowerLeft - (mouthUpperRight + mouthLowerRight) / 2, true);
            }
            else if (mouthUpperRight >= mouthUpperLeft && mouthUpperRight >= mouthLowerLeft &&
                     mouthUpperRight >= mouthLowerRight)
            {
                expressionPlayer.mouthLeft_Right =
                    ValueConverter(mouthUpperRight - (mouthUpperLeft + mouthLowerLeft) / 2, false);
            }
            else
            {
                expressionPlayer.mouthLeft_Right =
                    ValueConverter(mouthLowerRight - (mouthUpperLeft + mouthLowerLeft) / 2, false);
            }

            float mouthPout = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Pout"));
            expressionPlayer.mouthNarrow_Pucker = ValueConverter(mouthPout, true);


            float mouthSmileLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Smile_Left"));
            float mouthSadLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Sad_Left"));
            if (mouthSmileLeft >= mouthSadLeft)
            {
                expressionPlayer.leftMouthSmile_Frown = ValueConverter(mouthSmileLeft - mouthSadLeft, true);
            }
            else
            {
                expressionPlayer.leftMouthSmile_Frown = ValueConverter(mouthSadLeft - mouthSmileLeft, false);
            }


            float mouthSmileRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Smile_Right"));
            float mouthSadRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Sad_Right"));
            if (mouthSmileRight >= mouthSadRight)
            {
                expressionPlayer.rightMouthSmile_Frown = ValueConverter(mouthSmileRight - mouthSadRight, true);
            }
            else
            {
                expressionPlayer.rightMouthSmile_Frown = ValueConverter(mouthSadRight - mouthSmileRight, false);
            }


            float mouthUpperUpLeft =
                targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_UpLeft"));
            expressionPlayer.leftUpperLipUp_Down = ValueConverter(mouthUpperUpLeft, true);

            float mouthUpperUpRight =
                targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_UpRight"));
            expressionPlayer.rightUpperLipUp_Down = ValueConverter(mouthUpperUpRight, true);

            float mouthLowerDownLeft =
                targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_DownLeft"));
            expressionPlayer.leftLowerLipUp_Down = ValueConverter(mouthLowerDownLeft, false);

            float mouthLowerDownRight =
                targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_DownRight"));
            expressionPlayer.rightLowerLipUp_Down = ValueConverter(mouthLowerDownRight, false);


            // TODO
            float mouthUpperInside =
                targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Upper_Inside"));
            float mouthLowerInside =
                targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Mouth_Lower_Inside"));


            // Tongue
            float tongueLongStep1 = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_LongStep1"));
            float tongueLongStep2 = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_LongStep2"));
            if (tongueLongStep1 >= tongueLongStep2)
            {
                expressionPlayer.tongueOut = ValueConverter(tongueLongStep1, false); //Is not representable in UMA
            }
            else
            {
                expressionPlayer.tongueOut = ValueConverter(tongueLongStep2, true);
            }


            float tongueLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Left"));
            float tongueRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Right"));
            if (tongueLeft >= tongueRight)
            {
                expressionPlayer.tongueLeft_Right = ValueConverter(tongueLeft - tongueRight, true);
            }
            else
            {
                expressionPlayer.tongueLeft_Right = ValueConverter(tongueRight - tongueLeft, false);
            }


            float tongueUp = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Up"));
            float tongueDown = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Down"));
            if (tongueUp >= tongueDown)
            {
                expressionPlayer.tongueUp_Down = ValueConverter(tongueUp - tongueDown, true);
            }
            else
            {
                expressionPlayer.tongueUp_Down = ValueConverter(tongueDown - tongueUp, false);
            }


            float tongueRoll = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Tongue_Roll"));
            expressionPlayer.tongueCurl = ValueConverter(tongueRoll, true);




            // Cheeks
            float cheekPuffLeft = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Cheek_Puff_Left"));
            //expressionPlayer.leftCheekPuff_Squint = ValueConverter(cheekPuffLeft, true); //Is not representable in UMA

            float cheekPuffRight = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Cheek_Puff_Right"));
            //expressionPlayer.rightCheekPuff_Squint = ValueConverter(cheekPuffRight, true);

            float cheeckSuck = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString("Cheek_Suck"));
            //expressionPlayer.leftCheekPuff_Squint = ValueConverter(cheeckSuck, false);
            //expressionPlayer.rightCheekPuff_Squint = ValueConverter(cheeckSuck, false);


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

        private float ValueConverter(float param, bool negOrPos)
        {
            const float conversion = 0.01f;
            return negOrPos ? param * conversion : -param * conversion;
        }

        public int BlendShapeByString(string arg)
        {
            for (int i = 0; i < targetSkinnedRenderer.sharedMesh.blendShapeCount; i++)
            {
                if (targetSkinnedRenderer.sharedMesh.GetBlendShapeName(i) == arg)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}