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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.Animation.Internal
{
    /// <summary>
    /// Allows loading of multiple gxl files from a directory and stores them.
    /// !!! At the moment data for Metric.Clone_Rate is copied from Metric.LOC for better visualisation during test !!!
    /// </summary>
    public class Loader
    {
        /// <summary>
        /// Contains all loaded graphs after calling LoadGraphData().
        /// </summary>
        public readonly List<Graph> graphs = new List<Graph>();

        /// <summary>
        /// Loads all gxl files from GraphSettings.AnimatedPath() sorted by numbers in the file names.
        /// </summary>
        /// <param name="graphSettings">The GraphSettings defining the location of gxl files.</param>
        public void LoadGraphData(SEECity graphSettings, int maxRevisionsToLoad)
        {
            graphSettings.AssertNotNull("graphSettings");
            graphs.Clear();
            AddAllRevisions(graphSettings, maxRevisionsToLoad);
        }

        /// <summary>
        /// Internal function that loads all gxl files from the path set in GraphSettings and
        /// and saves all loaded graph data.
        /// </summary>
        /// <param name="graphSettings">The GraphSettings defining the location of gxl files.</param>
        private void AddAllRevisions(SEECity graphSettings, int maxRevisionsToLoad)
        {
            SEE.Performance p = SEE.Performance.Begin("loading animated graph data from " + graphSettings.GXLPath());

            // clear possible old data
            graphs.Clear();

            // get all gxl files sorted by numbers in their name
            var sortedGraphNames = Directory
                .GetFiles(graphSettings.GXLPath(), "*.gxl", SearchOption.TopDirectoryOnly)
                .Where(e => !string.IsNullOrEmpty(e))
                .Distinct()
                .NumericalSort();

            // for all found gxl files load and save the graph data
            foreach (string gxlPath in sortedGraphNames)
            {
                // load graph
                GraphReader graphCreator = new GraphReader(gxlPath, graphSettings.HierarchicalEdges, new SEELogger());
                graphCreator.Load();
                Graph graph = graphCreator.GetGraph();

                // TODO remove if Clone_Rate is properly represented in gxl-files
                graph.Traverse(leafNode => leafNode.SetFloat("Metric.Clone_Rate", leafNode.GetInt("Metric.LOC")));

                // if graph was loaded put in graph list
                if (graph == null)
                {
                    Debug.LogError("graph " + gxlPath + " could not be loaded.");
                }
                else
                {
                    graphs.Add(graph);
                }
                maxRevisionsToLoad--;
                if (maxRevisionsToLoad <= 0)
                {
                    break;
                }
            }

            p.End();
            Debug.Log("Number of graphs loaded: " + graphs.Count + "\n");
        }
    }
}