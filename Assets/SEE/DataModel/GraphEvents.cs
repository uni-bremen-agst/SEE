using System;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.DataModel.Events
{
    /// <summary>
    /// An event representing a change to a graph component.
    /// May be used outside of reflexion analysis contexts, in which case
    /// the <see cref="ReflexionSubgraphs"/> will be None.
    /// </summary>
    public abstract class GraphEvent : ChangeEvent
    {
        /// <summary>
        /// Creates a new instance of this graph event.
        /// </summary>
        /// <param name="version">The version ID associated with this event.</param>
        /// <param name="affectedGraph">The graph affected by this event.</param>
        /// <param name="change">The type of change this event represents.</param>
        protected GraphEvent(Guid version, ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(version, affectedGraph, change)
        {
        }
    }

    /// <summary>
    /// An event fired when an edge was added to the graph or removed from it.
    /// Note that this event will never originate from the edge which was changed; it will instead be emitted
    /// from the graph it is contained in.
    /// </summary>
    public class EdgeEvent : GraphEvent
    {
        /// <summary>
        /// The edge added to the graph or removed from it.
        /// </summary>
        public readonly Edge Edge;

        /// <summary>
        /// Constructor preserving the edge added to the graph or removed from it.
        /// </summary>
        /// <param name="version">The graph version this event is associated to.</param>
        /// <param name="edge">The edge being added or removed.</param>
        /// <param name="change">The type of change to <paramref name="edge"/>.</param>
        /// <param name="affectedGraph">The graph the edge was added to or removed from.</param>
        public EdgeEvent(Guid version, Edge edge, ChangeType change, ReflexionSubgraphs? affectedGraph = null)
            : base(version, affectedGraph ?? edge.GetSubgraph(), change)
        {
            Edge = edge;
        }

        /// <summary>
        /// Returns a string description of this event, including the affected graph, the edge, and the type of change.
        /// </summary>
        /// <returns>A string describing the edge event.</returns>
        protected override string Description() =>
            $"{Affected.ToShortString()} edge '{Edge.ToShortString()}' has been {(Change == ChangeType.Addition ? "Added" : "Removed")}.";
    }

    /// <summary>
    /// An event fired when a node is added or removed as a child.
    /// Note that this event will never be emitted from the <see cref="Parent"/>, only from the <see cref="Child"/>
    /// or its graph.
    /// </summary>
    public class HierarchyEvent : GraphEvent
    {
        /// <summary>
        /// The parent node, having <see cref="Child"/> as its direct child.
        /// </summary>
        public readonly Node Parent;

        /// <summary>
        /// The child node, being a direct descendant of <see cref="Parent"/>.
        /// </summary>
        public readonly Node Child;

        /// <summary>
        /// Creates a new hierarchy event, representing the addition or removal of <paramref name="child"/>
        /// as a child of <paramref name="parent"/>.
        /// </summary>
        /// <param name="version">The graph version this event is associated to.</param>
        /// <param name="parent">The parent node.</param>
        /// <param name="child">The child node.</param>
        /// <param name="change">The type of change to the hierarchy.</param>
        /// <param name="affectedGraph">The graph the hierarchy change is associated to.</param>
        /// <exception cref="ArgumentException">Thrown when attempting to change a mapping or full reflexion hierarchy.</exception>
        public HierarchyEvent(Guid version, Node parent, Node child, ChangeType change, ReflexionSubgraphs? affectedGraph = null) : base(version, affectedGraph ?? child.GetSubgraph(), change)
        {
            if (affectedGraph == ReflexionSubgraphs.Mapping || affectedGraph == ReflexionSubgraphs.FullReflexion)
            {
                throw new ArgumentException("Only architecture or implementation hierarchy can be changed!");
            }

            Parent = parent;
            Child = child;
        }

        /// <summary>
        /// Returns a string description of this event, including the affected graph, the parent
        /// and child nodes, and the type of change.
        /// </summary>
        /// <returns>A string description of this event.</returns>
        protected override string Description() =>
            $"{Affected.ToShortString()} node '{Child.ToShortString()}' {(Change == ChangeType.Addition ? "added as child to" : "removed as child from")} parent '{Parent.ToShortString()}'";
    }

    /// <summary>
    /// An event fired when a node is added to or removed from the graph.
    /// Note that this event will never originate from the node which was added or removed; it will instead be emitted
    /// from the graph it is (or was) contained in.
    /// </summary>
    public class NodeEvent : GraphEvent
    {
        /// <summary>
        /// The node which has either been added to or deleted from the graph.
        /// </summary>
        public readonly Node Node;

        /// <summary>
        /// Creates a new node event, representing the addition or removal of <paramref name="node"/> to/from the graph.
        /// </summary>
        /// <param name="version">The graph version this event is associated to.</param>
        /// <param name="node">The node which has either been added to or deleted from the graph.</param>
        /// <param name="change">The type of change to the node.</param>
        /// <param name="affectedGraph">The graph the node change is associated to.</param>
        /// <exception cref="ArgumentException">Thrown when attempting to change a mapping or full reflexion hierarchy.</exception>
        public NodeEvent(Guid version, Node node, ChangeType change, ReflexionSubgraphs? affectedGraph = null)
            : base(version, affectedGraph ?? node.GetSubgraph(), change)
        {
            if (affectedGraph == ReflexionSubgraphs.Mapping || affectedGraph == ReflexionSubgraphs.FullReflexion)
            {
                throw new ArgumentException("Nodes can only be added to architecture or implementation!");
            }

            Node = node;
        }

        /// <summary>
        /// Returns a string description of this event, including the affected graph, the node, and the type of change.
        /// </summary>
        /// <returns>A string description of this event.</returns>
        protected override string Description() => $"node '{Node.ToShortString()}' {(Change == ChangeType.Addition ? "added to" : "removed from")} {Affected.ToShortString()}";
    }

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
