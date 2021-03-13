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

        private List<Tuple<GameObject,string,string>> editedNodes = new List<Tuple<GameObject,string,string>>();

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
                    if(nodeToEdit != null)
                    {
                        Debug.Log("now");
                        editedNodes.Add(nodeToEdit);
                        nodeToEdit = null;
                        Debug.Log(editedNodes.Count);
                    }
                    break;

                case ProgressState.NodeSelected:
                    if (canvasObject.GetComponent<EditNodeCanvasAction>() == null)
                    {
                        CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
                        EditNodeCanvasAction script = generator.InstantiateEditNodeCanvas(this);
                        script.nodeToEdit = hoveredObject.GetComponent<NodeRef>().Value;
                        script.gameObjectID = hoveredObject.name;
                        nodeToEdit = new Tuple<GameObject, string, string>(hoveredObject, hoveredObject.GetComponent<NodeRef>().Value.SourceName, hoveredObject.GetComponent<NodeRef>().Value.Type);
                        Debug.Log(nodeToEdit.Item1);
                        Debug.Log(nodeToEdit.Item2);
                        Debug.Log(nodeToEdit.Item3);
                    }
                    break;

                case ProgressState.EditIsCanceled:
                    CanvasGenerator canvasGenerator = canvasObject.GetComponent<CanvasGenerator>();
                    canvasGenerator.DestroyEditNodeCanvasAction();
                    hoveredObject = null;
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
            Debug.Log("Undo EditNode");
        }

        /// <summary>
        /// Redoes this DeleteAction
        /// </summary>
        public override void Redo()
        {
            Debug.Log("Redo EditNode");
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
        /// Returns a new instance of <see cref="EditNodeAction"/>.
        /// </summary>
        /// <returns></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new EditNodeAction();
        }
    }
}

