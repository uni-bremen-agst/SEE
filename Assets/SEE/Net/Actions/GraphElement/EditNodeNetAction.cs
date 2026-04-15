using SEE.DataModel.DG;
using SEE.Game.SceneManipulation;
using SEE.GO;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class is responsible for the edit-node process via network from one client
    /// to all others and to the server.
    /// </summary>
    public class EditNodeNetAction : NodeNetAction
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
        /// Constructor.
        /// </summary>
        /// <param name="gameNodeID">The unique name of the gameobject the node belongs to.</param>
        /// <param name="sourceName">The new source name.</param>
        /// <param name="type">The new node type.</param>
        public EditNodeNetAction(string gameNodeID, string sourceName, string type) : base(gameNodeID)
        {
            SourceName = sourceName;
            NodeType = type;
        }

        /// <summary>
        /// Sets the attributes of the GameObject on the client side.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Node node = Find(GraphElementID).GetNode();
            GameNodeEditor.ChangeName(node, SourceName);
            GameNodeEditor.ChangeType(node, NodeType);
        }
    }
}
