using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows connected edges when the user hovers over or selects a node.
    /// </summary>
    public class ShowEdges : InteractableObjectAction
    {
        // TODO: Perhaps edges being hidden should be a city setting? The setting would simply need to set the 
        //       `Edge.IsHiddenToggle` on all edges the user wishes to hide. Such edges will then only be shown
        //        when hovering or selecting connected nodes. This is currently the case for all reflexion edges
        //        except absences and divergences.

        /// <summary>
        /// True if the object is currently being hovered over.
        /// </summary>
        private bool isHovered = false;

        /// <summary>
        /// True if the object is currently selected.
        /// </summary>
        private bool isSelected = false;

        private const float EDGE_FADE_DURATION = 1f;

        /// <summary>
        /// Registers On() and Off() for the respective hovering and selection events.
        /// </summary>
        protected void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn += SelectionOn;
                Interactable.SelectOut += SelectionOff;
                Interactable.HoverIn += HoverOn;
                Interactable.HoverOut += HoverOff;
            }
            else
            {
                Debug.LogError($"ShowEdges.OnEnable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective hovering and selection events.
        /// </summary>
        protected void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn -= SelectionOn;
                Interactable.SelectOut -= SelectionOff;
                Interactable.HoverIn -= HoverOn;
                Interactable.HoverOut -= HoverOff;
            }
            else
            {
                Debug.LogError($"ShowEdges.OnDisable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Called when the object is selected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the connected edges are shown.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void SelectionOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = true;
                // if the object is currently hovered over, the edges are already shown
                if (!isHovered)
                {
                    On();
                }
            }
        }

        /// <summary>
        /// Called when the object is deselected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the shown edges are hidden unless the object is still hovered.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void SelectionOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = false;
                if (!isHovered)
                {
                    Off();
                }
            }
        }

        /// <summary>
        /// Called when the object is being hovered over. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the connected edges are shown.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void HoverOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isHovered = true;
                // if the object is currently selected, the edges are already shown
                if (!isSelected)
                {
                    On();
                }
            }
        }

        /// <summary>
        /// Called when the object is no longer hovered over. If <paramref name="isInitiator"/>
        /// is false, a remote player has triggered this event and, hence, nothing will be done.
        /// Otherwise the connected edges are hidden unless the object is still selected.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void HoverOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isHovered = false;
                if (!isSelected)
                {
                    Off();
                }
            }
        }

        /// <summary>
        /// Returns the code city holding the settings for the visualization of the node.
        /// May be null.
        /// </summary>
        private AbstractSEECity City()
        {
            GameObject codeCityObject = SceneQueries.GetCodeCity(gameObject.transform)?.gameObject;
            if (codeCityObject == null)
            {
                return null;
            }

            codeCityObject.TryGetComponent(out AbstractSEECity city);
            return city;
        }

        /// <summary>
        /// Returns the node this class is attached to.
        /// May be null.
        /// </summary>
        private Node Node()
        {
            if (!gameObject.TryGetComponent(out NodeRef nodeRef) || nodeRef.Value == null)
            {
                return null;
            }

            return nodeRef.Value;
        }

        /// <summary>
        /// Hides the given <paramref name="edge"/> by fading its alpha value to zero.
        /// </summary>
        /// <param name="edge">the edge to hide</param>
        private static void HideEdge(Edge edge)
        {
            GameObject edgeObject = GraphElementIDMap.Find(edge.ID);
            if (edgeObject == null)
            {
                Debug.LogError($"Could not find edge {edge.ToShortString()}!");
                return;
            }

            EdgeOperator @operator = edgeObject.AddOrGetComponent<EdgeOperator>();
            @operator.FadeTo(0f, EDGE_FADE_DURATION);
        }

        /// <summary>
        /// Shows the given <paramref name="edge"/> by fading its alpha value to one.
        /// </summary>
        /// <param name="edge">the edge to show</param>
        private static void ShowEdge(Edge edge)
        {
            GameObject edgeObject = GraphElementIDMap.Find(edge.ID);
            if (edgeObject == null)
            {
                Debug.LogError($"Could not find edge {edge.ToShortString()}!");
                return;
            }

            EdgeOperator @operator = edgeObject.AddOrGetComponent<EdgeOperator>();
            @operator.FadeTo(1f, EDGE_FADE_DURATION);
        }

        private void On()
        {
            Node node = Node();
            if (node == null)
            {
                return;
            }

            // TODO: Perhaps the node along with its edges should be cached?
            foreach (Edge edge in node.Incomings.Concat(node.Outgoings).Where(x => x.HasToggle(Edge.IsHiddenToggle)))
            {
                ShowEdge(edge);
            }
        }

        private void Off()
        {
            Node node = Node();
            if (node == null)
            {
                return;
            }

            foreach (Edge edge in node.Incomings.Concat(node.Outgoings).Where(x => x.HasToggle(Edge.IsHiddenToggle)))
            {
                HideEdge(edge);
            }
        }
    }
}