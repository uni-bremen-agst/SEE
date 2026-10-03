namespace SEE.Graphs.Events
{
    /// <summary>
    /// Type of change to a graph element (node or edge, including "part-of" edges).
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// The graph element has been added.
        /// </summary>
        Addition,

        /// <summary>
        /// The graph element has been removed.
        /// </summary>
        Removal
    }
}
