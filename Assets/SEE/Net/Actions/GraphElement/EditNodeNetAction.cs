using SEE.DataModel.DG;
using SEE.Game.SceneManipulation;
using SEE.GO;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class is responsible for the edit-node process via network from one client
    /// to all others and to the server.
    /// </summary>
    public class EditNodeNetAction : GraphElementNetAction
    {
        /// <summary>
        /// The new name of the node that has to be edited.
        /// </summary>
        public string SourceName;

        /// <summary>
        /// The new type of the node that has to be edited
        /// </summary>
        public string NodeType;

        /// <summary>
        /// Constructs an EditNodeNetAction object.
        /// </summary>
        /// <param name="nodeID">The unique name of the gameobject the node belongs to.</param>
        /// <param name="sourceName">The new source name.</param>
        /// <param name="type">The new node type.</param>
        public EditNodeNetAction(string nodeID, string sourceName, string type) : base(nodeID)
        {
            SourceName = sourceName;
            NodeType = type;
        }

        /// <summary>
        /// Things to execute on the server (none for this class)
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Sets the attributes of the GameObject on the client side.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Node node = Find(GameObjectID).GetNode();
            GameNodeEditor.ChangeName(node, SourceName);
            GameNodeEditor.ChangeType(node, NodeType);
        }
    }
}
