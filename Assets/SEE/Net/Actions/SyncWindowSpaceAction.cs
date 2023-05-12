using System;
using SEE.Controls;
using SEE.Game.UI.Window;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Synchronizes the window spaces across all clients.
    /// </summary>
    [Serializable]
    public class SyncWindowSpaceAction: AbstractNetAction
    {
        /// <summary>
        /// The value object of the window space which shall be transmitted over the network.
        /// </summary>
        [field: SerializeField]
        private WindowSpace.WindowSpaceValues Space;

        /// <summary>
        /// For the given <paramref name="space"/>, create a value object, and depending on <paramref name="execute"/>,
        /// send it over the network as well.
        /// </summary>
        /// <param name="space">The space which shall be serialized (and sent across the network).</param>
        /// <param name="execute">Whether the <paramref name="space"/> should be sent across the network.</param>
        public void UpdateSpace(WindowSpace space, bool execute = true)
        {
            if (space == null)
            {
                return;
            }

            Space = space.ToValueObject();

            if (execute)
            {
                Execute();
            }
        }

        protected override void ExecuteOnServer()
        {
            // Nothing needs to be done on the server
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (!WindowSpaceManager.ManagerInstance)
                {
                    // If no space manager exists, there is nothing we can (or should) do.
                    return;
                }

                WindowSpaceManager.ManagerInstance.UpdateSpaceFromValueObject(RequesterIPAddress, Space);
            }
        }
    }
}
