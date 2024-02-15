using System.Collections;
using SEE.Net.Actions;
using UMA.PoseTools;
using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Responsible for synchronizing the facial expressions
    /// of the <see cref="UMAExpressionPlayer"/> on all clients.
    ///
    /// This component is intended to be attached to a gameobject that has a
    /// <see cref="UMAExpressionPlayer"/> as a component.
    /// </summary>
    internal class ExpressionPlayerSynchronizer : NetSynchronizer
    {
        /// <summary>
        /// Parts of this <see cref="UMAExpressionPlayer"/> should be synchronized
        /// on all clients.
        /// </summary>
        private UMAExpressionPlayer expressionPlayer;

        /// <summary>
        /// Waits until <see cref="UMAExpressionPlayer"/> is available and
        /// initializes <see cref="expressionPlayer"/>.
        /// </summary>
        private IEnumerator WaitForExpressionPlayer()
        {
            // Waits for UMAExpressionPlayer to be created
            yield return new WaitUntil(() => gameObject.GetComponent<UMAExpressionPlayer>() != null);
            expressionPlayer = gameObject.GetComponent<UMAExpressionPlayer>();
        }

        /// <summary>
        /// Initializes the network object and starts a coroutine that
        /// waits for components to be generated at runtime.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            StartCoroutine(WaitForExpressionPlayer());
        }

        /// <summary>
        /// This method is invoked periodically while this component is active.
        /// </summary>
        protected override void Synchronize()
        {
            if (expressionPlayer != null)
            {
                new ExpressionPlayerNetAction(expressionPlayer, NetworkObject.NetworkObjectId).Execute();
            }
        }
    }
}