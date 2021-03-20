using SEE.Controls;
using SEE.Game.UI.CodeWindow;

namespace SEE.Net
{
    /// <summary>
    /// Synchronizes the code spaces across all clients.
    /// </summary>
    public class SyncCodeSpaceAction: AbstractAction
    {

        private const bool SYNC_FULL_TEXT = false;

        public CodeWindowSpace.CodeWindowSpaceValues Space;

        public SyncCodeSpaceAction(CodeWindowSpace space)
        {
            UpdateSpace(space, false);
        }

        public void UpdateSpace(CodeWindowSpace space, bool execute = true)
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
                
                CodeSpaceManager.ManagerInstance.UpdateCodeWindowSpaceFromValueObject(RequesterIPAddress, Space);
            }
        }
    }
}