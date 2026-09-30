using System;
using SEE.DataModel.DG;

namespace SEE.DataModel.Events
{
    /// <summary>
    /// An event fired when an attribute in a graph element is changed.
    /// </summary>
    /// <typeparam name="T">type of the attribute value</typeparam>
    public class AttributeEvent<T> : GraphEvent, IAttributeEvent
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

        /// <summary>
        /// The value of the changed attribute.
        /// Will be null either if the attribute has been unset, or if it is a toggle attribute.
        /// </summary>
        public readonly T AttributeValue;

        /// <summary>
        /// Creates a new attribute event, representing the change of an attribute in a graph element.
        /// </summary>
        /// <param name="version">The graph version this event is associated to.</param>
        /// <param name="attributable">The attributable (i.e., graph element) whose attribute was changed.</param>
        /// <param name="attributeName">The name of the attribute that was changed.</param>
        /// <param name="attributeValue">The value of the changed attribute.</param>
        /// <param name="change">The type of change to the attribute.</param>
        public AttributeEvent(Guid version, Attributable attributable, string attributeName, T attributeValue, ChangeType change)
            : base(version, null, change)
        {
            Attributable = attributable;
            AttributeName = attributeName;
            AttributeValue = attributeValue;
        }

        /// <summary>
        /// Returns a string description of this event, including the attributable, the attribute name,
        /// and the type of change.
        /// </summary>
        /// <returns>A string description of this event.</returns>
        protected override string Description() => $"Attribute '{AttributeName}' has been {(Change == ChangeType.Addition ? "set to " + AttributeValue : "unset")} in {Attributable}";
    }
}
