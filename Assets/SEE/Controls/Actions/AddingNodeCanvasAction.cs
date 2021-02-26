using UnityEngine;
using UnityEngine.UI;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Creates a clone of a canvas-prefab for adding a new node. Extracts these values from the canvas 
    /// after closing it.
    /// </summary>
    public class AddingNodeCanvasAction : NodeCanvasAction
    {
        /// <summary>
        /// The path of the NewNodeCanvas prefab without the file extension .prefab.
        /// </summary>
        private static readonly string prefabPath = "Prefabs/NewNodeCanvas";

        void Start()
        {
            /// Note: It is important that the Prefab is contained in the Resources folder to use the 
            /// Resources.Load method.
            InstantiatePrefab(prefabPath);
            Canvas.transform.SetParent(gameObject.transform);
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
    }
}