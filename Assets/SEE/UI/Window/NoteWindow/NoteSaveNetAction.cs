using SEE.DataModel.DG;
using SEE.GO;
using SEE.Net.Actions;
using SEE.UI.Window.NoteWindow;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window.NoteWindow
{
    /// <summary>
    /// Saves the note through the network on each client.
    /// </summary>
    public class NoteSaveNetAction : AbstractNetAction
    {
        /// <summary>
        /// The GraphElementRef 
        /// </summary>
        public GraphElementRef graphElementRef;

        public bool isPublic;

        /// <summary>
        /// The content to save from the note.
        /// </summary>
        public string content;

        public NoteSaveNetAction(GraphElementRef graphElementRef, bool isPublic, string content)
        {
            this.graphElementRef = graphElementRef;
            this.isPublic = isPublic;
            this.content = content;
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                NoteManager.Instance.SaveNote(graphElementRef, isPublic, content);
            }
        }

        protected override void ExecuteOnServer()
        {
            //Intentionally left blank
        }
    }
}
