using SEE.DataModel.DG;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Creates a clone of a canvas prefab for editing an existing node. Extracts these new values from the canvas after closing it.
    /// </summary>
    public class EditNodeCanvasAction : NodeCanvasAction
    {
        /// <summary>
        /// The specific node which has to be edited.
        /// </summary>
        public Node nodeToEdit;

        /// <summary>
        /// The id of the gameObject, which contains the nodeToEdit. Nessecary for the Network-Action.
        /// </summary>
        public string gameObjectID;

        /// <summary>
        /// A bool which is true after pushing the editNode-button on the canvas, else false.
        /// </summary>
        private static bool editNode = false;

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
        /// The gameObject which contains the canvasPrefab.
        /// </summary>
        public GameObject canvasObject;

        /// <summary>
        /// The directory of the Edit-Node-Prefab.
        /// </summary>
        private string prefabDirectory = "Prefabs/EditNodeCanvas";


        void Start()
        {
            InstantiatePrefab(prefabDirectory);
            canvas.transform.SetParent(gameObject.transform);
            Component[] c = canvas.GetComponentsInChildren<InputField>();
            inputname = (InputField)c[0];
            inputtype = (InputField)c[1];
            inputname.text = nodeToEdit.SourceName;
            inputtype.text = nodeToEdit.Type;
            canvasObject = GameObject.Find("CanvasObject");
        }

        /// <summary>
        /// Gets the values of the canvas-inputfields after pushing the edit-button and removes the canvas-gameObject.
        /// Saves them in the DesktopEditNodeAction-script.
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
                generator.DestroyEditNodeCanvas();
                new EditNodeNetAction(nodeToEdit.SourceName, nodeToEdit.Type, gameObjectID).Execute(null);
                GameObject g = GameObject.Find("Player Desktop");
                EditNodeAction current = g.GetComponent<EditNodeAction>();
                current.EditProgress = EditNodeAction.Progress.NoNodeSelected;
                InteractableObject.UnselectAll(true);
                editNode = false;

            }
        }
    }
}