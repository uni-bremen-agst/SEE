using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to edit an existing node.
    /// </summary>
    public class EditNodeAction : AbstractPlayerAction
    {

        /// <summary>
        /// The life cycle of this edit action.
        /// </summary>
        public enum ProgressState
        {
            NoNodeSelected,  // initial state when no node is selected
            NodeSelected,    // a node is currently selected
            EditIsCanceled,  // the edit action is canceled
        }

        /// <summary>
        /// The current state of the edit-node process.
        /// </summary>
        public ProgressState EditProgress { get; set; } = ProgressState.NoNodeSelected;

        /// <summary>
        /// All nodes and their previous names and types edited by this action. 
        /// </summary>
        private List<Tuple<GameObject, string, string>> editedNodes = new List<Tuple<GameObject, string, string>>();

        /// <summary>
        /// All new names and new types of nodes, which was been undone.
        /// </summary>
        private List<Tuple<string, string, Node>> changesToRedone = new List<Tuple<string, string, Node>>();

        /// <summary>
        /// The previous values (name and type) of the GameObject node to be edited.
        /// </summary>
        private Tuple<GameObject, string, string> nodeToEdit;

        public override void Start()
        {
            if (!InitializeCanvasObject())
            {
                Debug.LogError($"No canvas object named {nameOfCanvasObject} could be found in the scene.\n");
                return;
            }
            InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
            instantiated = true;
        }

        /// <summary>
        /// The Update method's behavior depends on the edit-progress state (sequential series).
        /// NoNodeSelected: Waits until a node is selected by selecting a game node via the mouse button.
        /// NodeSelected: Instantiates the canvasObject if a gameNode is selected.
        /// EditIsCanceled: Removes the canvas and resets all values if the process is canceled.
        /// </summary>
        public override void Update()
        {
            switch (EditProgress)
            {
                case ProgressState.NoNodeSelected:
                    if (hoveredObject != null && Input.GetMouseButtonDown(0))
                    {
                        EditProgress = ProgressState.NodeSelected;
                    }
                    // this case will be reached after editing a node for saving
                    // the previous values of the node to edit in a history (Undo/redo).
                    if (nodeToEdit != null)
                    {
                        editedNodes.Add(nodeToEdit);
                        nodeToEdit = null;
                    }
                    break;

                case ProgressState.NodeSelected:
                    if (canvasObject.GetComponent<EditNodeCanvasAction>() == null)
                    {
                        CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
                        EditNodeCanvasAction script = generator.InstantiateEditNodeCanvas(this);
                        script.nodeToEdit = hoveredObject.GetComponent<NodeRef>().Value;
                        script.gameObjectID = hoveredObject.name;
                        nodeToEdit = new Tuple<GameObject, string, string>
                            (hoveredObject, hoveredObject.GetComponent<NodeRef>().Value.SourceName, hoveredObject.GetComponent<NodeRef>().Value.Type);
                        changesToRedone.Clear();
                    }
                    break;

                case ProgressState.EditIsCanceled:
                    CanvasGenerator canvasGenerator = canvasObject.GetComponent<CanvasGenerator>();
                    canvasGenerator.DestroyEditNodeCanvasAction();
                    hoveredObject = null;
                    nodeToEdit = null;
                    EditProgress = ProgressState.NoNodeSelected;
                    break;

                default:
                    throw new System.NotImplementedException("Unhandled case.");
            }
        }

        /// <summary>
        /// Undoes this EditNodeAction
        /// </summary>
        public override void Undo()
        {
            foreach (Tuple<GameObject, string, string> tuple in editedNodes)
            {
                changesToRedone.Add(new Tuple<string,string,Node>
                    (tuple.Item1.GetComponent<NodeRef>().Value.SourceName, tuple.Item1.GetComponent<NodeRef>().Value.Type, tuple.Item1.GetComponent<NodeRef>().Value));
                tuple.Item1.GetComponent<NodeRef>().Value.SourceName = tuple.Item2;
                tuple.Item1.GetComponent<NodeRef>().Value.Type = tuple.Item3;
            }
        }

        /// <summary>
        /// Redoes this DeleteAction
        /// </summary>
        public override void Redo()
        {
            foreach(Tuple<string, string, Node> tuple in changesToRedone)
            {
                UpdateNode(tuple.Item1, tuple.Item2, tuple.Item3);
            }
        }

        /// <summary>
        /// Is called when a new action is started at the end of this action
        /// </summary>
        public override void Stop()
        {
            CanvasGenerator canvasGenerator = canvasObject.GetComponent<CanvasGenerator>();
            canvasGenerator.DestroyEditNodeCanvasAction();
            hoveredObject = null;
            EditProgress = ProgressState.NoNodeSelected;
        }

        /// <summary>
        /// Updates the values such as nodename and nodetype of a specific <paramref name="node"/>
        /// </summary>
        /// <param name="newName">the new name of the <paramref name="node"/></param>
        /// <param name="newType">the new type of the <paramref name="node"/></param>
        /// <param name="node">the node to be edited</param>
        public static void UpdateNode(string newName, string newType, Node node)
        {
            if (!newName.Equals(node.SourceName))
            {
                node.SourceName = newName;
            }
            if (!newType.Equals(node.Type))
            {
                node.Type = newType;
            }
        }

        /// <summary>
        /// Returns a new instance of <see cref="EditNodeAction"/>.
        /// </summary>
        /// <returns></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new EditNodeAction();
        }
    }
}

