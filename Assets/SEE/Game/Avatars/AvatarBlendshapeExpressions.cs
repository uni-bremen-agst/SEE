using System;
using System.Collections;
using System.Collections.Generic;
using SEE.GO;
using UnityEngine;
using UMA.PoseTools;
using System.Linq;

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
        /// The SkinnedMeshRenderer of CC_Base_Body.
        /// </summary>
        private SkinnedMeshRenderer targetSkinnedRenderer;

        /// <summary>
        /// Mesh for baking.
        /// </summary>
        private Mesh bakedMesh;

        private Transform ccBaseBody;

        /// <summary>
        /// Constant string literals for the fake Blendshapes.
        /// </summary>
        // Jaw
        private const string jawLeft = "Jaw_Left";
        private const string ccJawLeft = "Jaw_L";

        private const string jawRight = "Jaw_Right";
        private const string ccJawRight = "Jaw_R";

        private const string jawForward = "Jaw_Forward"; // Same for CC Avatar

        private const string jawOpen = "Jaw_Open"; // Same for CC Avatar
        private const string ccJawOpen = "Merged_Open_Mouth";

        // Mouth
        private const string mouthApeShape = "Mouth_Ape_Shape";

        private const string mouthUpperLeft = "Mouth_Upper_Left";
        private const string ccMouthUpperLeft = "Mouth_Upper_L";

        private const string mouthUpperRight = "Mouth_Upper_Right";
        private const string ccMouthUpperRight = "Mouth_Upper_R";

        private const string mouthLowerLeft = "Mouth_Lower_Left";
        private const string ccLowerLeft = "Mouth_Lower_L";

        private const string mouthLowerRight = "Mouth_Lower_Right";
        private const string ccLowerRight = "Mouth_Lower_R";

        private const string mouthUpperOverturn = "Mouth_Upper_Overturn";
        private const string mouthLowerOverturn = "Mouth_Lower_Overturn";

        private const string mouthPout = "Mouth_Pout";
        private const string ccMouthPuckerUpL = "Mouth_Pucker_Up_L";
        private const string ccMouthPuckerUpR = "Mouth_Pucker_Up_R";
        private const string ccMouthPuckerDownL = "Mouth_Pucker_Down_L";
        private const string ccMouthPuckerDownR = "Mouth_Pucker_Down_R";

        private const string mouthSmileLeft = "Mouth_Smile_Left";
        private const string ccMouthSmileLeft = "Mouth_Smile_L";

        private const string mouthSmileRight = "Mouth_Smile_Right";
        private const string ccMouthSmileRight = "Mouth_Smile_R";

        private const string mouthSadLeft = "Mouth_Sad_Left";
        private const string ccMouthFrownLeft = "Mouth_Frown_L";

        private const string mouthSadRight = "Mouth_Sad_Right";
        private const string ccMouthFrownRight = "Mouth_Frown_R";

        private const string mouthUpperUpLeft = "Mouth_Upper_UpLeft";
        private const string ccMouthUpperUpLeft = "Mouth_Up_Upper_L";

        private const string mouthUpperUpRight = "Mouth_Upper_UpRight";
        private const string ccMouthUpperUpRight = "Mouth_Up_Upper_R";

        private const string mouthLowerDownLeft = "Mouth_Lower_DownLeft";
        private const string ccMouthLowerDownLeft = "Mouth_Down_Lower_L";

        private const string mouthLowerDownRight = "Mouth_Lower_DownRight";
        private const string ccMouthLowerDownRight = "Mouth_Down_Lower_R";

        private const string mouthUpperInside = "Mouth_Upper_Inside";
        private const string ccMouthUpperInsideLeft = "Mouth_Roll_In_Upper_L";
        private const string ccMouthUpperInsideRight = "Mouth_Roll_In_Upper_R";

        private const string mouthLowerInside = "Mouth_Lower_Inside";
        private const string ccMouthLowerInsideLeft = "Mouth_Roll_In_Lower_L";
        private const string ccMouthLowerInsideRight = "Mouth_Roll_In_Lower_R";

        private const string mouthLowerOverlay = "Mouth_Lower_Overlay";

        // Tongue
        private const string tongueLongStep1 = "Tongue_LongStep1";
        private const string ccTongueLongStep1 = "Tongue_Up";

        private const string tongueLongStep2 = "Tongue_LongStep2";
        private const string ccTongueLongStep2 = "Tongue_Out";

        private const string tongueLeft = "Tongue_Left";
        private const string ccTongueLeft = "Tongue_L";

        private const string tongueRight = "Tongue_Right";
        private const string ccTongueRight = "Tongue_R";

        private const string tongueUp = "Tongue_Up"; // Same for CC Avatar
        private const string ccTongueUp = "Tongue_Tip_Up";

        private const string tongueDown = "Tongue_Down"; // Same for CC Avatar
        private const string ccTongueDown = "Tongue_Tip_Down";

        private const string tongueRoll = "Tongue_Roll"; // Same for CC Avatar

        private const string tongueUpLeftMorph = "Tongue_UpLeft_Morph";
        private const string ccTongueBulgeLeft = "Tongue_Bulge_L"; // + Tongue UP

        private const string tongueUpRightMorph = "Tongue_UpRight_Morph";
        private const string ccTongueBulgeRight = "Tongue_Bulge_R"; // + Tongue UP

        private const string tongueDownLeftMorph = "Tongue_DownLeft_Morph"; // + BulgeLeft && Tongue Down
        private const string tongueDownRightMorph = "Tongue_DownRight_Morph"; // + BulgeRight && Tongue Down

        // Cheeks
        private const string cheekPuffLeft = "Cheek_Puff_Left";
        private const string ccCheekPuffLeft = "Cheek_Puff_L";

        private const string cheekPuffRight = "Cheek_Puff_Right";
        private const string ccCheekPuffRight = "Cheek_Puff_R";

        private const string cheekSuck = "Cheek_Suck";
        private const string ccCheekSuckLeft = "Cheek_Suck_L";
        private const string ccCheekSuckRight = "Cheek_Suck_R";

        // Eyes
        private const string eyeLeftBlink = "Eye_Left_Blink";
        private const string ccEyeLeftBlink = "Eye_Blink_L";

        private const string eyeLeftWide = "Eye_Left_Wide";
        private const string ccEyeLeftWide = "Eye_Wide_L";

        private const string eyeLeftRight = "Eye_Left_Right";
        private const string ccEyeLeftRight = "Eye_L_Look_R";

        private const string eyeLeftLeft = "Eye_Left_Left";
        private const string ccEyeLeftLeft = "Eye_L_Look_L";

        private const string eyeLeftUp = "Eye_Left_Up";
        private const string ccEyeLeftUp = "Eye_L_Look_Up";

        private const string eyeLeftDown = "Eye_Left_Down";
        private const string ccEyeLeftDown = "Eye_L_Look_Down";

        private const string eyeRightBlink = "Eye_Right_Blink";
        private const string ccEyeRightBlink = "Eye_Blink_R";

        private const string eyeRightWide = "Eye_Right_Wide";
        private const string ccEyeRightWide = "Eye_Wide_R";

        private const string eyeRightRight = "Eye_Right_Right";
        private const string ccEyeRightRight = "Eye_R_Look_R";

        private const string eyeRightLeft = "Eye_Right_Left";
        private const string ccEyeRightLeft = "Eye_R_Look_L";

        private const string eyeRightUp = "Eye_Right_Up";
        private const string ccEyeRightUp = "Eye_R_Look_Up";

        private const string eyeRightDown = "Eye_Right_Down";
        private const string ccEyeRightDown = "Eye_R_Look_Down";

        private const string eyeFrown = "Eye_Frown";
        private const string eyeLeftSqueeze = "Eye_Left_squeeze";
        private const string eyeRightSqueeze = "Eye_Right_squeeze";

        private bool waitForInit = true;

        private static readonly string[] blendShapeNames;

        private Dictionary<string, float> blendShapes = new Dictionary<string, float>();


        private ArrayList GetArrayListWithBlendShapeStrings()
        {
            ArrayList list = new ArrayList();
            list.Add(jawLeft);
            list.Add(jawRight);
            list.Add(jawOpen);
            list.Add(mouthApeShape);
            list.Add(mouthUpperLeft);
            list.Add(mouthUpperRight);
            list.Add(mouthLowerLeft);
            list.Add(mouthLowerRight);
            list.Add(mouthUpperOverturn);
            list.Add(mouthLowerOverturn);
            list.Add(mouthPout);
            list.Add(mouthSmileLeft);
            list.Add(mouthSmileRight);
            list.Add(mouthSadLeft);
            list.Add(mouthSadRight);
            list.Add(mouthUpperUpRight);
            list.Add(mouthUpperUpLeft);
            list.Add(mouthLowerDownLeft);
            list.Add(mouthLowerDownRight);
            list.Add(mouthUpperInside);
            list.Add(mouthLowerInside);
            list.Add(mouthLowerOverlay);
            list.Add(tongueLongStep1);
            list.Add(tongueLongStep2);
            list.Add(tongueLeft);
            list.Add(tongueRight);
            list.Add(tongueUp);
            list.Add(tongueDown);
            list.Add(tongueRoll);
            list.Add(tongueUpLeftMorph);
            list.Add(tongueUpRightMorph);
            list.Add(tongueDownLeftMorph);
            list.Add(tongueDownRightMorph);
            list.Add(cheekPuffLeft);
            list.Add(cheekPuffRight);
            list.Add(cheekSuck);
            list.Add(eyeLeftBlink);
            list.Add(eyeLeftWide);
            list.Add(eyeLeftRight);
            list.Add(eyeLeftLeft);
            list.Add(eyeLeftUp);
            list.Add(eyeLeftDown);
            list.Add(eyeRightBlink);
            list.Add(eyeRightWide);
            list.Add(eyeRightRight);
            list.Add(eyeRightLeft);
            list.Add(eyeRightUp);
            list.Add(eyeRightDown);
            list.Add(eyeFrown);
            list.Add(eyeLeftSqueeze);
            list.Add(eyeRightSqueeze);
            return list;
        }


        /// <summary>
        /// Starts a coroutine that waits for components to be generated at runtime.
        /// </summary>
        private void Start()
        {
            ccBaseBody = gameObject.transform.Find("CC_Base_Body");
            SkinnedMeshRenderer skinnedMeshRenderer = ccBaseBody.GetComponent<SkinnedMeshRenderer>();
            ccBaseBody.gameObject.AddComponent<AvatarSRanipalLipV2>();
            InitializeBlendshapes(skinnedMeshRenderer);
            //StartCoroutine(WaitForComponents());
        }

        private IEnumerator WaitForComponents()
        {
            yield return new WaitUntil(() => gameObject.transform.Find("CC_Base_Body").GetComponent<SkinnedMeshRenderer>() != null);
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

            foreach (String blendShapeName in GetArrayListWithBlendShapeStrings())
            {
                if (BlendShapeByString(blendShapeName) == -1)
                {
                    targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(blendShapeName, 100, junkData, junkData, junkData);
                }
            }

            /*
            // Setup fake Blendshapes
            // Jaw Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(jawLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(jawRight, 100, junkData, junkData, junkData);
            //targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(jawForward, 100, junkData, junkData, junkData); //  Jaw_Forward_Back - range (0 - 1)
            //targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(jawOpen, 100, junkData, junkData, junkData);

            // Mouth Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthApeShape, 100, junkData, junkData, junkData); // Drag whole mouth down FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperRight, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerRight, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperOverturn, 100, junkData, junkData, junkData); // FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerOverturn, 100, junkData, junkData, junkData); // FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthPout, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthSmileLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthSmileRight, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthSadLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthSadRight, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperUpLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperUpRight, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerDownLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerDownRight, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthUpperInside, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerInside, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(mouthLowerOverlay, 100, junkData, junkData, junkData); // FIXME

            // Eye Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeLeftBlink, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeLeftWide, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeLeftRight, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeLeftLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeLeftUp, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeLeftDown, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeRightBlink, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeRightWide, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeRightRight, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeRightLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeRightUp, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeRightDown, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeFrown, 100, junkData, junkData, junkData); // FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeLeftSqueeze, 100, junkData, junkData, junkData); //FIXME
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(eyeRightSqueeze, 100, junkData, junkData, junkData); //FIXME

            // Tongue Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueLongStep1, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueLongStep2, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueRight, 100, junkData, junkData, junkData);
            //targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueUp, 100, junkData, junkData, junkData);
            //targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueDown, 100, junkData, junkData, junkData);
            //targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueRoll, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueUpLeftMorph, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueUpRightMorph, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueDownLeftMorph, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(tongueDownRightMorph, 100, junkData, junkData, junkData);

            // Cheek Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(cheekPuffLeft, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(cheekPuffRight, 100, junkData, junkData, junkData);
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame(cheekSuck, 100, junkData, junkData, junkData);

            */
            waitForInit = false;
        }


        private void Update()
        {
            if (!waitForInit)
            {

                float jawOpenValue = targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(jawOpen));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(jawOpen), 0f);


                // Jaw
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccJawLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(jawLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccJawRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(jawRight)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccJawOpen), jawOpenValue);

                // Mouth
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthUpperLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthUpperLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthUpperRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthUpperRight)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccLowerLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthLowerLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccLowerRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthLowerRight)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthPuckerUpL),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthPout)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthPuckerUpR),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthPout)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthPuckerDownL),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthPout)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthPuckerDownR),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthPout)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthSmileLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthSmileLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthSmileRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthSmileRight)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthFrownLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthSadLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthFrownRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthSadRight)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthUpperUpLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthUpperUpLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthUpperUpRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthUpperUpRight)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthLowerDownLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthLowerDownLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthLowerDownRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthLowerDownRight)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthUpperInsideLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthUpperInside)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthUpperInsideRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthUpperInside)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthLowerInsideLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthLowerInside)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccMouthLowerInsideRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(mouthLowerInside)));

                // Tongue
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueLongStep1),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueLongStep1)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueLongStep2),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueLongStep2)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueRight)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueUp),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueUp)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueDown),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueDown)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueBulgeLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueUpLeftMorph)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueUp),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueUpLeftMorph)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueBulgeRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueUpRightMorph)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueUp),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueUpRightMorph)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueBulgeLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueDownLeftMorph)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueDown),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueDownLeftMorph)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueBulgeRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueDownRightMorph)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccTongueDown),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(tongueDownRightMorph)));

                // Cheeks
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccCheekPuffLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(cheekPuffLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccCheekPuffRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(cheekPuffRight)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccCheekSuckLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(cheekSuck)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccCheekSuckRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(cheekSuck)));

                // Eyes
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeLeftBlink),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeLeftBlink)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeLeftWide),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeLeftWide)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeLeftRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeLeftRight)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeLeftLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeLeftLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeLeftUp),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeLeftUp)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeLeftDown),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeLeftDown)));

                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeRightBlink),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(ccEyeRightDown)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeRightWide),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeRightWide)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeRightRight),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeRightRight)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeRightLeft),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(ccEyeRightLeft)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeRightUp),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeRightUp)));
                targetSkinnedRenderer.SetBlendShapeWeight(BlendShapeByString(ccEyeRightDown),
                    targetSkinnedRenderer.GetBlendShapeWeight(BlendShapeByString(eyeRightDown)));
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