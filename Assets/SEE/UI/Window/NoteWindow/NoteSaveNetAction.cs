using SEE.Net.Actions;
using SEE.UI.Window.NoteWindow;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window.NoteWindow
{
    public class NoteSaveNetAction : AbstractNetAction
    {
        public string title;

        public bool isPublic;

        public string content;

        public NoteSaveNetAction(string title, bool isPublic, string content)
        {
            this.title = title;
            this.isPublic = isPublic;
            this.content = content;
        }

        protected override void ExecuteOnClient()
        {
            Debug.Log("SaveNetBeforeIf");
            if (!IsRequester())
            {
                Debug.Log("SaveNetActionAfterIf");
                NoteManager.Instance.SaveNote(title, isPublic, content);
            }
        }

        protected override void ExecuteOnServer()
        {
            //Intentionally left blank
        }
    }
}
