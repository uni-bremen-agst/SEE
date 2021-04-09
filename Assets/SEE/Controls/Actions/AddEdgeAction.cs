using System;
using System.Collections.Generic;
using SEE.Game;
using SEE.GO;
using SEE.Net;
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
        /// Returns a new instance of <see cref="AddEdgeAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="AddEdgeAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new AddEdgeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddEdgeAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="AddEdgeAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// The source node for an edge to be drawn during the selection process.
        /// </summary>
        private GameObject from;

        /// <summary>
        /// The target node of the edge to be drawn during the selection process.
        /// </summary>
        private GameObject to;

        /// <summary>
        /// The information we need to (re-)create an edge.
        /// </summary>
        private struct EdgeMemento
        {
            public readonly GameObject from;
            public readonly GameObject to;
            public string edgeID;
            public EdgeMemento(GameObject from, GameObject to, string edgeID)
            {
                this.from = from;
                this.to = to;
                this.edgeID = edgeID;
            }
        }

        /// <summary>
        /// The information needed to re-create the edge.
        /// </summary>
        private EdgeMemento edgeMemento;

        /// <summary>
        /// The edge created by this action. Can be null if no edge has been created yet
        /// or whether an Undo was called. The created edge is stored only to delete
        /// it again if Undo is called. All information to create the edge is kept in
        /// <see cref="edgeMemento"/>.
        /// </summary>
        private GameObject createdEdge;

        /// <summary>
        /// Registers itself at <see cref="InteractableObject"/> to listen for hovering events.
        /// </summary>
        public override void Start()
        {
            InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
        }

        /// <summary>
        /// Unregisters itself from <see cref="InteractableObject"/>. Does no
        /// longer listen for hovering events.
        /// </summary>
        public override void Stop()
        {
            InteractableObject.LocalAnyHoverIn -= LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut -= LocalAnyHoverOut;
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
            if (hoveredObject != null && Input.GetMouseButtonDown(0) && !Raycasting.IsMouseOverGUI())
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
                // We do not have an edge ID yet, so we pass null to let the
                // graph renderer create a unique ID.
                edgeMemento = new EdgeMemento(from, to, null);
                createdEdge = CreateEdge(edgeMemento);
                if (createdEdge != null)
                {
                    edgeMemento.edgeID = createdEdge.ID();
                    from = null;
                    to = null;
                    // action is completed (successfully or not; it does not matter)
                    result = true;
                    hadAnEffect = true;
                }
            }
            // Adding the key to forget the selected GameObjects.
            if (Input.GetKeyDown(KeyBindings.Unselect))
            {
                from = null;
                to = null;
            }
            return result;
        }

        /// <summary>
        /// Undoes this AddEdgeAction
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            DeleteAction deleteAction = new DeleteAction();
            deleteAction.DeleteSelectedObject(createdEdge);
            Destroyer.DestroyGameObject(createdEdge);
            createdEdge = null;
        }

        /// <summary>
        /// Redoes this AddEdgeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            createdEdge = CreateEdge(edgeMemento);
        }

        /// <summary>
        /// Creates a new edge using the given <paramref name="edgeMemento"/>.
        /// In case of any error, null will be returned.
        /// </summary>
        /// <param name="edgeMemento">information needed to create the edge</param>
        /// <returns>a new edge or null</returns>
        private static GameObject CreateEdge(EdgeMemento edgeMemento)
        {
            GameObject result = null;
            Transform cityObject = SceneQueries.GetCodeCity(edgeMemento.from.transform);
            if (cityObject != null)
            {
                // FIXME: This will work only for SEECity but not other subclasses of AbstractSEECity.
                if (cityObject.TryGetComponent(out SEECity city))
                {
                    try
                    {
                        result = city.Renderer.DrawEdge(edgeMemento.from, edgeMemento.to, edgeMemento.edgeID);
                        // Note that we need to result.name as edge ID because edgeMemento.edgeID could be null.
                        new AddEdgeNetAction(edgeMemento.from.name, edgeMemento.to.name, result.name).Execute();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"The new edge from {edgeMemento.from.name} to {edgeMemento.to.name} could not be created: {e.Message}.\n");
                    }
                }
                else
                {
                    Debug.LogError($"The code city for the new edge from {edgeMemento.from.name} to {edgeMemento.to.name} has no .\n");
                }
            }
            else
            {
                Debug.LogError($"Could not determine the code city for the new edge from {edgeMemento.from.name} to {edgeMemento.to.name}.\n");
            }
            return result;
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewEdge"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.NewEdge;
        }

        public override List<string> GetChangedObjects()
        {
            List<string> changedObjects = new List<string>();

            changedObjects.Add(edgeMemento.from.name);
            changedObjects.Add(edgeMemento.to.name);
            changedObjects.Add(createdEdge.name);

            return changedObjects;
        }
    }
}
