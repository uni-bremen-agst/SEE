using System;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create an edge between two selected nodes.
    /// </summary>
    public class AddEdgeAction : AbstractPlayerAction
    {

        /// <summary>
        /// The source for the edge to be drawn.
        /// </summary>
        private GameObject from;

        /// <summary>
        /// The target of the edge to be drawn.
        /// </summary>
        private GameObject to;

        public override void Start()
        {

            InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
        }

        public override void Update()
        {
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
                            new AddEdgeNetAction(from.name, to.name).Execute();
                            CurrentState = CurrentActionState.Executed;
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
            // Adding the key "F1" in order to delete the selected GameObjects.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                from = null;
                to = null;
            }
        }

        /// <summary>
        /// Undoes this AddEdgeActíon
        /// </summary>
        public override void Undo()
        {
            Debug.Log("Undo AddEdge");
        }

        /// <summary>
        /// Redoes this DeleteAction
        /// </summary>
        public override void Redo()
        {
            Debug.Log("Redo AddEdge");
        }

        public static ReversibleAction CreateReversibleAction()
        {
            return new AddEdgeAction();
        }
    }
}