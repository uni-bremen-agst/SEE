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
        private SEECity city;

        private void Start()
        {
            city = GetComponent<SEECity>();
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
            Graph selectedGraph = interactableObject.GraphElemRef.elem.ItsGraph;
            if (selectedGraph.Equals(city.LoadedGraph))
            {
                E.AddFocus(interactableObject);
            }
        }

        private void AnySelectOut(InteractableObject interactableObject, bool isOwner)
        {
            Graph selectedGraph = interactableObject.GraphElemRef.elem.ItsGraph;
            if (selectedGraph.Equals(city.LoadedGraph))
            {
                E.RemoveFocus(interactableObject);
            }
        }
    }
}
