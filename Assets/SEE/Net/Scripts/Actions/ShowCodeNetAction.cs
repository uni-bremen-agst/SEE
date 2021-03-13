using SEE.Game;
using SEE.GO;
using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows the Code Window on each client.
    /// </summary>
    public class ShowCodeNetAction : Net.AbstractAction
    {
        
        public ShowCodeNetAction()
        {
        }

        /// <summary>
        /// Stuff to execute on the Server. Nothing to be done here.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }


        protected override void ExecuteOnClient()
        {
          
        }
    }
}