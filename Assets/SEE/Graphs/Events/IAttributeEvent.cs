using SEE.Graphs.DG;

namespace SEE.Graphs.Events
{
    /// <summary>
    /// An event fired when an attribute in a graph element is changed.
    /// </summary>
    public interface IAttributeEvent
    {
        /// <summary>
        /// The attributable (i.e., graph element) whose attribute was changed.
        /// </summary>
        public Attributable Attributable
        {
            get;
        }

        /// <summary>
        /// The name of the attribute that was changed.
        /// </summary>
        public string AttributeName
        {
            get;
        }
    }
}
