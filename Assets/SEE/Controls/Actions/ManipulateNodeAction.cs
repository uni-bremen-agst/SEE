
namespace SEE.Controls.Actions
{
    /// <summary>
    /// Common super class of actions manipulating the name or type of a node.
    /// </summary>
    public abstract class ManipulateNodeAction : AbstractPlayerAction
    {
        /// <summary>
        /// The name of a new node given in the input field.
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        /// The type of a new node given in the input field.
        /// </summary>
        public string NodeType { get; set; }

    }
}