using UMA.PoseTools;
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

        // SkinnedMeshRenderer Varibales
        // Jaw
        public float JawLeft;
        public float JawRight;
        public float JawForward;
        public float JawOpen;

        // Mouth
        public float MouthApeShape;
        public float MouthUpperLeft;
        public float MouthUpperRight;
        public float MouthLowerLeft;
        public float MouthLowerRight;
        public float MouthUpperOverturn;
        public float MouthLowerOverturn;
        public float MouthPout;
        public float MouthSmileLeft;
        public float MouthSmileRight;
        public float MouthSadLeft;
        public float MouthSadRight;
        public float MouthUpperUpLeft;
        public float MouthUpperUpRight;
        public float MouthLowerDownLeft;
        public float MouthLowerDownRight;
        public float MouthUpperInside;
        public float MouthLowerInside;
        public float MouthLowerOverlay;

        // Cheek
        public float CheekPuffLeft;
        public float CheekPuffRight;
        public float CheekSuck;

        // Eye
        public float EyeLeftBlink;
        public float EyeLeftWide;
        public float EyeLeftRight;
        public float EyeLeftLeft;
        public float EyeLeftUp;
        public float EyeLeftDown;
        public float EyeRightBlink;
        public float EyeRightWide;
        public float EyeRightRight;
        public float EyeRightLeft;
        public float EyeRightUp;
        public float EyeRightDown;
        public float EyeFrown;
        public float EyeLeftSqueeze;
        public float EyeRightSqueeze;

        // Tongue
        public float TongueLongStep1;
        public float TongueLongStep2;
        public float TongueLeft;
        public float TongueRight;
        public float TongueUp;
        public float TongueDown;
        public float TongueRoll;
        public float TongueUpLeftMorph;
        public float TongueUpRightMorph;
        public float TongueDownLeftMorph;
        public float TongueDownRightMorph;

        // BlendShape name
        public const string JawLeftName = "Jaw_Left";
        public const string JawRightName = "Jaw_Right";
        public const string JawForwardName = "Jaw_Forward";
        public const string JawOpenName = "Jaw_Open";

        // Mouth names
        public const string MouthApeShapeName = "Mouth_Ape_Shape";
        public const string MouthUpperLeftName = "Mouth_Upper_Left";
        public const string MouthUpperRightName = "Mouth_Upper_Right";
        public const string MouthLowerLeftName = "Mouth_Lower_Left";
        public const string MouthLowerRightName = "Mouth_Lower_Right";
        public const string MouthUpperOverturnName = "Mouth_Upper_Overturn";
        public const string MouthLowerOverturnName = "Mouth_Lower_Overturn";
        public const string MouthPoutName = "Mouth_Pout";
        public const string MouthSmileLeftName = "Mouth_Smile_Left";
        public const string MouthSmileRightName = "Mouth_Smile_Right";
        public const string MouthSadLeftName = "Mouth_Sad_Left";
        public const string MouthSadRightName = "Mouth_Sad_Right";
        public const string MouthUpperUpLeftName = "Mouth_Upper_UpLeft";
        public const string MouthUpperUpRightName = "Mouth_Upper_UpRight";
        public const string MouthLowerDownLeftName = "Mouth_Lower_DownLeft";
        public const string MouthLowerDownRightName = "Mouth_Lower_DownRight";
        public const string MouthUpperInsideName = "Mouth_Upper_Inside";
        public const string MouthLowerInsideName = "Mouth_Lower_Inside";
        public const string MouthLowerOverlayName = "Mouth_Lower_Overlay";

        // Cheek names
        public const string CheekPuffLeftName = "Cheek_Puff_Left";
        public const string CheekPuffRightName = "Cheek_Puff_Right";
        public const string CheekSuckName = "Cheek_Suck";

        // Eye names
        public const string EyeLeftBlinkName = "Eye_Left_Blink";
        public const string EyeLeftWideName = "Eye_Left_Wide";
        public const string EyeLeftRightName = "Eye_Left_Right";
        public const string EyeLeftLeftName = "Eye_Left_Left";
        public const string EyeLeftUpName = "Eye_Left_Up";
        public const string EyeLeftDownName = "Eye_Left_Down";
        public const string EyeRightBlinkName = "Eye_Right_Blink";
        public const string EyeRightWideName = "Eye_Right_Wide";
        public const string EyeRightRightName = "Eye_Right_Right";
        public const string EyeRightLeftName = "Eye_Right_Left";
        public const string EyeRightUpName = "Eye_Right_Up";
        public const string EyeRightDownName = "Eye_Right_Down";
        public const string EyeFrownName = "Eye_Frown";
        public const string EyeLeftSqueezeName = "Eye_Left_squeeze";
        public const string EyeRightSqueezeName = "Eye_Right_squeeze";

        // Tongue names
        public const string TongueLongStep1Name = "Tongue_LongStep1";
        public const string TongueLongStep2Name = "Tongue_LongStep2";
        public const string TongueLeftName = "Tongue_Left";
        public const string TongueRightName = "Tongue_Right";
        public const string TongueUpName = "Tongue_Up";
        public const string TongueDownName = "Tongue_Down";
        public const string TongueRollName = "Tongue_Roll";
        public const string TongueUpLeftMorphName = "Tongue_UpLeft_Morph";
        public const string TongueUpRightMorphName = "Tongue_UpRight_Morph";
        public const string TongueDownLeftMorphName = "Tongue_DownLeft_Morph";
        public const string TongueDownRightMorphName = "Tongue_DownRight_Morph";

        /// <summary>
        /// Initializes all variables that should be transferred to the remote avatars.
        /// </summary>
        /// <param name="expressionPlayer">The ExpressionPlayer to be synchronized</param>
        /// <param name="networkObjectID">network object ID of the spawned avatar game object</param>
        public BlendshapeExpressionsNetAction(SkinnedMeshRenderer skinnedMeshRenderer, ulong networkObjectID)
        {
            NetworkObjectID = networkObjectID;
            JawLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(JawLeftName, skinnedMeshRenderer));
            JawRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(JawRightName, skinnedMeshRenderer));
            JawForward = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(JawForwardName, skinnedMeshRenderer));
            JawOpen = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(JawOpenName, skinnedMeshRenderer));
            MouthApeShape = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthApeShapeName, skinnedMeshRenderer));
            MouthUpperLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperLeftName, skinnedMeshRenderer));
            MouthUpperRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperRightName, skinnedMeshRenderer));
            MouthLowerLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerLeftName, skinnedMeshRenderer));
            MouthLowerRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerRightName, skinnedMeshRenderer));
            MouthUpperOverturn = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperOverturnName, skinnedMeshRenderer));
            MouthLowerOverturn = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerOverturnName, skinnedMeshRenderer));
            MouthPout = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthPoutName, skinnedMeshRenderer));
            MouthSmileLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthSmileLeftName, skinnedMeshRenderer));
            MouthSmileRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthSmileRightName, skinnedMeshRenderer));
            MouthSadLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthSadLeftName, skinnedMeshRenderer));
            MouthSadRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthSadRightName, skinnedMeshRenderer));
            CheekPuffLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(CheekPuffLeftName, skinnedMeshRenderer));
            CheekPuffRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(CheekPuffRightName, skinnedMeshRenderer));
            CheekSuck = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(CheekSuckName, skinnedMeshRenderer));
            MouthUpperUpLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperUpLeftName, skinnedMeshRenderer));
            MouthUpperUpRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperUpRightName, skinnedMeshRenderer));
            MouthLowerDownLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerDownLeftName, skinnedMeshRenderer));
            MouthLowerDownRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerDownRightName, skinnedMeshRenderer));
            MouthUpperInside = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthUpperInsideName, skinnedMeshRenderer));
            MouthLowerInside = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerInsideName, skinnedMeshRenderer));
            MouthLowerOverlay = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(MouthLowerOverlayName, skinnedMeshRenderer));
            EyeLeftBlink = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeLeftBlinkName, skinnedMeshRenderer));
            EyeLeftWide = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeLeftWideName, skinnedMeshRenderer));
            EyeLeftRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeLeftRightName, skinnedMeshRenderer));
            EyeLeftLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeLeftLeftName, skinnedMeshRenderer));
            EyeLeftUp = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeLeftUpName, skinnedMeshRenderer));
            EyeLeftDown = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeLeftDownName, skinnedMeshRenderer));
            EyeRightBlink = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeRightBlinkName, skinnedMeshRenderer));
            EyeRightWide = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeRightWideName, skinnedMeshRenderer));
            EyeRightRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeRightRightName, skinnedMeshRenderer));
            EyeRightLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeRightLeftName, skinnedMeshRenderer));
            EyeRightUp = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeRightUpName, skinnedMeshRenderer));
            EyeRightDown = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeRightDownName, skinnedMeshRenderer));
            EyeFrown = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeFrownName, skinnedMeshRenderer));
            EyeLeftSqueeze = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeLeftSqueezeName, skinnedMeshRenderer));
            EyeRightSqueeze = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(EyeRightSqueezeName, skinnedMeshRenderer));
            TongueLongStep1 = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueLongStep1Name, skinnedMeshRenderer));
            TongueLongStep2 = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueLongStep2Name, skinnedMeshRenderer));
            TongueLeft = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueLeftName, skinnedMeshRenderer));
            TongueRight = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueRightName, skinnedMeshRenderer));
            TongueUp = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueUpName, skinnedMeshRenderer));
            TongueDown = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueDownName, skinnedMeshRenderer));
            TongueRoll = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueRollName, skinnedMeshRenderer));
            TongueUpLeftMorph = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueUpLeftMorphName, skinnedMeshRenderer));
            TongueUpRightMorph = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueUpRightMorphName, skinnedMeshRenderer));
            TongueDownLeftMorph = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueDownLeftMorphName, skinnedMeshRenderer));
            TongueDownRightMorph = skinnedMeshRenderer.GetBlendShapeWeight(BlendShapeByString(TongueDownRightMorphName, skinnedMeshRenderer));
        }

        /// <summary>
        /// Searches for a blendshape by name and returns the index of it.
        /// </summary>
        /// <param name="blendShapeName"></param>
        /// <returns>The index of Blendshape</returns>
        public int BlendShapeByString(string blendShapeName, SkinnedMeshRenderer skinnedMeshRenderer)
        {
            for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
            {
                if(skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i) == blendShapeName)
                {
                    return i;
                }
            }
            return -1;
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
                    if (networkObject.gameObject.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
                    {
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(JawLeftName, skinnedMeshRenderer), JawLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(JawRightName, skinnedMeshRenderer), JawRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(JawForwardName, skinnedMeshRenderer), JawForward);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(JawOpenName, skinnedMeshRenderer), JawOpen);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthApeShapeName, skinnedMeshRenderer), MouthApeShape);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthUpperLeftName, skinnedMeshRenderer), MouthUpperLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthUpperRightName, skinnedMeshRenderer), MouthUpperRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthLowerLeftName, skinnedMeshRenderer), MouthLowerLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthLowerRightName, skinnedMeshRenderer), MouthLowerRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthUpperOverturnName, skinnedMeshRenderer), MouthUpperOverturn);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthLowerOverturnName, skinnedMeshRenderer), MouthLowerOverturn);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthPoutName, skinnedMeshRenderer), MouthPout);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthSmileLeftName, skinnedMeshRenderer), MouthSmileLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthSmileRightName, skinnedMeshRenderer), MouthSmileRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthSadLeftName, skinnedMeshRenderer), MouthSadLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthSadRightName, skinnedMeshRenderer), MouthSadRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(CheekPuffLeftName, skinnedMeshRenderer), CheekPuffLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(CheekPuffRightName, skinnedMeshRenderer), CheekPuffRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(CheekSuckName, skinnedMeshRenderer), CheekSuck);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthUpperUpLeftName, skinnedMeshRenderer), MouthUpperUpLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthUpperUpRightName, skinnedMeshRenderer), MouthUpperUpRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthLowerDownLeftName, skinnedMeshRenderer), MouthLowerDownLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthLowerDownRightName, skinnedMeshRenderer), MouthLowerDownRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthUpperInsideName, skinnedMeshRenderer), MouthUpperInside);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthLowerInsideName, skinnedMeshRenderer), MouthLowerInside);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(MouthLowerOverlayName, skinnedMeshRenderer), MouthLowerOverlay);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeLeftBlinkName, skinnedMeshRenderer), EyeLeftBlink);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeLeftWideName, skinnedMeshRenderer), EyeLeftWide);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeLeftRightName, skinnedMeshRenderer), EyeLeftRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeLeftLeftName, skinnedMeshRenderer), EyeLeftLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeLeftUpName, skinnedMeshRenderer), EyeLeftUp);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeLeftDownName, skinnedMeshRenderer), EyeLeftDown);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeRightBlinkName, skinnedMeshRenderer), EyeRightBlink);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeRightWideName, skinnedMeshRenderer), EyeRightWide);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeRightRightName, skinnedMeshRenderer), EyeRightRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeRightLeftName, skinnedMeshRenderer), EyeRightLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeRightUpName, skinnedMeshRenderer), EyeRightUp);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeRightDownName, skinnedMeshRenderer), EyeRightDown);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeFrownName, skinnedMeshRenderer), EyeFrown);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeLeftSqueezeName, skinnedMeshRenderer), EyeLeftSqueeze);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(EyeRightSqueezeName, skinnedMeshRenderer), EyeRightSqueeze);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueLongStep1Name, skinnedMeshRenderer), TongueLongStep1);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueLongStep2Name, skinnedMeshRenderer), TongueLongStep2);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueLeftName, skinnedMeshRenderer), TongueLeft);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueRightName, skinnedMeshRenderer), TongueRight);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueUpName, skinnedMeshRenderer), TongueUp);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueDownName, skinnedMeshRenderer), TongueDown);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueRollName, skinnedMeshRenderer), TongueRoll);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueUpLeftMorphName, skinnedMeshRenderer), TongueUpLeftMorph);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueUpRightMorphName, skinnedMeshRenderer), TongueUpRightMorph);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueDownLeftMorphName, skinnedMeshRenderer), TongueDownLeftMorph);
                        skinnedMeshRenderer.SetBlendShapeWeight(BlendShapeByString(TongueDownRightMorphName, skinnedMeshRenderer), TongueDownRightMorph);
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

        /// <summary>
        /// Does not do anything.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}