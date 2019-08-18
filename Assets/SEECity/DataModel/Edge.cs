using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// Implements IEdge.
    /// </summary>
    [System.Serializable]
    class Edge : GraphElement, IEdge
    {
        [SerializeField]
        private INode source;

        // Important note: Edges should be created only by calling IGraph.newEdge().
        // Do not use 'new Edge()'.

        public INode Source
        {
            get => source;
            set => source = value;
        }

        [SerializeField]
        private INode target;

        public INode Target
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