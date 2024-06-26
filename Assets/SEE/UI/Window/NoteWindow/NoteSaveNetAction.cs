using SEE.DataModel.DG;
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
        public GraphElement graphElement;

        public bool isPublic;

        public string content;

        public NoteSaveNetAction(GraphElement graphElement, bool isPublic, string content)
        {
            this.graphElement = graphElement;
            this.isPublic = isPublic;
            this.content = content;
        }

        protected override void ExecuteOnClient()
        {
            Debug.Log("SaveNetBeforeIf");
            if (!IsRequester())
            {
                Debug.Log("SaveNetActionAfterIf");
                NoteManager.Instance.SaveNote(graphElement, isPublic, content);
            }
        }

        protected override void ExecuteOnServer()
        {
            //Intentionally left blank
        }
    }
}
