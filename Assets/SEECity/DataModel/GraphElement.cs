using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// A type graph element. Either a node or an edge.
    /// </summary>
    [System.Serializable]
    public abstract class GraphElement : Attributable
    {
        /// <summary>
        /// The type of the graph element.
        /// </summary>
        [SerializeField]
        private string type;

        /// <summary>
        /// The graph this graph element is contained in. May be null if
        /// the element is currently not in a graph.
        /// 
        /// IMPORTANT NOTE: This attribute will not be serialized. It may
        /// be null at run-time or in the editor inspection view.
        /// </summary>
        [System.NonSerialized]
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
        [SerializeField]
        public string Type
        {
            get
            {
                return type;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    type = value;
                }
                else
                {
                    type = "Unknown";
                }
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
        public bool Has_Supertype_Of(string type)
        {
            // FIXME: We currently do not have the type hierarchy, so we cannot know
            // which type subsumes which other type. For the time being, we insist that
            // the types must be the same.
            return this.type == type;
        }

        public override string ToString()
        {
            return " \"type\": " + type + "\",\n" + base.ToString();
        }

        /// <summary>
        /// Creates deep copies of attributes where necessary. Is called by
        /// Clone() once the copy is created. Must be extended by every 
        /// subclass that adds fields that should be cloned, too.
        /// </summary>
        /// <param name="clone">the clone receiving the copied attributes</param>
        protected override void HandleCloned(object clone)
        {
            base.HandleCloned(clone);
            GraphElement target = (GraphElement)clone;
            target.type = this.type;
        }
    }
}