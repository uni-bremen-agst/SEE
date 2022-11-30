using System;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.DataModel
{
    /// <summary>
    /// An event representing a change to a graph component.
    /// May be used outside of reflexion analysis contexts, in which case the <see cref="ReflexionSubgraph"/> will be
    /// <c>None</c>.
    /// </summary>
    public abstract class GraphEvent : ChangeEvent
    {
        protected GraphEvent(Guid version, ReflexionSubgraph? affectedGraph = null, ChangeType? change = null) : base(version, affectedGraph, change)
        {
        }
    }

    /// <summary>
    /// An event representing a new version being introduced.
    /// Events following this one will have the new <see cref="VersionId"/>, while events before this
    /// (up until the last <see cref="VersionChangeEvent"/>) will have <see cref="OldVersion"/>.
    /// </summary>
    public class VersionChangeEvent : GraphEvent
    {
        /// <summary>
        /// The version before this one.
        /// </summary>
        private readonly Guid OldVersion;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="newVersion">new version ID</param>
        /// <param name="oldVersion">old version ID</param>
        public VersionChangeEvent(Guid newVersion, Guid oldVersion) : base(newVersion)
        {
            OldVersion = oldVersion;
        }

        protected override string Description() => $"Changed version from {OldVersion} to {VersionId}.";
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
        /// <param name="version">the graph version this event is associated to</param>
        /// <param name="edge">the edge being added or removed</param>
        /// <param name="change">the type of change to <paramref name="edge"/></param>
        /// <param name="affectedGraph">The graph the edge was added to or removed from</param>
        public EdgeEvent(Guid version, Edge edge, ChangeType change, ReflexionSubgraph? affectedGraph = null) : base(version, affectedGraph ?? edge.GetSubgraph(), change)
        {
            Edge = edge;
        }

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

        public HierarchyEvent(Guid version, Node parent, Node child, ChangeType change, ReflexionSubgraph? affectedGraph = null) : base(version, affectedGraph ?? child.GetSubgraph(), change)
        {
            if (affectedGraph == ReflexionSubgraph.Mapping || affectedGraph == ReflexionSubgraph.FullReflexion)
            {
                throw new ArgumentException("Only architecture or implementation hierarchy can be changed!");
            }

            Parent = parent;
            Child = child;
        }

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

        public NodeEvent(Guid version, Node node, ChangeType change, ReflexionSubgraph? affectedGraph = null) : base(version, affectedGraph ?? node.GetSubgraph(), change)
        {
            if (affectedGraph == ReflexionSubgraph.Mapping || affectedGraph == ReflexionSubgraph.FullReflexion)
            {
                throw new ArgumentException("Nodes can only be added to architecture or implementation!");
            }

            Node = node;
        }

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
        /// Will be <c>null</c> either if the attribute has been unset, or if it is a toggle attribute.
        /// </summary>
        public readonly T AttributeValue;

        public AttributeEvent(Guid version, Attributable attributable, string attributeName, T attributeValue, ChangeType change) : base(version, null, change)
        {
            Attributable = attributable;
            AttributeName = attributeName;
            AttributeValue = attributeValue;
        }

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

        public GraphElementTypeEvent(Guid version, string oldType, string newType, GraphElement element) : base(version)
        {
            OldType = oldType;
            NewType = newType;
            Element = element;
        }

        protected override string Description() => $"Type of '{Element.ToShortString()}' has changed from '{OldType}' to '{NewType}'";
    }
}