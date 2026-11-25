using System.Collections.Generic;
using SEE.UI.Notification;
using SEE.Extensions;
using SEE.Utils;
using System;
using UnityEngine;
using SEE.Audio;
using SEE.Graphs;
using SEE.SceneManipulation;
using SEE.XR;
using SEE.UserSettings;
using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.Controls.KeyActions;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Action to create an edge between two selected nodes.
    /// </summary>
    internal class AddEdgeAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="AddEdgeAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="AddEdgeAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new AddEdgeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddEdgeAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="AddEdgeAction"/>.</returns>
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
        /// Whether the initial source-selection notification has been shown.
        /// </summary>
        private bool sourceSelectionNotificationShown;

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
            /// The created edge ID.
            /// </summary>
            public string EdgeID;
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="from">The source of the edge.</param>
            /// <param name="to">The target of the edge.</param>
            /// <param name="edgeType">The edge type.</param>
            public Memento(GameObject from, GameObject to, string edgeType)
            {
                this.From = from;
                this.FromID = from.name;
                this.To = to;
                this.ToID = to.name;
                this.EdgeType = edgeType;
                this.EdgeID = null;
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
        /// <returns>True if completed.</returns>
        public override bool Update()
        {
            // Escape cancels a temporary context-menu action and restores the previous tool.
            if (ExecuteViaContextMenu && SEEInput.Cancel())
            {
                from = null;
                to = null;
                ExecuteViaContextMenu = false;
                IsCanceled = true;
                CurrentState = IReversibleAction.Progress.NoEffect;

                AudioManagerImpl.EnqueueSoundEffect(
                    IAudioManager.SoundEffect.DropSound);

                ShowNotification.Info(
                    "New edge canceled",
                    "Edge creation was canceled.",
                    duration: 4f,
                    log: false);

                return false;
            }

            bool result = false;

            // Assigning the game objects to be connected.
            // Checking whether the two game objects are not null and whether they are
            // actually nodes.
            if (UserSetting.IsVR)
            {
                if (XRSEEActions.Selected
                    && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                    && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef())
                {
                    if (from == null)
                    {
                        from = InteractableObject.HoveredObjectWithWorldFlag.gameObject;
                        XRSEEActions.Selected = false;

                        ShowNotification.Info(
                            "Select target",
                            "Next, select a target node for the line.",
                            duration: 5f,
                            log: false);
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
                if (HoveredObject != null
                    && Input.GetMouseButtonDown(0)
                    && !Raycasting.IsMouseOverGUI()
                    && HoveredObject.HasNodeRef())
                {
                    if (from == null)
                    {
                        // No source selected yet; this interaction is meant to set the source.
                        from = HoveredObject;

                        ShowNotification.Info(
                            "Select target",
                            "Next, select a target node for the line.",
                            duration: 5f,
                            log: false);
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

                // If creation failed, allow another attempt with different nodes.
                if (createdEdge != null)
                {
                    memento.EdgeID = createdEdge.name;
                }

                result = createdEdge != null;

                if (result)
                {
                    AudioManagerImpl.EnqueueSoundEffect(
                        IAudioManager.SoundEffect.NewEdgeSound,
                        createdEdge, true);
                }
                else
                {
                    AudioManagerImpl.EnqueueSoundEffect(
                        IAudioManager.SoundEffect.CancelSound,
                        from, false);
                }

                from = null;
                to = null;

                CurrentState = result
                    ? IReversibleAction.Progress.Completed
                    : IReversibleAction.Progress.NoEffect;

                if (result)
                {
                    ExecuteViaContextMenu = false;
                }
            }

            // U or Escape clears the current selection without leaving the tool.
            if (SEEInput.Unselect() || (!ExecuteViaContextMenu && SEEInput.Cancel()))
            {
                from = null;
                to = null;

                ShowNotification.Info(
                    "Selection cleared",
                    "Select a source node to start a new edge.",
                    duration: 4f,
                    log: false);

                sourceSelectionNotificationShown = true;
            }

            // Show the initial instruction once for the regular New Edge tool.
            // Context-menu actions already receive their source node and target instruction.
            if (!ExecuteViaContextMenu
                && !IsCanceled
                && !sourceSelectionNotificationShown
                && from == null
                && !result)
            {
                sourceSelectionNotificationShown = true;

                ShowNotification.Info(
                    "Select source",
                    "First, select a source node for the new edge.",
                    duration: 5f,
                    log: false);
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
            ExecuteViaContextMenu = true;
            ShowNotification.Info("Select target", "Next, select a target node for the line.");
        }

        /// <summary>
        /// Undoes this AddEdgeAction
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (createdEdge == null)
            {
                createdEdge = GraphElementIDMap.Find(memento.EdgeID);
            }
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
        /// <param name="memento">Information needed to create the edge.</param>
        /// <returns>A new edge or null.</returns>
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
        /// <returns><see cref="ActionStateType.NewEdge"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.NewEdge;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>All IDs of gameObjects manipulated by this action.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                memento.FromID,
                memento.ToID,
                memento.EdgeID
            };
        }
    }
}
