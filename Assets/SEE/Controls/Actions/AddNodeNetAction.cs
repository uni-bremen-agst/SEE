using SEE.Game;
using SEE.GO;
using SEE.Net;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class is responsible for adding a node via network from one client to all others and 
    /// to the server. 
    /// </summary>
    public class AddNodeNetAction : AbstractAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The city where the new node has to be placed.
        /// </summary>
        public SEECity City = null;

        /// <summary>
        /// The ID of the creators Game-Node object needed to find the city to place the new node in.
        /// Just one random object inside the code city from the new node.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// The ID of the parent gameObject of the new GameObject.
        /// </summary>
        public string ParentID;

        /// <summary>
        /// true, if the new node is an inner node, false if it is a leaf.
        /// </summary>
        public bool IsInnerNode;

        /// <summary>
        /// The id of the new node.
        /// </summary>
        public string NewNodeID;

        /// <summary>
        /// The position of the new node.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The scale of the new node.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// /Whether the the node should be placed or moved.
        /// true = place | false = dont place
        /// </summary>
        public bool Place;

        /// <summary>
        /// Whether a new node should be created.
        /// true = create new one | false = dont create new one
        /// </summary>
        public bool Create;

        /// <summary>
        /// True if a node was placed where it should not be placed. 
        /// It then needs to be destroyed in the network.
        /// </summary>
        public bool IllegalPlace;

        /// <summary>
        /// The name of the dummy object on which the AddNodeAction for network purpose is added.
        /// </summary>
        private static readonly string dummyName = "tmpNetNewNode";

        /// <summary>
        /// Creates a new NewNodeNetAction
        /// </summary>
        /// <param name="gameObjectID">the GameObject the city is attached to for the new node</param>
        /// <param name="isInnerNode">whether the new node should be an inner node</param>
        /// <param name="newNodeID">id for the new node</param>
        /// <param name="position">the position for the new node</param>
        /// <param name="place">whether the new node is in moving or in placing state</param>
        /// <param name="create">in the first method call a new node needs to be created, but only once (FIXME: meaning is unclear)</param>
        /// <param name="illegalPlace">whether the placement is illegal</param>
        public AddNodeNetAction
            (string gameObjectID,
             bool isInnerNode,
             string newNodeID,
             Vector3 position,
             Vector3 scale,
             string parentID,
             bool place,
             bool create,
             bool illegalPlace)
            : base()
        {
            this.GameObjectID = gameObjectID;
            this.ParentID = parentID;
            this.IsInnerNode = isInnerNode;
            this.NewNodeID = newNodeID;
            this.Position = position;
            this.Scale = scale;
            this.Place = place;
            this.Create = create;
            this.IllegalPlace = illegalPlace;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Creates a new GameObject on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                // Manages the creation of a new node on each client.
                if (Create)
                {
                    SceneQueries.GetCodeCity(GameObject.Find(GameObjectID).transform)?.gameObject.TryGetComponent(out City);
                    if (City != null)
                    {
                        //Just a gameObject to attatch the newNodeScript on
                        GameObject dummy = new GameObject();
                        dummy.AddComponent<AddNodeAction>();
                        dummy.GetComponent<AddNodeAction>().Network = true;
                        dummy.GetComponent<AddNodeAction>().NodeID = NewNodeID;
                        dummy.GetComponent<AddNodeAction>().City = City;
                        AddNodeAction.SetIsInnerNode(IsInnerNode);
                        dummy.GetComponent<AddNodeAction>().NewNode();
                        dummy.name = dummyName;

                    }
                }
                // Manages the placement of the new node on each client.
                else if (Place)
                {
                    if (IllegalPlace)
                    {
                        Object.Destroy(GameObject.Find(dummyName).GetComponent<AddNodeAction>().GONode);
                    }
                    else
                    {
                        GameObject.Find(dummyName).GetComponent<AddNodeAction>().NetworkPlaceNode(Position, Scale, ParentID);
                    }
                    Object.Destroy(GameObject.Find(dummyName).GetComponent<AddNodeAction>());
                    Object.Destroy(GameObject.Find(dummyName));
                }
                // Let the new node move with the cursor of the master.
                else
                {
                    GameObject dummyGameNode = GameObject.Find(dummyName).GetComponent<AddNodeAction>().GONode;
                    dummyGameNode.transform.position = Position;
                    dummyGameNode.SetScale(Scale);
                }
            }
        }
    }
}
