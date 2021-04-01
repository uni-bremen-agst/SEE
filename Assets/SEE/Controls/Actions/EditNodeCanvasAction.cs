using SEE.DataModel.DG;
using SEE.GO.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Creates a clone of a canvas prefab for editing an existing node. Extracts these new values from the 
    /// canvas after closing it.
    /// </summary>
    public class EditNodeCanvasAction : NodeCanvasAction
    {
        /// <summary>
        /// The specific node which has to be edited.
        /// </summary>
        public Node nodeToEdit;

        /// <summary>
        /// The id of the gameObject that contains the nodeToEdit. Necessary for the network action.
        /// </summary>
        public string gameObjectID;

        /// <summary>
        /// True when the editNode button on the canvas was pushed, else false.
        /// </summary>
        public static bool EditNode { get; set; } = false;

        /// <summary>
        /// True if the edit node process is canceled, else false.
        /// </summary>
        public static bool Canceled { get; set; } = false;

        /// <summary>
        /// The InputField for the new name of the node.
        /// </summary>
        InputField inputname;

        /// <summary>
        /// The InputField for the new type of the node.
        /// </summary>
        InputField inputtype;

        /// <summary>
        /// The path of the EditNodeCanvas prefab without the file extension .prefab.
        /// </summary>
        private const string prefabPath = "Prefabs/EditNodeCanvas";

        /// <summary>
        /// The editNodeAction instance where the progress state has to be set.
        /// </summary>
        public EditNodeAction editNodeAction;

        void Start()
        {
            PlayerMenu.InteractionIsForbidden = true;
            InstantiatePrefab(prefabPath);
            Canvas.transform.SetParent(gameObject.transform);
            Component[] c = Canvas.GetComponentsInChildren<InputField>();
            inputname = (InputField)c[0];
            inputtype = (InputField)c[1];
            inputname.text = nodeToEdit.SourceName;
            inputtype.text = nodeToEdit.Type;
        }

        /// <summary>
        /// Gets the values of the canvas input fields after pushing the edit button and removes the
        /// canvas gameObject. Saves them in the EditNodeAction. If the editing of the node is canceled,
        /// the EditNodeAction will be instructed and the process is canceled.
        /// </summary>
        void Update()
        {
            if (EditNode)
            {
                EditNodeAction.NodeName = inputname.text;
                EditNodeAction.NodeType = inputtype.text;
               
                editNodeAction.EditProgress = EditNodeAction.ProgressState.ValuesAreGiven;
                Finalise();
                EditNode = false;
            }
            if (Canceled)
            {
                editNodeAction.EditProgress = EditNodeAction.ProgressState.EditIsCanceled;
                Finalise();
                Canceled = false;
            }
        }

        /// <summary>
        /// Destroys the EditNodeCanvasAction and deselects the selected object. Re-allows the
        /// interaction with the menu.
        /// </summary>
        private void Finalise()
        {
            CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
            generator.DestroyEditNodeCanvasAction();
            PlayerMenu.InteractionIsForbidden = false;
            InteractableObject.UnselectAll(true);
        }
    }
}
