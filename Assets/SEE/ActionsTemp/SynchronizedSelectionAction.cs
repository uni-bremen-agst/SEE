using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls
{

    // TODO: SelectionAction and SelectAction should be combined somehow
    public class SynchronizedSelectionAction : AbstractAction
    {
        public uint id;



        public SynchronizedSelectionAction(HoverableObject hoverableObject) : base(true)
        {
            id = hoverableObject.id;
        }



        protected override bool ExecuteOnClient()
        {
            GameObject hitObject = null;
            foreach (HoverableObject hoverableObject in Object.FindObjectsOfType<HoverableObject>())
            {
                if (hoverableObject.id == id)
                {
                    hitObject = hoverableObject.gameObject;
                }
            }
            Assert.IsNotNull(hitObject);

            Actor actor = Object.FindObjectOfType<Actor>();
            Assert.IsNotNull(actor);

            SelectionAction selectAction = actor.selectionAction;
            selectAction.Select(hitObject);

            return true;
        }

        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool RedoOnClient()
        {
            return false;
        }

        protected override bool RedoOnServer()
        {
            return false;
        }

        protected override bool UndoOnClient()
        {
            return false;
        }

        protected override bool UndoOnServer()
        {
            return false;
        }
    }

}
