using RootMotion;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SEE.Controls.ReversibleActions

    //summary: marking a node by showing a sphere above the selected node
{
    internal class MarkAction : AbstractPlayerAction
    {
         //ruft Menüeintrag auf 
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MarkNode;
        }

        public override HashSet<string> GetChangedObjects()
        {
            throw new NotImplementedException();
        }

        //neues Werkzeug am Ende, wenn Aktion ausgeführt wurde
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        public override bool Update()
        {
            if (Input.GetMouseButtonDown(0)) 
            {
                Debug.Log("Auf Mark Node gedrückt");
            }
            return false;
        }

        //neues Objekt erstellen 
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }
    }
}
