using System.Collections.Generic;
﻿using SEE.Game;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create an edge between two selected nodes.
    /// </summary>
    internal class AddEdgeAction : AbstractPlayerAction
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
            public GameObject from;
            /// <summary>
            /// The unique ID of the source of the edge. It may be needed
            /// in situations in which <see cref="from"/> was destroyed.
            /// </summary>
            public readonly string fromID;
            /// <summary>
            /// The target of the edge.
            /// </summary>
            public GameObject to;
            /// <summary>
            /// The unique ID of the target of the edge. It may be needed
            /// in situations in which <see cref="to"/> was destroyed.
            /// </summary>
            public readonly string toID;
            /// <summary>
            /// The type of the edge.
            /// </summary>
            public string edgeType;
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="from">the source of the edge</param>
            /// <param name="to">the target of the edge</param>
            /// <param name="edgeType">the edge type</param>
            public Memento(GameObject from, GameObject to, string edgeType)
            {
                this.from = from;
                this.fromID = from.name;
                this.to = to;
                this.toID = to.name;
                this.edgeType = edgeType;
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
        /// The default type of an added edge.
        /// </summary>
        private const string DefaultEdgeType = "Source_Dependency";

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
                // We have both source and target of the edge.
                // FIXME: In the future, we need to query the edge type from the user.
                memento = new Memento(from, to, DefaultEdgeType);
                createdEdge = CreateEdge(memento);

                if (createdEdge != null)
                {
                    // action is completed (successfully or not; it does not matter)
                    from = null;
                    to = null;
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }
            }
            // Forget from and to upon user request.
            if (SEEInput.Unselect())
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
            base.Undo();
            GameEdgeAdder.Remove(createdEdge);
            new DeleteNetAction(createdEdge.name).Execute();
            Destroyer.DestroyGameObject(createdEdge);
            createdEdge = null;
        }

        /// <summary>
        /// Redoes this AddEdgeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            createdEdge = CreateEdge(memento);
        }

        /// <summary>
        /// Creates a new edge using the given <paramref name="memento"/>.
        /// In case of any error, null will be returned.
        /// </summary>
        /// <param name="memento">information needed to create the edge</param>
        /// <returns>a new edge or null</returns>
        private static GameObject CreateEdge(Memento memento)
        {
            // If we arrive here because Redo() call this method, it could happen
            // that the source or target in edgeMemento were replaced because their
            // addition was undone and then redone in which case new game objects
            // were created. If that happened, the source and/or target in the
            // memento are already destroyed. We need to retrieve the new
            // game objects for these from the scene using their IDs.
            if (memento.from == null)
            {
                memento.from = GameObject.Find(memento.fromID);
            }
            if (memento.to == null)
            {
                memento.to = GameObject.Find(memento.toID);
            }
            try
            {
                /// If <see cref="CreateEdge(Memento)"/> was called from Update
                /// when the edge is created for the first time, <see cref="memento.edgeID"/>
                /// will not be set. Then the creation process triggered by <see cref="GameEdgeAdder"/>
                /// will create a new unique id for the edge. If <see cref="CreateEdge(Memento)"/>
                /// is called from <see cref="Redo"/>, <see cref="memento.edgeID"/> has
                /// a valid edge id (set by the previous call to <see cref="CreateEdge(Memento)"/>.
                GameObject result = GameEdgeAdder.Add(memento.from, memento.to, memento.edgeType);
                UnityEngine.Assertions.Assert.IsNotNull(result);
                new AddEdgeNetAction(memento.from.name, memento.to.name, memento.edgeType).Execute();
                return result;
            }
            catch (Exception e)
            {
                ShowNotification.Error("New edge", $"An edge could not be created: {e.Message}.");
                return null;
            }
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewEdge"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.NewEdge;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                memento.from.name,
                memento.to.name,
                createdEdge.name
            };
        }
    }
}
