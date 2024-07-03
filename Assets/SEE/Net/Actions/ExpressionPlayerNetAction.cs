using UMA.PoseTools;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Updates values of the <see cref="UMAExpressionPlayer"/> on all clients.
    /// </summary>
    public class ExpressionPlayerNetAction : AbstractNetAction
    {
        /// <summary>
        /// The network object ID of the spawned avatar. Not to be confused
        /// with a network client ID.
        /// </summary>
        public ulong NetworkObjectID;

        // ExpressionPlayer Jaw variables.
        public float JawOpenClose;
        public float JawForwardBackward;
        public float JawLeftRight;

        // ExpressionPlayer Mouth variables.
        public float MouthLeftRight;
        public float MouthUpDown;
        public float MouthNarrowPucker;
        public float LeftMouthSmileFrown;
        public float RightMouthSmileFrown;
        public float LeftLowerLipUpDown;
        public float RightLowerLipUpDown;
        public float LeftUpperLipUpDown;
        public float RightUpperLipUpDown;

        // ExpressionPlayer Cheek variables.
        public float LeftCheekPuffSquint;
        public float RightCheekPuffSquint;

        // ExpressionPlayer Tongue variables.
        public float TongueOut;
        public float TongueCurl;
        public float TongueUpDown;
        public float TongueLeftRight;
        public float TongueWideNarror;

        /// <summary>
        /// Initializes all variables that should be transferred to the remote avatars.
        /// </summary>
        /// <param name="expressionPlayer">The ExpressionPlayer to be synchronized</param>
        /// <param name="networkObjectID">network object ID of the spawned avatar game object</param>
        public ExpressionPlayerNetAction(UMAExpressionPlayer expressionPlayer, ulong networkObjectID)
        {
            NetworkObjectID = networkObjectID;
            JawOpenClose = expressionPlayer.jawOpen_Close;
            JawForwardBackward = expressionPlayer.jawForward_Back;
            JawLeftRight = expressionPlayer.jawLeft_Right;
            MouthLeftRight = expressionPlayer.mouthLeft_Right;
            MouthUpDown = expressionPlayer.mouthUp_Down;
            MouthNarrowPucker = expressionPlayer.mouthNarrow_Pucker;
            LeftMouthSmileFrown = expressionPlayer.leftMouthSmile_Frown;
            RightMouthSmileFrown = expressionPlayer.rightMouthSmile_Frown;
            LeftLowerLipUpDown = expressionPlayer.leftLowerLipUp_Down;
            RightLowerLipUpDown = expressionPlayer.rightLowerLipUp_Down;
            LeftUpperLipUpDown = expressionPlayer.leftUpperLipUp_Down;
            RightUpperLipUpDown = expressionPlayer.rightUpperLipUp_Down;
            LeftCheekPuffSquint = expressionPlayer.leftCheekPuff_Squint;
            RightCheekPuffSquint = expressionPlayer.rightCheekPuff_Squint;
            TongueOut = expressionPlayer.tongueOut;
            TongueCurl = expressionPlayer.tongueCurl;
            TongueUpDown = expressionPlayer.tongueUp_Down;
            TongueLeftRight = expressionPlayer.tongueLeft_Right;
            TongueWideNarror = expressionPlayer.tongueWide_Narrow;
        }

        /// <summary>
        /// If executed by the initiating client, nothing happens. Otherwise the values of the
        /// <see cref="UMAExpressionPlayer"/> are transmitted.
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
                    if (networkObject.gameObject.TryGetComponent(out UMAExpressionPlayer expressionPlayer))
                    {
                        expressionPlayer.jawOpen_Close = JawOpenClose;
                        expressionPlayer.jawForward_Back = JawForwardBackward;
                        expressionPlayer.jawLeft_Right = JawLeftRight;
                        expressionPlayer.mouthLeft_Right = MouthLeftRight;
                        expressionPlayer.mouthUp_Down = MouthUpDown;
                        expressionPlayer.mouthNarrow_Pucker = MouthNarrowPucker;
                        expressionPlayer.leftMouthSmile_Frown = LeftMouthSmileFrown;
                        expressionPlayer.rightMouthSmile_Frown = RightMouthSmileFrown;
                        expressionPlayer.leftLowerLipUp_Down = LeftLowerLipUpDown;
                        expressionPlayer.rightLowerLipUp_Down = RightLowerLipUpDown;
                        expressionPlayer.leftUpperLipUp_Down = LeftUpperLipUpDown;
                        expressionPlayer.rightUpperLipUp_Down = RightUpperLipUpDown;
                        expressionPlayer.leftCheekPuff_Squint = LeftCheekPuffSquint;
                        expressionPlayer.rightCheekPuff_Squint = RightCheekPuffSquint;
                        expressionPlayer.tongueOut = TongueOut;
                        expressionPlayer.tongueCurl = TongueCurl;
                        expressionPlayer.tongueUp_Down = TongueUpDown;
                        expressionPlayer.tongueLeft_Right = TongueLeftRight;
                        expressionPlayer.tongueWide_Narrow = TongueWideNarror;
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