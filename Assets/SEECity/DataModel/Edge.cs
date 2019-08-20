using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// Directed and typed edges of the graph with source and target node.
    /// </summary>
    [System.Serializable]
    public class Edge : GraphElement
    {
        [SerializeField]
        private Node source;

        // Important note: Edges should be created only by calling Graph.newEdge().
        // Do not use 'new Edge()'.

        public Node Source
        {
            get => source;
            set => source = value;
        }

        [SerializeField]
        private Node target;

        public Node Target
        {
            get => target;
            set => target = value;
        }

        public override string ToString()
        {
            string result = "{\n";
            result += " \"kind\": edge,\n";
            result += " \"source\":  \"" + source.LinkName + "\",\n";
            result += " \"target\": \"" + target.LinkName + "\",\n";
            result += base.ToString();
            result += "}";
            return result;
        }
    }
}