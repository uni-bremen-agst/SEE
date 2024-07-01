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
        /// The ID of the graph element in which to add the note.
        /// </summary>
        public string graphElementID;

        /// <summary>
        /// Indicates whether the note is public or private.
        /// </summary>
        public bool isPublic;

        /// <summary>
        /// The content to save from the note.
        /// </summary>
        public string content;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graphElementID">The ID of the graph element in which to add the note.</param>
        /// <param name="isPublic">Indicates whether the note is public or private.</param>
        /// <param name="content">The content of the note to be saved.</param>
        public NoteSaveNetAction(string graphElementID, bool isPublic, string content)
        {
            this.graphElementID = graphElementID;
            this.isPublic = isPublic;
            this.content = content;
        }

        /// <summary>
        /// Creates a new Note on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                NoteManager.Instance.SaveNote(graphElementID, isPublic, content);
            }
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            //Intentionally left blank
        }
    }
}
