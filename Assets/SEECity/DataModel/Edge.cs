namespace SEE
{
    /// <summary>
    /// Implements IEdge.
    /// </summary>
    class Edge : GraphElement, IEdge
    {
        private INode source;

        // Important note: Edges should be created only by calling IGraph.newEdge().
        // Do not use 'new Edge()'.

        public INode Source
        {
            get => source;
            set => source = value;
        }

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