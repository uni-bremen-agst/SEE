using System.Collections.Generic;
using SEE.Utils;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// A type graph element. Either a node or an edge.
    /// </summary>
    public abstract class GraphElement : Attributable
    {
        /// <summary>
        /// The name of the toggle attribute that marks "virtual" graph elements, which are
        /// elements that are not intended to be layouted or drawn in SEE and only
        /// exist in the underlying graph.
        /// </summary>
        public const string IsVirtualToggle = "IsVirtual";

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
            set
            {
                string oldType = type;
                type = !string.IsNullOrEmpty(value) ? value : Graph.UnknownType;
                Notify(new GraphElementTypeEvent(version, oldType, type, this));
            }
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
        public bool HasSupertypeOf(string type)
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
        public override bool Equals(object other)
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

        //--------------------------------------------
        // Information related to source-code location
        //--------------------------------------------

        /// <summary>
        /// Returns the path of the source file for this graph element.
        /// Note that not all graph elements may have a source file.
        /// If the graph element does not have this attribute, null is returned.
        /// </summary>
        /// <returns>path of source file or null</returns>
        public string Path()
        {
            TryGetString("Source.Path", out string result);
            // If this attribute cannot be found, result will have the standard value
            // for strings, which is null.
            return result;
        }

        /// <summary>
        /// Returns the path of the source file for this graph element relative to the project root.
        /// The project root is determined by calling <see cref="DataPath.ProjectFolder"/> if it is not supplied
        /// by <paramref name="projectFolder"/>.
        /// Note that not all graph elements may have a source file.
        /// If the graph element does not have this attribute, null is returned.
        /// </summary>
        /// <param name="projectFolder">The project's folder, containing the node's path.</param>
        /// <returns>relative path of source file or null</returns>
        public string RelativePath(string projectFolder = null)
        {
            // FIXME: The data model (graph) should be independent of Unity (here: DataPath.ProjectFolder()).
            return Path()?.Replace(projectFolder ?? DataPath.ProjectFolder(), string.Empty).TrimStart('/');
        }

        /// <summary>
        /// Returns the name of the source file for this graph element.
        /// Note that not all graph elements may have a source file.
        /// If the graph element does not have this attribute, null is returned.
        /// </summary>
        /// <returns>name of source file or null</returns>
        public string Filename()
        {
            TryGetString("Source.File", out string result);
            // If this attribute cannot be found, result will have the standard value
            // for strings, which is null.
            return result;
        }

        /// <summary>
        /// Returns the absolute path of the file declaring this graph element
        /// by concatenating the <see cref="Graph.BasePath"/> of the graph containing
        /// this graph element and the path and filename attributes of this graph
        /// element.
        ///
        /// The result will be in the platform-specific syntax for filenames.
        /// </summary>
        /// <returns>platform-specific absolute path</returns>
        public string AbsolutePlatformPath()
        {
            return System.IO.Path.Combine(ItsGraph.BasePath,
                                          Filenames.OnCurrentPlatform(Path()),
                                          Filenames.OnCurrentPlatform(Filename()));
        }

        /// <summary>
        /// Returns the line in the source file declaring this graph element.
        /// Note that not all graph elements may have a source location.
        /// If the graph element does not have this attribute, null is returned.
        /// </summary>
        /// <returns>line in source file or null</returns>
        public int? SourceLine()
        {
            if (TryGetInt("Source.Line", out int result))
            {
                return result;
            }
            // If this attribute cannot be found, result will be null
            return null;
        }

        /// <summary>
        /// Returns the length of this graph element, measured in number of lines.
        /// Note that not all graph elements may have a length.
        /// If the graph element does not have this attribute, null is returned.
        /// </summary>
        /// <returns>number of lines of the element in source file or null</returns>
        public int? SourceLength()
        {
            if (TryGetInt("Source.Region_Length", out int result))
            {
                return result;
            }
            // If this attribute cannot be found, result will be null
            return null;
        }

        /// <summary>
        /// Returns the column in the source file declaring this graph element.
        /// Note that not all graph elements may have a source location.
        /// If the graph element does not have this attribute, null is returned.
        /// </summary>
        /// <returns>column in source file or null</returns>
        public int? SourceColumn()
        {
            if (TryGetInt("Source.Column", out int result))
            {
                return result;
            }
            // If this attribute cannot be found, result will be null.
            return null;
        }

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
        /// Returns a short string representation of this graph element, intended for user-facing output.
        /// </summary>
        /// <returns>short string representation of graph element</returns>
        public abstract string ToShortString();

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