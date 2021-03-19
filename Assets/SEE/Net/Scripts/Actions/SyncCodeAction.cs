using System.Collections.Generic;
using SEE.Game.UI.CodeWindow;

namespace SEE.Net
{
    /// <summary>
    /// Synchronizes the code spaces across all clients.
    /// </summary>
    public class SyncCodeSpaceAction: AbstractAction
    {

        public Dictionary<string, CodeWindowSpace.CodeWindowSpaceValues> Spaces;
        
        protected override void ExecuteOnServer()
        {
            // Nothing needs to be done on the server
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                
            }
        }
    }
}