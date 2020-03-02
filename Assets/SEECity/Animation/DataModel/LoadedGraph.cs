//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.DataModel;

namespace SEE.Animation.Internal
{
    /// <summary>
    /// DataModel containig all data generated for a graph loaded from a gxl-file
    /// and the layout data
    /// </summary>
    public class LoadedGraph
    {
        private readonly Graph graph;
        private readonly Layout layout;
        private readonly SEECityEvolution graphSettings;

        /// <summary>
        /// The loaded graph.
        /// </summary>
        public Graph Graph => graph;

        /// <summary>
        /// The calculated layout of the loaded graph.
        /// </summary>
        public Layout Layout => layout;

        /// <summary>
        /// The settings used for the loaded graph.
        /// </summary>
        public SEECityEvolution Settings => graphSettings;

        /// <summary>
        /// Creates a new LoadedGraph.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="layout"></param>
        /// <param name="graphSettings"></param>
        public LoadedGraph(Graph graph, Layout layout, SEECityEvolution graphSettings)
        {
            this.graph = graph.AssertNotNull("graph");
            this.layout = layout.AssertNotNull("layout");
            this.graphSettings = graphSettings.AssertNotNull("graphSettings");
        }
    }
}