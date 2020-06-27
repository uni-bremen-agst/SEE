using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net
{

    public class NavigationAction : AbstractAction
    {
        public NavigationAction() : base(false)
        {
        }

        protected override bool ExecuteOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool ExecuteOnClient()
        {
            throw new System.NotImplementedException();
        }

        protected override bool UndoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool UndoOnClient()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnClient()
        {
            throw new System.NotImplementedException();
        }
    }

}
