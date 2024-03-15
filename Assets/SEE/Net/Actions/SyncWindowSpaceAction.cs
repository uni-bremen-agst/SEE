using System;
using SEE.Controls;
using SEE.UI.Window;
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
        private WindowSpace.WindowSpaceValues space;

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

            this.space = space.ToValueObject();

            if (execute)
            {
                Execute();
            }
        }

        public override void ExecuteOnServer()
        {
            // Nothing needs to be done on the server
        }

        public override void ExecuteOnClient()
        {
            // If no space manager exists, there is nothing we can (or should) do.
            if (WindowSpaceManager.ManagerInstance)
            {
                WindowSpaceManager.ManagerInstance.UpdateSpaceFromValueObject(Requester.ToString(), space);
            }
        }
    }
}
