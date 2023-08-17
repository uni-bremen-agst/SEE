using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.UI3D;
using SEE.Utils;
using UnityEngine;

namespace SEE.GO
{
    public class CityCursor : MonoBehaviour
    {
        internal Cursor3D E { get; private set; }
        private AbstractSEECity city;

        private void Start()
        {
            if (TryGetComponent(out AbstractSEECity city))
            {
                this.city = city;
#if UNITY_EDITOR
                E = Cursor3D.Create(city.name);
#else
                E = Cursor3D.Create();
#endif

                InteractableObject.AnySelectIn += AnySelectIn;
                InteractableObject.AnySelectOut += AnySelectOut;
            }
            else
            {
                Debug.LogWarning($"{name} has no {nameof(AbstractSEECity)} component attached to it. {nameof(CityCursor)} will be disabled.\n");
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            InteractableObject.AnySelectIn -= AnySelectIn;
            InteractableObject.AnySelectOut -= AnySelectOut;

            Destroyer.Destroy(E);
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
