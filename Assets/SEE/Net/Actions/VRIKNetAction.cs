using System;
using RootMotion.FinalIK;
using SEE.Game.Avatars;
using SEE.GO;
using UMA.CharacterSystem;
using UMA.PoseTools;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
{
    public class VRIKNetAction : AbstractNetAction
    {
        /// <summary>
        /// The network object ID of the spawned avatar. Not to be confused
        /// with a network client ID.
        /// </summary>
        public ulong NetworkObjectID;

        public VRIK Vrik;
        
        public VRIKNetAction(ulong networkObjectID, VRIK vrik)
        {
            NetworkObjectID = networkObjectID;
            Vrik = vrik;

        }
        
        /// <summary>
        /// If executed by the initiating client, nothing happens.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                NetworkManager networkManager = NetworkManager.Singleton;

                if (networkManager != null)
                {
                    NetworkSpawnManager networkSpawnManager = networkManager.SpawnManager;
                    if (networkSpawnManager.SpawnedObjects.TryGetValue(NetworkObjectID,
                            out NetworkObject networkObject))
                    {
                        if (networkObject.gameObject.TryGetComponent(out VRIK Vrik))
                        {
                            if (Vrik.solver.spine.headTarget.name != "Remote Head")
                            {
                                // Adds required components and gives starting parameters.
                                GameObject remoteHead = new GameObject();
                                remoteHead.name = "Remote Head";
                                remoteHead.transform.position =
                                    networkObject.gameObject.transform.Find("Head").transform.position;
                                
                                GameObject remoteLeftHand = new GameObject();
                                remoteLeftHand.name = "Remote Left Hand";
                                remoteLeftHand.transform.position =
                                    networkObject.gameObject.transform.Find("LeftArm").transform.position;
                                
                                GameObject remoteRightHand = new GameObject();
                                remoteRightHand.name = "Remote Right Hand";
                                remoteRightHand.transform.position =
                                    networkObject.gameObject.transform.Find("RightArm").transform.position;

                                // Connects required components.
                                Vrik.solver.spine.headTarget = remoteHead.transform;
                                Vrik.solver.leftArm.target = remoteLeftHand.transform;
                                Vrik.solver.rightArm.target = remoteRightHand.transform;
                            }
                            else
                            {
                                //TODO
                            }

                        }
                    }
                }
                else
                {
                    Debug.LogError($"There is no component {typeof(NetworkManager)} in the scene.\n");
                }
            }
        }
        
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
        
    }
}