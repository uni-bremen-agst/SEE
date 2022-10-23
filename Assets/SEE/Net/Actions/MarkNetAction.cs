using SEE.Game;
using UnityEngine;

namespace SEE.Net.Actions
{
    public class MarkNetAction : AbstractNetAction
    {
        
        public GameObject Node { get; }
        public bool Added { get; }

        public MarkNetAction(GameObject node, bool added)
        {
            Node = node;
            Added = added;
        }

        protected override void ExecuteOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override void ExecuteOnClient()
        {
            if (Added)
            {
                GameNodeMarker.CreateMarker(Node);
            }
            throw new System.NotImplementedException();
        }
    }
}