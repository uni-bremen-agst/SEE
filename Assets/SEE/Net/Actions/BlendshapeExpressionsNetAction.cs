using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Updates values of the <see cref="SkinnedMeshRenderer"/> on all clients.
    /// </summary>
    public class BlendshapeExpressionsNetAction : AbstractNetAction
    {
        /// <summary>
        /// The network object ID of the spawned avatar. Not to be confused
        /// with a network client ID.
        /// </summary>
        public ulong NetworkObjectID;

        /// <summary>
        /// Initialise variables to store the values of the Blendshapes.
        /// </summary>
        // Jaw
        public float JawOpenValue;
        public float JawForwardValue;
        public float JawLValue;
        public float JawRValue;
        public float JawUpValue;
        public float JawDownValue;
        public float JawBackwardValue;

        // Brows
        public float BrowRaiseInnerLValue;
        public float BrowRaiseInnerRValue;
        public float BrowRaiseOuterLValue;
        public float BrowRaiseOuterRValue;
        public float BrowDropLValue;
        public float BrowDropRValue;
        public float BrowCompressLValue;
        public float BrowCompressRValue;

        // Nose
        public float NoseSneerLValue;
        public float NoseSneerRValue;
        public float NoseNostrilRaiseLValue;
        public float NoseNostrilRaiseRValue;
        public float NoseNostrilDilateLValue;
        public float NoseNostrilDilateRValue;
        public float NoseCreaseLValue;
        public float NoseCreaseRValue;
        public float NoseNostrilDownLValue;
        public float NoseNostrilDownRValue;
        public float NoseNostrilInLValue;
        public float NoseNostrilInRValue;
        public float NoseTipLValue;
        public float NoseTipRValue;
        public float NoseTipUpValue;
        public float NoseTipDownValue;

        // Cheek
        public float CheekRaiseLValue;
        public float CheekRaiseRValue;
        public float CheekSuckLValue;
        public float CheekSuckRValue;
        public float CheekPuffLValue;
        public float CheekPuffRValue;

        // Mouth
        public float MergedOpenMouthValue;
        public float MouthSmileLValue;
        public float MouthSmileRValue;
        public float MouthSmileSharpLValue;
        public float MouthSmileSharpRValue;
        public float MouthFrownLValue;
        public float MouthFrownRValue;
        public float MouthStretchLValue;
        public float MouthStretchRValue;
        public float MouthDimpleLValue;
        public float MouthDimpleRValue;
        public float MouthPressLValue;
        public float MouthPressRValue;
        public float MouthTightenLValue;
        public float MouthTightenRValue;
        public float MouthBlowLValue;
        public float MouthBlowRValue;
        public float MouthPuckerUpLValue;
        public float MouthPuckerUpRValue;
        public float MouthPuckerDownLValue;
        public float MouthPuckerDownRValue;
        public float MouthFunnelUpLValue;
        public float MouthFunnelUpRValue;
        public float MouthFunnelDownLValue;
        public float MouthFunnelDownRValue;
        public float MouthRollInUpperLValue;
        public float MouthRollInUpperRValue;
        public float MouthRollInLowerLValue;
        public float MouthRollInLowerRValue;
        public float MouthRollOutUpperLValue;
        public float MouthRollOutUpperRValue;
        public float MouthRollOutLowerLValue;
        public float MouthRollOutLowerRValue;
        public float MouthPushUpperLValue;
        public float MouthPushUpperRValue;
        public float MouthPushLowerLValue;
        public float MouthPushLowerRValue;
        public float MouthPullUpperLValue;
        public float MouthPullUpperRValue;
        public float MouthPullLowerLValue;
        public float MouthPullLowerRValue;
        public float MouthLValue;
        public float MouthRValue;
        public float MouthUpValue;
        public float MouthDownValue;
        public float MouthUpperLValue;
        public float MouthUpperRValue;
        public float MouthLowerLValue;
        public float MouthLowerRValue;
        public float MouthShrugUpperValue;
        public float MouthShrugLowerValue;
        public float MouthDropUpperValue;
        public float MouthDropLowerValue;
        public float MouthUpUpperLValue;
        public float MouthUpUpperRValue;
        public float MouthDownLowerLValue;
        public float MouthDownLowerRValue;
        public float MouthChinUpValue;
        public float MouthCloseValue;
        public float MouthContractValue;

        // Tongue
        public float TongueNarrowValue;
        public float TongueWideValue;
        public float TongueRollValue;
        public float TongueLValue;
        public float TongueRValue;
        public float TongueTipLValue;
        public float TongueTipRValue;
        public float TongueTwistLValue;
        public float TongueTwistRValue;
        public float TongueBulgeRValue;
        public float TongueBulgeLValue;
        public float TongueExtendValue;
        public float TongueEnlargeValue;

        /// <summary>
        /// String literals of the original Blendshape names of the CC4 avatar.
        /// </summary>
        // Jaw
        public const string ccJawOpen = "Jaw_Open";
        public const string ccJawForward = "Jaw_Forward";
        public const string ccJawL = "Jaw_L";
        public const string ccJawR = "Jaw_R";
        public const string ccJawUp = "Jaw_Up";
        public const string ccJawDown = "Jaw_Down";
        public const string ccJawBackward = "Jaw_Backward";

        // Brow
        public const string ccBrowRaiseInnerL = "Brow_Raise_Inner_L";
        public const string ccBrowRaiseInnerR = "Brow_Raise_Inner_R";
        public const string ccBrowRaiseOuterL = "Brow_Raise_Outer_L";
        public const string ccBrowRaiseOuterR = "Brow_Raise_Outer_R";
        public const string ccBrowDropL = "Brow_Drop_L";
        public const string ccBrowDropR = "Brow_Drop_R";
        public const string ccBrowCompressL = "Brow_Compress_L";
        public const string ccBrowCompressR = "Brow_Compress_R";

        // Nose
        public const string ccNoseSneerL = "Nose_Sneer_L";
        public const string ccNoseSneerR = "Nose_Sneer_R";
        public const string ccNoseNostrilRaiseL = "Nose_Nostril_Raise_L";
        public const string ccNoseNostrilRaiseR = "Nose_Nostril_Raise_R";
        public const string ccNoseNostrilDilateL = "Nose_Nostril_Dilate_L";
        public const string ccNoseNostrilDilateR = "Nose_Nostril_Dilate_R";
        public const string ccNoseCreaseL = "Nose_Crease_L";
        public const string ccNoseCreaseR = "Nose_Crease_R";
        public const string ccNoseNostrilDownL = "Nose_Nostril_Down_L";
        public const string ccNoseNostrilDownR = "Nose_Nostril_Down_R";
        public const string ccNoseNostrilInL = "Nose_Nostril_In_L";
        public const string ccNoseNostrilInR = "Nose_Nostril_In_R";
        public const string ccNoseTipL = "Nose_Tip_L";
        public const string ccNoseTipR = "Nose_Tip_R";
        public const string ccNoseTipUp = "Nose_Tip_Up";
        public const string ccNoseTipDown = "Nose_Tip_Down";

        // Cheek
        public const string ccCheekRaiseL = "Cheek_Raise_L";
        public const string ccCheekRaiseR = "Cheek_Raise_R";
        public const string ccCheekSuckL = "Cheek_Suck_L";
        public const string ccCheekSuckR = "Cheek_Suck_R";
        public const string ccCheekPuffL = "Cheek_Puff_L";
        public const string ccCheekPuffR = "Cheek_Puff_R";

        // Mouth
        public const string ccMergedOpenMouth = "Merged_Open_Mouth";
        public const string ccMouthSmileL = "Mouth_Smile_L";
        public const string ccMouthSmileR = "Mouth_Smile_R";
        public const string ccMouthSmileSharpL = "Mouth_Smile_Sharp_L";
        public const string ccMouthSmileSharpR = "Mouth_Smile_Sharp_R";
        public const string ccMouthFrownL = "Mouth_Frown_L";
        public const string ccMouthFrownR = "Mouth_Frown_R";
        public const string ccMouthStretchL = "Mouth_Stretch_L";
        public const string ccMouthStretchR = "Mouth_Stretch_R";
        public const string ccMouthDimpleL = "Mouth_Dimple_L";
        public const string ccMouthDimpleR = "Mouth_Dimple_R";
        public const string ccMouthPressL = "Mouth_Press_L";
        public const string ccMouthPressR = "Mouth_Press_R";
        public const string ccMouthTightenL = "Mouth_Tighten_L";
        public const string ccMouthTightenR = "Mouth_Tighten_R";
        public const string ccMouthBlowL = "Mouth_Blow_L";
        public const string ccMouthBlowR = "Mouth_Blow_R";
        public const string ccMouthPuckerUpL = "Mouth_Pucker_Up_L";
        public const string ccMouthPuckerUpR = "Mouth_Pucker_Up_R";
        public const string ccMouthPuckerDownL = "Mouth_Pucker_Down_L";
        public const string ccMouthPuckerDownR = "Mouth_Pucker_Down_R";
        public const string ccMouthFunnelUpL = "Mouth_Funnel_Up_L";
        public const string ccMouthFunnelUpR = "Mouth_Funnel_Up_R";
        public const string ccMouthFunnelDownL = "Mouth_Funnel_Down_L";
        public const string ccMouthFunnelDownR = "Mouth_Funnel_Down_R";
        public const string ccMouthRollInUpperL = "Mouth_Roll_In_Upper_L";
        public const string ccMouthRollInUpperR = "Mouth_Roll_In_Upper_R";
        public const string ccMouthRollInLowerL = "Mouth_Roll_In_Lower_L";
        public const string ccMouthRollInLowerR = "Mouth_Roll_In_Lower_R";
        public const string ccMouthRollOutUpperL = "Mouth_Roll_Out_Upper_L";
        public const string ccMouthRollOutUpperR = "Mouth_Roll_Out_Upper_R";
        public const string ccMouthRollOutLowerL = "Mouth_Roll_Out_Lower_L";
        public const string ccMouthRollOutLowerR = "Mouth_Roll_Out_Lower_R";
        public const string ccMouthPushUpperL = "Mouth_Push_Upper_L";
        public const string ccMouthPushUpperR = "Mouth_Push_Upper_R";
        public const string ccMouthPushLowerL = "Mouth_Push_Lower_L";
        public const string ccMouthPushLowerR = "Mouth_Push_Lower_R";
        public const string ccMouthPullUpperL = "Mouth_Pull_Upper_L";
        public const string ccMouthPullUpperR = "Mouth_Pull_Upper_R";
        public const string ccMouthPullLowerL = "Mouth_Pull_Lower_L";
        public const string ccMouthPullLowerR = "Mouth_Pull_Lower_R";
        public const string ccMouthL = "Mouth_L";
        public const string ccMouthR = "Mouth_R";
        public const string ccMouthUp = "Mouth_Up";
        public const string ccMouthDown = "Mouth_Down";
        public const string ccMouthUpperL = "Mouth_Upper_L";
        public const string ccMouthUpperR = "Mouth_Upper_R";
        public const string ccMouthLowerL = "Mouth_Lower_L";
        public const string ccMouthLowerR = "Mouth_Lower_R";
        public const string ccMouthShrugUpper = "Mouth_Shrug_Upper";
        public const string ccMouthShrugLower = "Mouth_Shrug_Lower";
        public const string ccMouthDropUpper = "Mouth_Drop_Upper";
        public const string ccMouthDropLower = "Mouth_Drop_Lower";
        public const string ccMouthUpUpperL = "Mouth_Up_Upper_L";
        public const string ccMouthUpUpperR = "Mouth_Up_Upper_R";
        public const string ccMouthDownLowerL = "Mouth_Down_Lower_L";
        public const string ccMouthDownLowerR = "Mouth_Down_Lower_R";
        public const string ccMouthChinUp = "Mouth_Chin_Up";
        public const string ccMouthClose = "Mouth_Close";
        public const string ccMouthContract = "Mouth_Contract";

        // Tongue
        public const string ccTongueNarrow = "Tongue_Narrow";
        public const string ccTongueWide = "Tongue_Wide";
        public const string ccTongueRoll = "Tongue_Roll";
        public const string ccTongueL = "Tongue_L";
        public const string ccTongueR = "Tongue_R";
        public const string ccTongueTipL = "Tongue_Tip_L";
        public const string ccTongueTipR = "Tongue_Tip_R";
        public const string ccTongueTwistL = "Tongue_Twist_L";
        public const string ccTongueTwistR = "Tongue_Twist_R";
        public const string ccTongueBulgeR = "Tongue_Bulge_R";
        public const string ccTongueBulgeL = "Tongue_Bulge_L";
        public const string ccTongueExtend = "Tongue_Extend";
        public const string ccTongueEnlarge = "Tongue_Enlarge";


        /*
        // TODO: simplification suggested by falko
        private readonly List<string> blendShapeNames = new List<string>
        {
            JawLeftName, JawRightName, JawForwardName, JawOpenName, MouthApeShapeName, MouthUpperLeftName, MouthUpperRightName, MouthLowerLeftName, MouthLowerRightName,
            MouthUpperOverturnName, MouthLowerOverturnName, MouthPoutName, MouthSmileLeftName, MouthSmileRightName, MouthSadLeftName, MouthSadRightName,
            CheekPuffLeftName, CheekPuffRightName, CheekSuckName, MouthUpperUpLeftName, MouthUpperUpRightName, MouthLowerDownLeftName, MouthLowerDownRightName,
            MouthUpperInsideName, MouthLowerInsideName, MouthLowerOverlayName, EyeLeftBlinkName, EyeLeftWideName, EyeLeftRightName, EyeLeftLeftName, EyeLeftUpName,
            EyeLeftDownName, EyeRightBlinkName, EyeRightWideName, EyeRightRightName, EyeRightLeftName, EyeRightUpName, EyeRightDownName, EyeFrownName, EyeLeftSqueezeName,
            EyeRightSqueezeName, TongueLongStep1Name, TongueLongStep2Name, TongueLeftName, TongueRightName, TongueUpName, TongueDownName, TongueRollName,
            TongueUpLeftMorphName, TongueUpRightMorphName, TongueDownLeftMorphName, TongueDownRightMorphName
        };
        private Dictionary<string, float> blendShapes;
        */

        /// <summary>
        /// Initializes all variables that should be transferred to the remote avatars.
        /// </summary>
        /// <param name="skinnedMeshRenderer">The skinnedMeshRenderer to be synchronized.</param>
        /// <param name="networkObjectID">Network object ID of the spawned avatar game object.</param>
        public BlendshapeExpressionsNetAction(SkinnedMeshRenderer skinnedMeshRenderer, ulong networkObjectID)
        {
            NetworkObjectID = networkObjectID;
            //blendShapes = blendShapeNames.ToDictionary(name => name, name => skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(name)));

            // Jaw
            JawOpenValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawOpen));
            JawForwardValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawForward));
            JawLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawL));
            JawRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawR));
            JawUpValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawUp));
            JawDownValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawDown));
            JawBackwardValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawBackward));

            // Brow
            BrowRaiseInnerLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowRaiseInnerL));
            BrowRaiseInnerRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowRaiseInnerR));
            BrowRaiseOuterLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowRaiseOuterL));
            BrowRaiseOuterRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowRaiseOuterR));
            BrowDropLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowDropL));
            BrowDropRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowDropR));
            BrowCompressLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowCompressL));
            BrowCompressRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowCompressR));

            // Nose
            NoseSneerLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseSneerL));
            NoseSneerRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseSneerR));
            NoseNostrilRaiseLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilRaiseL));
            NoseNostrilRaiseRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilRaiseR));
            NoseNostrilDilateLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilDilateL));
            NoseNostrilDilateRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilDilateR));
            NoseCreaseLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseCreaseL));
            NoseCreaseRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseCreaseR));
            NoseNostrilDownLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilDownL));
            NoseNostrilDownRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilDownR));
            NoseNostrilInLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilInL));
            NoseNostrilInRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilInR));
            NoseTipLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseTipL));
            NoseTipRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseTipR));
            NoseTipUpValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseTipUp));
            NoseTipDownValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseTipDown));

            // Cheek
            CheekRaiseLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekRaiseL));
            CheekRaiseRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekRaiseR));
            CheekSuckLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekSuckL));
            CheekSuckRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekSuckR));
            CheekPuffLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekPuffL));
            CheekPuffRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekPuffR));

            // Mouth
            MergedOpenMouthValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMergedOpenMouth));
            MouthSmileLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthSmileL));
            MouthSmileRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthSmileR));
            MouthSmileSharpLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthSmileSharpL));
            MouthSmileSharpRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthSmileSharpR));
            MouthFrownLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFrownL));
            MouthFrownRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFrownR));
            MouthStretchLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthStretchL));
            MouthStretchRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthStretchR));
            MouthDimpleLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDimpleL));
            MouthDimpleRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDimpleR));
            MouthPressLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPressL));
            MouthPressRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPressR));
            MouthTightenLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthTightenL));
            MouthTightenRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthTightenR));
            MouthBlowLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthBlowL));
            MouthBlowRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthBlowR));
            MouthPuckerUpLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerUpL));
            MouthPuckerUpRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerUpR));
            MouthPuckerDownLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerDownL));
            MouthPuckerDownRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerDownR));
            MouthFunnelUpLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFunnelUpL));
            MouthFunnelUpRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFunnelUpR));
            MouthFunnelDownLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFunnelDownL));
            MouthFunnelDownRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFunnelDownR));
            MouthRollInUpperLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollInUpperL));
            MouthRollInUpperRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollInUpperR));
            MouthRollInLowerLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollInLowerL));
            MouthRollInLowerRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollInLowerR));
            MouthRollOutUpperLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollOutUpperL));
            MouthRollOutUpperRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollOutUpperR));
            MouthRollOutLowerLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollOutLowerL));
            MouthRollOutLowerRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollOutLowerR));
            MouthPushUpperLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPushUpperL));
            MouthPushUpperRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPushUpperR));
            MouthPushLowerLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPushLowerL));
            MouthPushLowerRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPushLowerR));
            MouthPullUpperLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPullUpperL));
            MouthPullUpperRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPullUpperR));
            MouthPullLowerLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPullLowerL));
            MouthPullLowerRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPullLowerR));
            MouthLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthL));
            MouthRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthR));
            MouthUpValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUp));
            MouthDownValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDown));
            MouthUpperLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpperL));
            MouthUpperRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpperR));
            MouthLowerLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthLowerL));
            MouthLowerRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthLowerR));
            MouthShrugUpperValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthShrugUpper));
            MouthShrugLowerValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthShrugLower));
            MouthDropUpperValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDropUpper));
            MouthDropLowerValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDropLower));
            MouthUpUpperLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpUpperL));
            MouthUpUpperRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpUpperR));
            MouthDownLowerLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDownLowerL));
            MouthDownLowerRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDownLowerR));
            MouthChinUpValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthChinUp));
            MouthCloseValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthClose));
            MouthContractValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthContract));

            // Tongue
            TongueNarrowValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueNarrow));
            TongueWideValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueWide));
            TongueRollValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueRoll));
            TongueLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueL));
            TongueRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueR));
            TongueTipLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueTipL));
            TongueTipRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueTipR));
            TongueTwistLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueTwistL));
            TongueTwistRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueTwistR));
            TongueBulgeRValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueBulgeR));
            TongueBulgeLValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueBulgeL));
            TongueExtendValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueExtend));
            TongueEnlargeValue = skinnedMeshRenderer.GetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueEnlarge));
        }

        /// <summary>
        /// If executed by the initiating client, nothing happens. Otherwise the values of the
        /// <see cref="SkinnedMeshRenderer"/> are transmitted.
        /// </summary>
        public override void ExecuteOnClient()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager != null)
            {
                NetworkSpawnManager networkSpawnManager = networkManager.SpawnManager;
                if (networkSpawnManager.SpawnedObjects.TryGetValue(NetworkObjectID,
                        out NetworkObject networkObject))
                {
                    Transform ccBaseBody = networkObject.gameObject.transform.Find("CC_Base_Body");
                    if (ccBaseBody.gameObject != null)
                    {
                        if (ccBaseBody.gameObject.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
                        {
                            // Jaw
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawOpen), JawOpenValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawForward), JawForwardValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawL), JawLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawR), JawRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawUp), JawUpValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawDown), JawDownValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccJawBackward), JawBackwardValue);

                            // Brow
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowRaiseInnerL), BrowRaiseInnerLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowRaiseInnerR), BrowRaiseInnerRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowRaiseOuterL), BrowRaiseOuterLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowRaiseOuterR), BrowRaiseOuterRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowDropL), BrowDropLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowDropR), BrowDropRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowCompressL), BrowCompressLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccBrowCompressR), BrowCompressRValue);

                            // Nose
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseSneerL), NoseSneerLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseSneerR), NoseSneerRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilRaiseL), NoseNostrilRaiseLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilRaiseR), NoseNostrilRaiseRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilDilateL), NoseNostrilDilateLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilDilateR), NoseNostrilDilateRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseCreaseL), NoseCreaseLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseCreaseR), NoseCreaseRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilDownL), NoseNostrilDownLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilDownR), NoseNostrilDownRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilInL), NoseNostrilInLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseNostrilInR), NoseNostrilInRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseTipL), NoseTipLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseTipR), NoseTipRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseTipUp), NoseTipUpValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccNoseTipDown), NoseTipDownValue);

                            // Cheek
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekRaiseL), CheekRaiseLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekRaiseR), CheekRaiseRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekSuckL), CheekSuckLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekSuckR), CheekSuckRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekPuffL), CheekPuffLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccCheekPuffR), CheekPuffRValue);

                            // Mouth
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMergedOpenMouth), MergedOpenMouthValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthSmileL), MouthSmileLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthSmileR), MouthSmileRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthSmileSharpL), MouthSmileSharpLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthSmileSharpR), MouthSmileSharpRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFrownL), MouthFrownLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFrownR), MouthFrownRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthStretchL), MouthStretchLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthStretchR), MouthStretchRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDimpleL), MouthDimpleLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDimpleR), MouthDimpleRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPressL), MouthPressLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPressR), MouthPressRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthTightenL), MouthTightenLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthTightenR), MouthTightenRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthBlowL), MouthBlowLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthBlowR), MouthBlowRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerUpL), MouthPuckerUpLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerUpR), MouthPuckerUpRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerDownL), MouthPuckerDownLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPuckerDownR), MouthPuckerDownRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFunnelUpL), MouthFunnelUpLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFunnelUpR), MouthFunnelUpRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFunnelDownL), MouthFunnelDownLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthFunnelDownR), MouthFunnelDownRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollInUpperL), MouthRollInUpperLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollInUpperR), MouthRollInUpperRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollInLowerL), MouthRollInLowerLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollInLowerR), MouthRollInLowerRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollOutUpperL), MouthRollOutUpperLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollOutUpperR), MouthRollOutUpperRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollOutLowerL), MouthRollOutLowerLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthRollOutLowerR), MouthRollOutLowerRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPushUpperL), MouthPushUpperLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPushUpperR), MouthPushUpperRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPushLowerL), MouthPushLowerLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPushLowerR), MouthPushLowerRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPullUpperL), MouthPullUpperLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPullUpperR), MouthPullUpperRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPullLowerL), MouthPullLowerLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthPullLowerR), MouthPullLowerRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthL), MouthLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthR), MouthRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUp), MouthUpValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDown), MouthDownValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpperL), MouthUpperLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpperR), MouthUpperRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthLowerL), MouthLowerLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthLowerR), MouthLowerRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthShrugUpper), MouthShrugUpperValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthShrugLower), MouthShrugLowerValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDropUpper), MouthDropUpperValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDropLower), MouthDropLowerValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpUpperL), MouthUpUpperLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthUpUpperR), MouthUpUpperRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDownLowerL), MouthDownLowerLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthDownLowerR), MouthDownLowerRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthChinUp), MouthChinUpValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthClose), MouthCloseValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccMouthContract), MouthContractValue);

                            // Tongue
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueNarrow), TongueNarrowValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueWide), TongueWideValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueRoll), TongueRollValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueL), TongueLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueR), TongueRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueTipL), TongueTipLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueTipR), TongueTipRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueTwistL), TongueTwistLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueTwistR), TongueTwistRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueBulgeR), TongueBulgeRValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueBulgeL), TongueBulgeLValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueExtend), TongueExtendValue);
                            skinnedMeshRenderer.SetBlendShapeWeight(skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(ccTongueEnlarge), TongueEnlargeValue);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"There is no network object with ID {NetworkObjectID}.\n");
                }
            }
            else
            {
                Debug.LogError($"There is no component {typeof(NetworkManager)} in the scene.\n");
            }
        }
    }
}
