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
        internal Cursor3D E { get; private set; }
        private SEECity city;

        private void Start()
        {
            if (TryGetComponent(out SEECity city))
            {
                this.city = city;
                E = Cursor3D.Create();

                InteractableObject.AnySelectIn += AnySelectIn;
                InteractableObject.AnySelectOut += AnySelectOut;
            }
            else
            {
                Debug.LogError($"{name} has no SEECity component attached to it. CityCursor will be disabled.\n");
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            InteractableObject.AnySelectIn -= AnySelectIn;
            InteractableObject.AnySelectOut -= AnySelectOut;

            Destroy(E);
        }

        private void AnySelectIn(InteractableObject interactableObject, bool isInitiator)
        {
            Graph selectedGraph = interactableObject.GraphElemRef.elem.ItsGraph;
            if (selectedGraph.Equals(city.LoadedGraph))
            {
                E.AddFocus(interactableObject);
            }
        }

        private void AnySelectOut(InteractableObject interactableObject, bool isInitiator)
        {
            Graph selectedGraph = interactableObject.GraphElemRef.elem.ItsGraph;
            if (selectedGraph.Equals(city.LoadedGraph))
            {
                E.RemoveFocus(interactableObject);
            }
        }
    }
}
