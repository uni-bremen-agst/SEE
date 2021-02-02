using SEE.Game;
using SEE.GO;
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
        /// Should the node be placed or moved
        /// </summary>
        public bool place;

        /// <summary>
        /// Should a new node be created
        /// </summary>
        public bool create;

        public string dummyName = "tmpNetNewNode";

        /// <summary>
        /// Creates a new NewNodeNetAction
        /// </summary>
        /// <param name="gameObjectID">the  GameObject on that the city is attached for the new node</param>
        /// <param name="isInnerNode">should it be a inner node</param>
        /// <param name="nodeMetrics1">id for the new node</param>
        /// <param name="nodeMetrics2">name for the new node</param>
        /// <param name="nodeMetrics3">type for the new node</param>
        /// <param name="position">the position for the new node</param>
        /// <param name="place">is the new node in moving or in placing state</param>
        /// <param name="create">in the first method call a new node needs to be created, but only once</param>
        public NewNodeNetAction(string gameObjectID, bool isInnerNode, string nodeMetrics1,Vector3 position, Vector3 scale, string parentID, bool place, bool create) : base()
        {
            this.gameObjectID = gameObjectID;
            this.parentID = parentID;
            this.isInnerNode = isInnerNode;
            id = nodeMetrics1;
            //sourceName = nodeMetrics2;
            //type = nodeMetrics3;
            this.position = position;
            this.scale = scale;
            this.place = place;
            this.create = create;
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
                if (create)
                {
                    SceneQueries.GetCodeCity(GameObject.Find(gameObjectID).transform)?.gameObject.TryGetComponent(out city);
                    if (city != null)
                    {
                        GameObject dummy = new GameObject();
                        dummy.AddComponent<NewNodeAction>();
                        dummy.GetComponent<NewNodeAction>().NodeID = id;
                        dummy.GetComponent<NewNodeAction>().City = city;
                        
                        dummy.GetComponent<NewNodeAction>().SetIsInnerNode(isInnerNode);
                        dummy.GetComponent<NewNodeAction>().NewNode();
                        dummy.name = dummyName;
                        //dummy.GetComponent<NewNodeAction>().NetworkNewNode(position, scale, parentID);
                    }
                }
                else if(place)
                {
                    GameObject.Find(dummyName).GetComponent<NewNodeAction>().NetworkPlaceNode(position, scale, parentID);
                   
                }
                else
                {
                    Debug.Log(position);
                    GameObject.Find(dummyName).GetComponent<NewNodeAction>().GONode.transform.position = position;
                    GameObject.Find(dummyName).GetComponent<NewNodeAction>().GONode.SetScale(scale);
                }
            }
        }

    }
}