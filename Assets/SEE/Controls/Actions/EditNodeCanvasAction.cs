using SEE.DataModel.DG;
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

        private static bool editNode = false;
        /// <summary>
        /// True when the editNode button on the canvas was pushed, else false.
        /// </summary>
        public static bool EditNode { get => editNode; set => editNode = value; }

        /// <summary>
        /// The InputField for the new name of the node.
        /// </summary>
        InputField inputname;

        /// <summary>
        /// The InputField for the new type of the node.
        /// </summary>
        InputField inputtype;

        /// <summary>
        /// The gameObject that contains the canvasPrefab.
        /// </summary>
        public GameObject canvasObject;

        /// <summary>
        /// The path of the EditNodeCanvas prefab without the file extension .prefab.
        /// </summary>
        private const string prefabPath = "Prefabs/EditNodeCanvas";

        void Start()
        {
            InstantiatePrefab(prefabPath);
            Canvas.transform.SetParent(gameObject.transform);
            Component[] c = Canvas.GetComponentsInChildren<InputField>();
            inputname = (InputField)c[0];
            inputtype = (InputField)c[1];
            inputname.text = nodeToEdit.SourceName;
            inputtype.text = nodeToEdit.Type;
            canvasObject = GameObject.Find("CanvasObject");
        }

        /// <summary>
        /// Gets the values of the canvas input fields after pushing the edit button and removes the
        /// canvas gameObject. Saves them in the DesktopEditNodeAction.
        /// </summary>
        void Update()
        {
            if (editNode)
            {
                if (!inputname.text.Equals(nodeToEdit.SourceName))
                {
                    nodeToEdit.SourceName = inputname.text;
                }
                if (!inputtype.text.Equals(nodeToEdit.Type))
                {
                    nodeToEdit.Type = inputtype.text;
                }
                CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
                generator.DestroyEditNodeCanvasAction();
                new EditNodeNetAction(nodeToEdit.SourceName, nodeToEdit.Type, gameObjectID).Execute(null);
                GameObject player = PlayerSettings.LocalPlayer;
                EditNodeAction current = player.GetComponent<EditNodeAction>();
                current.EditProgress = EditNodeAction.ProgressState.NoNodeSelected;
                InteractableObject.UnselectAll(true);
                editNode = false;
            }
        }
    }
}
