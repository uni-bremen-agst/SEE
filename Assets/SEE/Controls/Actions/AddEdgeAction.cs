using System;
using System.Collections.Generic;
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
        /// The source for an edge to be drawn.
        /// </summary>
        private GameObject from;

        /// <summary>
        /// The target of the edge to be drawn.
        /// </summary>
        private GameObject to;

        /// <summary>
        /// The Objects which are needed to create a new edge:
        /// The source, the target and the city where the edge will be attached to.
        /// </summary>
        private List<Tuple<GameObject, GameObject, SEECity>> edgesToBeDrawn = new List<Tuple<GameObject, GameObject, SEECity>>();

        /// <summary>
        /// All createdEdges by this action.
        /// </summary>
        private List<GameObject> createdEdges = new List<GameObject>();

        /// <summary>
        /// The names of the generated edges.
        /// </summary>
        private List<string> edgeNames = new List<string>();

        public override void Start()
        {
            InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
        }

        /// <summary>
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

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
                            GameObject addedEdge = city.Renderer.DrawEdge(from, to, null);
                            edgesToBeDrawn.Add(new Tuple<GameObject, GameObject, SEECity>(from, to, city));
                            createdEdges.Add(addedEdge);
                            new AddEdgeNetAction(from.name, to.name).Execute();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"The new edge from {from.name} to {to.name} could not be created: {e.Message}.\n");
                        }
                        from = null;
                        to = null;
                        // action is completed (successfully or not; it does not matter)
                        result = true;
                    }
                }
            }
            // Adding the key "F1" in order to forget the selected GameObjects.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                from = null;
                to = null;
            }
            return result;
        }

        /// <summary>
        /// Undoes this AddEdgeActíon
        /// </summary>
        public override void Undo()
        {
            DeleteAction deleteAction = new DeleteAction();
            foreach (GameObject edge in createdEdges)
            {
                deleteAction.DeleteSelectedObject(edge);
                edgeNames.Add(edge.name);
                Destroyer.DestroyGameObject(edge);
            }
        }

        /// <summary>
        /// Redoes this AddEdgeAction
        /// </summary>
        public override void Redo()
        {
            createdEdges.Clear();
            for(int i = 0; i < edgesToBeDrawn.Count; i++)
            {
                Tuple<GameObject, GameObject, SEECity> edgeToBeDrawn = edgesToBeDrawn[i];
                GameObject redoneEdge = edgeToBeDrawn.Item3.Renderer.DrawEdge(edgeToBeDrawn.Item1, edgeToBeDrawn.Item2,edgeNames[i]);
                createdEdges.Add(redoneEdge);
            }
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddEdgeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new AddEdgeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddEdgeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
    }
}