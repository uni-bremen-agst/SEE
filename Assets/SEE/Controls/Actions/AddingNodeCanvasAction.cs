using SEE.GO.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Creates a clone of a canvas prefab for adding a new node. Extracts these values from the canvas 
    /// after closing it.
    /// </summary>
    public class AddingNodeCanvasAction : NodeCanvasAction
    {
        /// <summary>
        /// The path of the NewNodeCanvas prefab without the file extension .prefab.
        /// </summary>
        private static readonly string prefabPath = "Prefabs/NewNodeCanvas";

        /// <summary>
        /// True if the add-node process is canceled, else false.
        /// </summary>
        public static bool Canceled { get; set; } = false;

        /// <summary>
        /// True if the user adds a new node by pushing the "add" button, else false.
        /// </summary>
        public static bool AddNode { get; set; } = false;

        /// <summary>
        /// The addNodeAction instance where the progress state has to be set.
        /// </summary>
        public AddNodeAction addNodeAction;

        private void Start()
        {
            /// Note: It is important that the Prefab is contained in the Resources folder to use the 
            /// Resources.Load method.
            InstantiatePrefab(prefabPath);
            Canvas.transform.SetParent(gameObject.transform);
        }

        private void Update()
        {
            if (AddNode)
            {
                addNodeAction.Progress = AddNodeAction.ProgressState.CanvasIsClosed;
                canvasObject.GetComponent<AddingNodeCanvasAction>().GetNodeValues();
                Finish();
                AddNode = false;
            }
            if (Canceled)
            {
                addNodeAction.Progress = AddNodeAction.ProgressState.AddingIsCanceled;
                Finish();
                Canceled = false;
            }
        }

        /// <summary>
        /// Extracts the given node name, the node type and whether it is an inner node or a leaf from the canvas.
        /// Therefore, it extracts the string from the InputFields on the prefab.
        /// Note: The sequences of the extracted arrays are based on the sequence of the components in the prefab.
        /// </summary>
        public void GetNodeValues()
        {
            // FIXME: this part has to be removed by the new UI Team
            AddingNodeCanvasAction script = gameObject.GetComponent<AddingNodeCanvasAction>();

            Component[] c = script.Canvas.GetComponentsInChildren<InputField>();
            InputField inputname = (InputField)c[0];
            InputField inputtype = (InputField)c[1];

            Component toggleGroup = script.Canvas.GetComponentInChildren<ToggleGroup>();
            Toggle[] toggles = toggleGroup.GetComponentsInChildren<Toggle>();

            if (toggles[0].isOn)
            {
                AddNodeAction.IsInnerNode = true;
            }
            if (toggles[1].isOn)
            {
                AddNodeAction.IsInnerNode = false;
            }
            string inputNodename = inputname.text;
            string inputNodetype = inputtype.text;
            // until here 

            AddNodeAction.NodeName = inputNodename;
            AddNodeAction.NodeType = inputNodetype;
        }

        /// <summary>
        /// Destroys the AddingNodeCanvasAction and deselects the selected object. Re-allows the
        /// interaction with the menu.
        /// </summary>
        public void Finish()
        {
            CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
            generator.DestroyAddNodeCanvasAction();
            PlayerMenu.InteractionIsForbidden = false;
            InteractableObject.UnselectAll(true);
        }
    }
}