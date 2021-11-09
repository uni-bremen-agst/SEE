namespace SEE.DataModel.DG
{
    /// <summary>
    /// A memento for an edge.
    /// </summary>
    public class EdgeMemento : GraphElementsMemento
    {
        /// <summary>
        /// Constructor setting the edge to be memorized.
        /// </summary>
        /// <param name="edge">edge to be memorized</param>
        public EdgeMemento(Edge edge) : base(edge.ItsGraph)
        {
            this.edge = edge;
        }

        /// <summary>
        /// Memorized edge.
        /// </summary>
        private readonly Edge edge;

        /// <summary>
        /// Re-adds the memorized edge to <see cref="ItsGraph"/>.
        /// </summary>
        public override void Restore()
        {
            ItsGraph.AddEdge(edge);
        }
    }
}