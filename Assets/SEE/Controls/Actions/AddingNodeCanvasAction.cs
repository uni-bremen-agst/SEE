using UnityEngine;
using UnityEngine.UI;

namespace SEE.Controls.Actions
{
    public class AddingNodeCanvasAction : NodeCanvasAction
    {
        /// <summary>
        /// The directory of the AddingNodeCanvas-prefab.
        /// </summary>
        private string prefabDirectory = "Prefabs/NewNodeCanvas";

        void Start()
        {
            /// Note: Its important that the Prefab lays inside of the Resources-Folder to use the Resources.Load-Method.
            InstantiatePrefab(prefabDirectory);
            canvas.transform.SetParent(gameObject.transform);
        }

        /// <summary>
        /// Extracts the given Nodename, the nodetype and wether it is a inner node or a leaf from the canvas.
        /// Therefore, it extracts the string from the InputFields on the prefab.
        /// Note: The sequences of the extracted Arrays are based on the sequence of the components in the prefab.
        /// </summary>
        public void GetNodeMetrics()
        {
            string inputNodename;
            string inputNodetype;

            //this part has to be removed by the new UI-Team
            AddingNodeCanvasAction script = gameObject.GetComponent<AddingNodeCanvasAction>();

            Component[] c = script.canvas.GetComponentsInChildren<InputField>();
            InputField inputname = (InputField)c[0];
            InputField inputtype = (InputField)c[1];


            Component toggleGroup = script.canvas.GetComponentInChildren<ToggleGroup>();
            Toggle[] toggles = toggleGroup.GetComponentsInChildren<Toggle>();

            if (toggles[0].isOn)
            {
                NewNodeAction.IsInnerNode = true;
            }
            if (toggles[1].isOn)
            {
                NewNodeAction.IsInnerNode = false;
            }
            inputNodename = inputname.text;
            inputNodetype = inputtype.text;
            //until here 


            NewNodeAction.Nodename = inputNodename;
            NewNodeAction.Nodetype = inputNodetype;

        }

    }
}