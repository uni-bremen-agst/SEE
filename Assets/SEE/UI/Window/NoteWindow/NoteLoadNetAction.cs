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

        public NoteLoadNetAction(string title, bool isPublic)
        {
            this.title = title;
            this.isPublic = isPublic;
        }
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                NoteManager.LoadNote(title, isPublic);
            }
        }

        protected override void ExecuteOnServer()
        {
            //Intentionally left blank
        }
    }
}
