using System.Collections.Generic;
﻿using SEE.Game;
using SEE.UI.Notification;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using SEE.Utils.History;
using System;
using UnityEngine;
using SEE.Audio;
using SEE.DataModel.DG;
using SEE.Game.SceneManipulation;
using SEE.XR;

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
        public static IReversibleAction CreateReversibleAction()
        {
            return new AddEdgeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddEdgeAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="AddEdgeAction"/></returns>
        public override IReversibleAction NewInstance()
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
            public GameObject From;
            /// <summary>
            /// The unique ID of the source of the edge. It may be needed
            /// in situations in which <see cref="From"/> was destroyed.
            /// </summary>
            public readonly string FromID;
            /// <summary>
            /// The target of the edge.
            /// </summary>
            public GameObject To;
            /// <summary>
            /// The unique ID of the target of the edge. It may be needed
            /// in situations in which <see cref="To"/> was destroyed.
            /// </summary>
            public readonly string ToID;
            /// <summary>
            /// The type of the edge.
            /// </summary>
            public string EdgeType;
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="from">the source of the edge</param>
            /// <param name="to">the target of the edge</param>
            /// <param name="edgeType">the edge type</param>
            public Memento(GameObject from, GameObject to, string edgeType)
            {
                this.From = from;
                this.FromID = from.name;
                this.To = to;
                this.ToID = to.name;
                this.EdgeType = edgeType;
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
        private const string defaultEdgeType = Edge.SourceDependency;

        /// <summary>
        /// <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;
            // Assigning the game objects to be connected.
            // Checking whether the two game objects are not null and whether they are
            // actually nodes.
            if (SceneSettings.InputType == PlayerInputType.VRPlayer)
            {
                if (XRSEEActions.Selected && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef())
                {
                    if (from == null)
                    {
                        from = InteractableObject.HoveredObjectWithWorldFlag.gameObject;
                        XRSEEActions.Selected = false;
                    }
                    else if (to == null)
                    {
                        to = InteractableObject.HoveredObjectWithWorldFlag.gameObject;
                        XRSEEActions.Selected = false;
                    }
                }
            }
            else
            {
                if (HoveredObject != null && Input.GetMouseButtonDown(0) && !Raycasting.IsMouseOverGUI() && HoveredObject.HasNodeRef())
                {
                    if (from == null)
                    {
                        // No source selected yet; this interaction is meant to set the source.
                        from = HoveredObject;
                    }
                    else if (to == null)
                    {
                        // Source is already set; this interaction is meant to set the target.
                        to = HoveredObject;
                    }
                }
            }
            // Note: from == to may be possible.
            if (from != null && to != null)
            {
                // We have both source and target of the edge.
                // FIXME: In the future, we need to query the edge type from the user.
                memento = new Memento(from, to, defaultEdgeType);
                createdEdge = CreateEdge(memento);

                // action is completed (successfully or not; it does not matter)
                from = null;
                to = null;
                result = createdEdge != null;
                AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NewEdgeSound, createdEdge, true);
                CurrentState = result ? IReversibleAction.Progress.Completed : IReversibleAction.Progress.NoEffect;
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
        /// Used to execute the <see cref="AddEdgeAction"/> from the context menu.
        /// It ensures that the <see cref="Update"/> method performs the execution via context menu.
        /// </summary>
        /// <param name="source">Is the source node of the edge.</param>
        public void ContextMenuExecution(GameObject source)
        {
            from = source;
            ShowNotification.Info("Select target", "Next, select a target node for the line.");
        }

        /// <summary>
        /// Undoes this AddEdgeAction
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameEdgeAdder.Remove(createdEdge);
            new DeleteNetAction(createdEdge.name).Execute();
            Destroyer.Destroy(createdEdge);
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
            if (memento.From == null)
            {
                memento.From = GraphElementIDMap.Find(memento.FromID);
            }
            if (memento.To == null)
            {
                memento.To = GraphElementIDMap.Find(memento.ToID);
            }
            try
            {
                /// If <see cref="CreateEdge(Memento)"/> was called from Update
                /// when the edge is created for the first time, <see cref="memento.edgeID"/>
                /// will not be set. Then the creation process triggered by <see cref="GameEdgeAdder"/>
                /// will create a new unique id for the edge. If <see cref="CreateEdge(Memento)"/>
                /// is called from <see cref="Redo"/>, <see cref="memento.edgeID"/> has
                /// a valid edge id (set by the previous call to <see cref="CreateEdge(Memento)"/>.
                GameObject result = GameEdgeAdder.Add(memento.From, memento.To, memento.EdgeType);
                UnityEngine.Assertions.Assert.IsNotNull(result);
                new AddEdgeNetAction(memento.From.name, memento.To.name, memento.EdgeType).Execute();
                return result;
            }
            catch (Exception e)
            {
                ShowNotification.Error("New edge", $"An edge could not be created: {e.Message}.");
                Debug.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewEdge"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.NewEdge;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                memento.From.name,
                memento.To.name,
                createdEdge.name
            };
        }
    }
}
