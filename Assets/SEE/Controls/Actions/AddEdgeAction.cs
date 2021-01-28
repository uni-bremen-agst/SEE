using UnityEngine;
using SEE.Game;
using SEE.GO;
using System;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create an edge between two selected nodes.
    /// </summary>
    public class AddEdgeAction : MonoBehaviour
    {
        const ActionState.Type ThisActionState = ActionState.Type.DrawEdge;

        /// <summary>
        /// The currently hovered object.
        /// </summary>
        private GameObject hoveredObject;

        /// <summary>
        /// The source for the edge to be drawn.
        /// </summary>
        private GameObject from;

        /// <summary>
        /// The target of the edge to be drawn.
        /// </summary>
        private GameObject to;

        private void Start()
        {
            ActionState.OnStateChanged += (ActionState.Type v) =>
            {
                if (v == ThisActionState)
                {
                    enabled = true;
                    InteractableObject.AnyHoverIn += AnyHoverIn;
                    InteractableObject.AnyHoverOut += AnyHoverOut;
                }
                else
                {
                    enabled = false;
                    InteractableObject.AnyHoverIn -= AnyHoverIn;
                    InteractableObject.AnyHoverOut -= AnyHoverOut;
                }
            };
            enabled = ActionState.Is(ThisActionState);
        }

        private void Update()
        {
            Assert.IsTrue(ActionState.Is(ThisActionState));

            // Assigning the game objects to be connected.
            // Checking whether the two game objects are not null and whether they are 
            // actually nodes.
            if (Input.GetMouseButtonDown(0) && hoveredObject != null)
            {
                Assert.IsTrue(hoveredObject.HasNodeRef());
                if (from == null)
                {
                    from = hoveredObject;
                }
                else if (to == null)
                {
                    to = hoveredObject;
                }
            }
            // Note: from == to may be possible.
            if (from != null && to != null)
            {
                Transform cityObject = SceneQueries.GetCodeCity(from.transform);
                if (cityObject != null)
                {
                    if (cityObject.TryGetComponent(out SEECity city))
                    {
                        try
                        {
                            city.Renderer.DrawEdge(from, to);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"The new edge from {from.name} to {to.name} could not be created: {e.Message}.\n");
                        }
                        from = null;
                        to = null;
                    }
                }
            }
            // Adding the key "F1" in order to delete the selected gameobjects.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                from = null;
                to = null;
            }
        }

        private void AnyHoverIn(InteractableObject interactableObject, bool isOwner)
        {
            Assert.IsNull(hoveredObject);
            hoveredObject = interactableObject.gameObject;
        }

        private void AnyHoverOut(InteractableObject interactableObject, bool isOwner)
        {
            Assert.IsTrue(hoveredObject == interactableObject.gameObject);
            hoveredObject = null;
        }
    }
}