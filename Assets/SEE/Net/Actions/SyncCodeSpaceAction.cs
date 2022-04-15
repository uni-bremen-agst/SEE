using SEE.Controls;
using SEE.Game.UI.CodeWindow;

namespace SEE.Net
{
    /// <summary>
    /// Synchronizes the code spaces across all clients.
    /// </summary>
    public class SyncCodeSpaceAction: AbstractNetAction
    {
        /// <summary>
        /// Whether the full text of the code windows should be transmitted instead of just the filename.
        /// </summary>
        private const bool SYNC_FULL_TEXT = false;

        /// <summary>
        /// The value object of the code space which shall be transmitted over the network.
        /// </summary>
        public CodeSpace.CodeSpaceValues Space;

        /// <summary>
        /// For the given <paramref name="space"/>, create a value object, and depending on <paramref name="execute"/>,
        /// send it over the network as well.
        /// </summary>
        /// <param name="space">The space which shall be serialized (and sent across the network).</param>
        /// <param name="execute">Whether the <paramref name="space"/> should be sent across the network.</param>
        public void UpdateSpace(CodeSpace space, bool execute = true)
        {
            if (space == null)
            {
                return;
            }

            Space = space.ToValueObject(SYNC_FULL_TEXT);

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
                if (!CodeSpaceManager.ManagerInstance)
                {
                    // If no code space manager exists, there is nothing we can (or should) do.
                    return;
                }

                CodeSpaceManager.ManagerInstance.UpdateCodeSpaceFromValueObject(RequesterIPAddress, Space);
            }
        }
    }
}
