using System;
using System.Collections.Generic;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// A type graph element. Either a node or an edge.
    /// </summary>
    public abstract class GraphElement : Attributable
    {
        /// <summary>
        /// The type of the graph element.
        /// </summary>
        private string type;

        /// <summary>
        /// The graph this graph element is contained in. May be null if
        /// the element is currently not in a graph.
        /// 
        /// IMPORTANT NOTE: This attribute will not be serialized. It may
        /// be null at run-time or in the editor inspection view.
        /// </summary>
        protected Graph graph;

        /// <summary>
        /// The graph this graph element is contained in. May be null if
        /// the element is currently not in a graph.
        /// 
        /// Note: The set operation is intended only for Graph.
        /// 
        /// IMPORTANT NOTE: This attribute will not be serialized. It may
        /// be null at run-time or in the editor inspection view.
        /// </summary>
        public Graph ItsGraph
        {
            get => graph;
            set => graph = value;
        }

        /// <summary>
        /// The type of this graph element.
        /// </summary>
        public virtual string Type
        {
            get => type;
            set => type = !string.IsNullOrEmpty(value) ? value : Graph.UnknownType;
        }

        /// <summary>
        /// True if the type of this graph element is a super type of given type or equal to
        /// given type. In other words, type --extends*--> this.Type.
        /// 
        /// IMPORTANT NOTE: Currently, we do not have a type hierarchy of the underlying
        /// graph, hence, we only test whether both types are equal.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>true iff type --extends*--> this.Type</returns>
        public bool Has_Supertype_Of(string type)
        {
            // FIXME: We currently do not have the type hierarchy, so we cannot know
            // which type subsumes which other type. For the time being, we insist that
            // the types must be the same.
            return this.type == type;
        }

        /// <summary>
        /// Returns true if <paramref name="other"/> meets all of the following conditions: 
        /// (1) is not null
        /// (2) has exactly the same C# type
        /// (3) has exactly the same attributes with exactly the same values as this graph element
        /// (4) has the same type name
        /// 
        /// Note: This graph element and the other graph element may or may not be in the same graph.
        /// </summary>
        /// <param name="other">to be compared to</param>
        /// <returns>true if equal</returns>
        public override bool Equals(Object other)
        {
            if (!base.Equals(other))
            {
                GraphElement otherNode = other as GraphElement;
                if (other != null)
                {
                    Report(ID + " " + otherNode.ID + " have differences");
                }
                return false;
            }
            else
            {
                GraphElement graphElement = other as GraphElement;
                bool equal = type == graphElement.type;
                if (!equal)
                {
                    Report("The types are different");
                }
                return equal;
            }
        }

        /// <summary>
        /// A unique identifier (unique within the same graph).
        /// </summary>
        public abstract string ID { set; get; }

        /// <summary>
        /// Returns a string representation of the graph element's type and all its attributes and
        /// their values.
        /// </summary>
        /// <returns>string representation of type and all attributes</returns>
        public override string ToString()
        {
            return " \"type\": " + type + "\",\n" + base.ToString();
        }

        /// <summary>
        /// Creates deep copies of attributes where necessary. Is called by
        /// Clone() once the copy is created. Must be extended by every 
        /// subclass that adds fields that should be cloned, too.
        /// 
        /// The clone will have all attributes and also the type of this graph element,
        /// but will not be contained in any graph.
        /// </summary>
        /// <param name="clone">the clone receiving the copied attributes</param>
        protected override void HandleCloned(object clone)
        {
            base.HandleCloned(clone);
            GraphElement target = (GraphElement)clone;
            target.type = type;
            target.graph = null;
        }

        public override int GetHashCode()
        {
            int hashCode = 316397938;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(type);
            hashCode = hashCode * -1521134295 + EqualityComparer<Graph>.Default.GetHashCode(graph);
            hashCode = hashCode * -1521134295 + EqualityComparer<Graph>.Default.GetHashCode(ItsGraph);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ID);
            return hashCode;
        }
    }
}