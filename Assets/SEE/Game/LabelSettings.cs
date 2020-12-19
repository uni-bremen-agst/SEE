namespace SEE.Game
{
    /// <summary>
    /// Setting for labels to be shown above game nodes.
    /// </summary>
    public class LabelSettings
    {
        /// <summary>
        /// If true, a label with the node's SourceName will be displayed above each node.
        /// </summary>
        public bool Show = true;
        /// <summary>
        /// The distance between the top of the node and its label.
        /// </summary>
        public float Distance = 0.2f;
        /// <summary>
        /// The font size of the node's label.
        /// </summary>
        public float FontSize = 0.4f;
    }
}
