using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.UI3D;
using UnityEngine;

namespace SEE.GO
{
    // [RequireComponent(typeof(SEECity))] // FIXME: We cannot simply request that a SEECity exists. There are also other kinds of AbstractSEECity classes.
    public class CityCursor : MonoBehaviour
    {
        private Graph graph;
        internal Cursor3D E { get; private set; }

        private void Start()
        {
            graph = GetComponent<SEECity>().LoadedGraph;
            E = Cursor3D.Create();

            InteractableObject.AnySelectIn += AnySelectIn;
            InteractableObject.AnySelectOut += AnySelectOut;
        }

        private void OnDestroy()
        {
            InteractableObject.AnySelectIn -= AnySelectIn;
            InteractableObject.AnySelectOut -= AnySelectOut;

            Destroy(E);
        }

        private void AnySelectIn(InteractableObject interactableObject, bool isOwner)
        {
            if (interactableObject.GraphElemRef.elem.ItsGraph.Equals(graph))
            {
                E.AddFocus(interactableObject.transform);
            }
        }

        private void AnySelectOut(InteractableObject interactableObject, bool isOwner)
        {
            if (interactableObject.GraphElemRef.elem.ItsGraph.Equals(graph))
            {
                E.RemoveFocus(interactableObject.transform);
            }
        }
    }
}
