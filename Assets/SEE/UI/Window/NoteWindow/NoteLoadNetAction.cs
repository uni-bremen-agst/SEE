using SEE.Net.Actions;
using SEE.UI.Window.NoteWindow;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.UI.Window.NoteWindow
{
    public class NoteLoadNetAction : AbstractNetAction
    {
        public string title;

        public bool isPublic;

        public NoteWindow noteWindow;

        public NoteLoadNetAction(string title, bool isPublic, NoteWindow noteWindow)
        {
            this.title = title;
            this.isPublic = isPublic;
            this.noteWindow = noteWindow;
            Debug.Log("noteWindow: " + noteWindow);
        }
        protected override void ExecuteOnClient()
        {
            Debug.Log("NoteLoadBeforeIf");
            if (!IsRequester())
            {
                Debug.Log("NoteLoadAfterIf" );
                noteWindow.searchField.text = NoteManager.Instance.LoadNote(title, isPublic);
            }
        }

        protected override void ExecuteOnServer()
        {
            //Intentionally left blank
        }
    }
}
