using UMA.PoseTools;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
{
    
    
    
    
    public class ExpressionPlayerNetAction : AbstractNetAction
    {



        public ulong NetworkObjectID;

        public ExpressionPlayer ExpressionPlayer;



        public ExpressionPlayerNetAction(ulong networkObjectID, ExpressionPlayer expressionPlayer)
        {
            NetworkObjectID = networkObjectID;
            ExpressionPlayer = expressionPlayer;
        }
        
        
        protected override void ExecuteOnClient()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager != null)
            {
                NetworkSpawnManager networkSpawnManager = networkManager.SpawnManager;
                if (networkSpawnManager.SpawnedObjects.TryGetValue(NetworkObjectID, out NetworkObject networkObject))
                {
                    if (networkObject.gameObject.TryGetComponent(out ExpressionPlayer expressionPlayer))
                    {
                        expressionPlayer = ExpressionPlayer;
                    }
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
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
        
    }
}