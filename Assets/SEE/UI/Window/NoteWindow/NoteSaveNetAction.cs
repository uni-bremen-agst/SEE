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
    public class NoteSaveNetAction : AbstractNetAction
    {
        public GraphElementRef graphElementRef;

        public bool isPublic;

        public string content;

        public NoteSaveNetAction(GraphElementRef graphElementRef, bool isPublic, string content)
        {
            this.graphElementRef = graphElementRef;
            this.isPublic = isPublic;
            this.content = content;
        }

        protected override void ExecuteOnClient()
        {
            Debug.Log("SaveNetBeforeIf");
            if (!IsRequester())
            {
                Debug.Log("SaveNetActionAfterIf");
                NoteManager.Instance.SaveNote(graphElementRef, isPublic, content);
            }
        }

        protected override void ExecuteOnServer()
        {
            //Intentionally left blank
        }
    }
}
