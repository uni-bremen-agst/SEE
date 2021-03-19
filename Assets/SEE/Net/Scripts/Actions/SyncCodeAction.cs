using System.Collections.Generic;
using SEE.Controls;
using SEE.Game.UI.CodeWindow;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Synchronizes the code spaces across all clients.
    /// </summary>
    public class SyncCodeSpaceAction: AbstractAction
    {

        private const bool SYNC_FULL_TEXT = false;

        public Dictionary<string, CodeWindowSpace.CodeWindowSpaceValues> Spaces = new Dictionary<string, CodeWindowSpace.CodeWindowSpaceValues>();

        public SyncCodeSpaceAction(CodeWindowSpace space)
        {
            UpdateSpace(space, false);
        }

        public void UpdateSpace(CodeWindowSpace space, bool execute = true)
        {
            Debug.Log("Synchronizing code now...");
            if (space == null)
            {
                return;
            }
            
            Spaces[CodeSpaceManager.LOCAL_PLAYER] = space.ToValueObject(SYNC_FULL_TEXT);

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
                //TODO
            }
        }
    }
}