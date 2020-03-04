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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.Animation.Internal
{
    // TODO: Move this code to GraphLoader.

    /// <summary>
    /// Loads and stores multiple GXL files from a directory.
    /// </summary>
    public class Loader
    {
        /// <summary>
        /// Contains all loaded graphs after calling LoadGraphData().
        /// </summary>
        public readonly List<Graph> graphs = new List<Graph>();

        /// <summary>
        /// Loads all GXL files from cityEvolution.PathPrefix sorted by the
        /// numbers in the file names.
        /// </summary>
        /// <param name="cityEvolution">The city evolution defining the location of gxl files.</param>
        public void LoadGraphData(SEECityEvolution cityEvolution, int maxRevisionsToLoad)
        {
            cityEvolution.AssertNotNull("graphSettings");
            graphs.Clear();
            AddAllRevisions(cityEvolution, maxRevisionsToLoad);
        }

        /// <summary>
        /// Internal function that loads all gxl files from the path set in GraphSettings and
        /// and saves all loaded graph data.
        /// </summary>
        /// <param name="cityEvolution">The GraphSettings defining the location of gxl files.</param>
        private void AddAllRevisions(SEECityEvolution cityEvolution, int maxRevisionsToLoad)
        {
            if (String.IsNullOrEmpty(cityEvolution.PathPrefix))
            {
                throw new Exception("Path prefix not set.");
            }
            Debug.LogFormat("Loading animated graph data from {0}.\n", cityEvolution.PathPrefix);
            SEE.Performance p = SEE.Performance.Begin("Loading animated graph data from " + cityEvolution.PathPrefix);

            // clear possible old data
            graphs.Clear();

            // get all gxl files sorted by numbers in their name
            IEnumerable<string> sortedGraphNames = Directory
                .GetFiles(cityEvolution.PathPrefix, "*.gxl", SearchOption.TopDirectoryOnly)
                .Where(e => !string.IsNullOrEmpty(e));

            if (sortedGraphNames.Count<string>() == 0)
            {
                throw new Exception("Directory '" + cityEvolution.PathPrefix + "' has no GXL files.");
            }
            sortedGraphNames = sortedGraphNames.Distinct().NumericalSort();

            // for all found gxl files load and save the graph data
            foreach (string gxlPath in sortedGraphNames)
            {
                // load graph
                GraphReader graphCreator = new GraphReader(gxlPath, cityEvolution.HierarchicalEdges, "ROOT", new SEELogger());
                graphCreator.Load();
                Graph graph = graphCreator.GetGraph();

                // if graph was loaded put in graph list
                if (graph == null)
                {
                    Debug.LogError("graph " + gxlPath + " could not be loaded.\n");
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