using System;
using SEE.DataModel.DG;

namespace SEE.DataModel.Events
{
    /// <summary>
    /// An event fired when the <see cref="GraphElement.Type"/> of a graph element changes.
    /// </summary>
    public class GraphElementTypeEvent : GraphEvent
    {
        /// <summary>
        /// The previous type of the graph element.
        /// </summary>
        public readonly string OldType;

        /// <summary>
        /// The new type of the graph element.
        /// </summary>
        public readonly string NewType;

        /// <summary>
        /// The element whose type was changed.
        /// </summary>
        public readonly GraphElement Element;

        /// <summary>
        /// Creates a new graph element type event, representing the change of the type of a graph element.
        /// </summary>
        /// <param name="version">The graph version this event is associated to.</param>
        /// <param name="oldType">The previous type of the graph element.</param>
        /// <param name="newType">The new type of the graph element.</param>
        /// <param name="element">The element whose type was changed.</param>
        public GraphElementTypeEvent(Guid version, string oldType, string newType, GraphElement element) : base(version)
        {
            OldType = oldType;
            NewType = newType;
            Element = element;
        }

        /// <summary>
        /// Returns a string description of this event, including the element, the old type, and the new type.
        /// </summary>
        /// <returns>A string description of this event.</returns>
        protected override string Description() => $"Type of '{Element.ToShortString()}' has changed from '{OldType}' to '{NewType}'";
    }
}
