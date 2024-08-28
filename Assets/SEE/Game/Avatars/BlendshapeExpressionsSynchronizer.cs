using System.Collections;
using SEE.Net.Actions;
using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Responsible for synchronizing the facial expressions
    /// of the <see cref="SkinnedMeshRenderer"/> on all clients.
    ///
    /// This component is intended to be attached to an avatar that has a
    /// <see cref="SkinnedMeshRenderer"/> as a component.
    /// </summary>
    internal class BlendshapeExpressionsSynchronizer : NetSynchronizer
    {

        /**
         * The gameobject containing SkinnedMeshRenderer component.
         */
        private Transform ccBaseBody;

        /**
         * SkinnedMeshRenderer to be synchronized.
         */
        private SkinnedMeshRenderer skinnedMeshRenderer;

        /// <summary>
        /// Waits until <see cref="SkinnedMeshRenderer"/> is available and
        /// initializes <see cref="skinnedMeshRenderer"/>.
        /// </summary>
        private IEnumerator WaitForAvatar()
        {
            // Waits for UMAExpressionPlayer to be created
            yield return new WaitUntil(() => gameObject.transform.Find("CC_Base_Body") != null);
            ccBaseBody = gameObject.transform.Find("CC_Base_Body");
            skinnedMeshRenderer = ccBaseBody.GetComponent<SkinnedMeshRenderer>();
        }

        /// <summary>
        /// Initializes the network object and starts a coroutine that
        /// waits for components to be generated at runtime.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            StartCoroutine(WaitForAvatar());
        }

        /// <summary>
        /// This method is invoked periodically while this component is active.
        /// </summary>
        protected override void Synchronize()
        {
            if (skinnedMeshRenderer != null)
            {
                new BlendshapeExpressionsNetAction(skinnedMeshRenderer, NetworkObject.NetworkObjectId).Execute();
            }
        }
    }
}