using System;
using SEE.GO;
using UMA.PoseTools;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace SEE.Net.Actions
{
    public class ExpressionPlayerSynchronizer : MonoBehaviour
    {
        
        private UMAExpressionPlayer ExpressionPlayer;
        
        private const float RepeatCycle = 0.1f;
        
        private NetworkObject NetworkObject;

        
        IEnumerator WaitForExpressionPlayer()
        {
            Debug.Log("Waiting for ExpressionPlayer.");
            yield return new WaitUntil(() => gameObject.GetComponent<ExpressionPlayer>() != null);
            Debug.Log("ExpressionPlayer found.");
            ExpressionPlayer = gameObject.GetComponent<UMAExpressionPlayer>();
            NetworkObject = gameObject.GetComponent<NetworkObject>();
            InvokeRepeating(nameof(Synchronize), RepeatCycle, RepeatCycle);
        }

        private void Start()
        {
            StartCoroutine(WaitForExpressionPlayer());
        }
        
        private void Synchronize()
        {
            new ExpressionPlayerNetAction(ExpressionPlayer, NetworkObject.NetworkObjectId).Execute();
        }
    }
}