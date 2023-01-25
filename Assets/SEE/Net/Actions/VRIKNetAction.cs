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
        
        public VRIKNetAction(ulong networkObjectID)
        {
            NetworkObjectID = networkObjectID;
        }
        
        /// <summary>
        /// If executed by the initiating client, nothing happens.
        /// </summary>
        protected override void ExecuteOnClient()
        {
         
        }
        
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
        
    }
}