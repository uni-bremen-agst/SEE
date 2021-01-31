using SEE.Game;
using SEE.Net;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class is responsible for the new-node-process via network from one client to all others and to the server. 
    /// </summary>
    public class NewNodeNetAction : AbstractAction
    {
        /// <summary>
        /// The city where the new node has to be placed
        /// </summary>
        public SEECity city = null;

        /// <summary>
        /// The ID of the creators Game-Node-Object
        /// </summary>
        public string gameObjectID;

        /// <summary>
        /// The ID of the parent-gameObject of the new GameObject
        /// </summary>
        public string parentID;

        /// <summary>
        /// true, if the new Node is an inner node, false if its a leaf.
        /// </summary>
        public bool isInnerNode;

        /// <summary>
        /// The id of the new node.
        /// </summary>
        public string id;

        /// <summary>
        /// The sourceName of the new node.
        /// </summary>
        public string sourceName;

        /// <summary>
        /// The type of the new node.
        /// </summary>
        public string type;

        /// <summary>
        /// The position of the new node.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The scale of the new node.
        /// </summary>
        public Vector3 scale;

        /// <summary>
        /// Creates a new NewNodeNetAction
        /// </summary>
        /// <param name="GameObjectID">the  GameObject on that the city is attached for the new node</param>
        /// <param name="IsInnerNode">should it be a inner node</param>
        /// <param name="NodeMetrics1">id for the new node</param>
        /// <param name="NodeMetrics2">name for the new node</param>
        /// <param name="NodeMetrics3">type for the new node</param>
        /// <param name="Position">the postition for the new node</param>
        public NewNodeNetAction(string gameObjectID, bool isInnerNode, string nodeMetrics1, string nodeMetrics2, string nodeMetrics3, Vector3 position, Vector3 scale, string parentID) : base()
        {
            this.gameObjectID = gameObjectID;
            this.parentID = parentID;
            this.isInnerNode = isInnerNode;
            id = nodeMetrics1;
            sourceName = nodeMetrics2;
            type = nodeMetrics3;
            this.position = position;
            this.scale = scale;
        }

        /// <summary>
        /// Things to Execute on the Server (None for this Class). Nessecary, because its abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Things to Execute on the Client Creates a new GameObject on each client
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                SceneQueries.GetCodeCity(GameObject.Find(gameObjectID).transform)?.gameObject.TryGetComponent(out city);
                if (city != null)
                {
                    GameObject dummy = new GameObject();
                    dummy.AddComponent<NewNodeAction>();
                    dummy.GetComponent<NewNodeAction>().City = city;
                    dummy.GetComponent<NewNodeAction>().SetIsInnerNode(isInnerNode);
                    dummy.GetComponent<NewNodeAction>().NetworkNewNode(position, scale, parentID);
                }
            }
        }

    }
}