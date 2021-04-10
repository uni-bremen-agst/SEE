using System;
using Assets.SEE.Game;
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
        private struct Memento
        {
            /// <summary>
            /// The source of the edge.
            /// </summary>
            public readonly GameObject from;
            /// <summary>
            /// The target of the edge.
            /// </summary>
            public readonly GameObject to;
            /// <summary>
            /// The unique ID of the edge.
            /// </summary>
            public string edgeID;
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="from">the source of the edge</param>
            /// <param name="to">the target of the edge</param>
            public Memento(GameObject from, GameObject to)
            {
                this.from = from;
                this.to = to;
                this.edgeID = null;
            }
        }

        /// <summary>
        /// The information needed to re-create the edge.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The edge created by this action. Can be null if no edge has been created yet
        /// or whether an Undo was called. The created edge is stored only to delete
        /// it again if Undo is called. All information to create the edge is kept in
        /// <see cref="memento"/>.
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
            // FIXME: We need an interaction for VR, too.
            if (hoveredObject != null && Input.GetMouseButtonDown(0) && !Raycasting.IsMouseOverGUI() && hoveredObject.HasNodeRef())
            {
                if (from == null)
                {
                    // No source selected yet; this interaction is meant to set the source.
                    from = hoveredObject;
                }
                else if (to == null)
                {
                    // Source is already set; this interaction is meant to set the target.
                    to = hoveredObject;
                }
            }
            // Note: from == to may be possible.
            if (from != null && to != null)
            {
                // We do not have an edge ID yet, so we let the graph renderer create a unique ID.
                memento = new Memento(from, to);
                createdEdge = CreateEdge(memento);
                if (createdEdge != null)
                {
                    // The edge ID was created by the graph renderer.
                    memento.edgeID = createdEdge.ID();
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
            GameEdgeAdder.Remove(createdEdge);
            new DeleteNetAction(createdEdge.name).Execute();
            createdEdge = null;
        }

        /// <summary>
        /// Redoes this AddEdgeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            createdEdge = CreateEdge(memento);
        }

        /// <summary>
        /// Creates a new edge using the given <paramref name="edgeMemento"/>.
        /// In case of any error, null will be returned.
        /// </summary>
        /// <param name="edgeMemento">information needed to create the edge</param>
        /// <returns>a new edge or null</returns>
        private static GameObject CreateEdge(Memento edgeMemento)
        {
            GameObject result = GameEdgeAdder.Add(edgeMemento.from, edgeMemento.to, edgeMemento.edgeID);
            // Note that we need to use result.name as edge ID because edgeMemento.edgeID could be null.
            new AddEdgeNetAction(edgeMemento.from.name, edgeMemento.to.name, result.name).Execute();
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
    }
}
