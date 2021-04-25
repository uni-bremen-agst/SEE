using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.UI3D;
using UnityEngine;

namespace SEE.GO
{
    [RequireComponent(typeof(SEECity))]
    public class CityCursor : MonoBehaviour
    {
        internal Cursor3D E { get; private set; }

        private void Start()
        {
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
            Graph thisGraph = GetComponent<SEECity>().LoadedGraph;
            Graph selectedGraph = interactableObject.GraphElemRef.elem.ItsGraph;
            if (selectedGraph.Equals(thisGraph))
            {
                E.AddFocus(interactableObject.transform);
            }
        }

        private void AnySelectOut(InteractableObject interactableObject, bool isOwner)
        {
            Graph thisGraph = GetComponent<SEECity>().LoadedGraph;
            Graph selectedGraph = interactableObject.GraphElemRef.elem.ItsGraph;
            if (selectedGraph.Equals(thisGraph))
            {
                E.RemoveFocus(interactableObject.transform);
            }
        }
    }
}
