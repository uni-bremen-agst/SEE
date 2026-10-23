using UnityEngine;
using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    public class MarkNetAction : NodeNetAction
    {
        public bool Marked;

        public MarkNetAction (string newNodeID, bool marked) : base(newNodeID)
        {
            Marked = marked;
        }

        public override void ExecuteOnClient()
        {
            GameNodeMarker.SetMarked(Find(GraphElementID), Marked);
        }

    }
}