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

using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE.DataModel.DG.IO;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Loads and stores multiple GXL files from a directory.
    /// </summary>
    public class GraphsReader
    {
        /// <summary>
        /// Contains all loaded graphs after calling Load().
        /// </summary>
        public readonly List<Graph> graphs = new List<Graph>();
        /// <summary>
        /// Contains a List of CSV files matching the GXL filenames.
        /// </summary>
        /// 
        public List<string> csvFileNames = new List<string>();
        /// <summary>
        /// Loads all GXL files (limited to <paramref name="maxRevisionsToLoad"/> many 
        /// files) from <paramref name="directory"/> and and saves all loaded graph data.
        /// </summary>
        /// <param name="directory">the directory path where the GXL file are located in</param>
        /// <param name="hierarchicalEdgeTypes">the set of edge-type names for edges considered to represent nesting</param>
        /// <param name="maxRevisionsToLoad">the upper limit of files to be loaded</param>
        public void Load(string directory, HashSet<string> hierarchicalEdgeTypes, int maxRevisionsToLoad)
        {
            IEnumerable<string> sortedGraphNames = Filenames.GXLFilenames(directory);
            IEnumerable<string> sortedCSVNames = Filenames.CSVFilenames(directory);
            if (sortedGraphNames.Count<string>() == 0)
            {
                throw new Exception("Directory '" + directory + "' has no GXL files.");
            }
            graphs.Clear();
            SEE.Utils.Performance p = SEE.Utils.Performance.Begin("Loading GXL files from " + directory);
            // for all found GXL files load and save the graph data
            foreach (string gxlPath in sortedGraphNames)
            {
                // load graph
                GraphReader graphCreator = new GraphReader(gxlPath, hierarchicalEdgeTypes, gxlPath, new SEELogger());
                graphCreator.Load();
                Graph graph = graphCreator.GetGraph();

                // if graph was loaded put in graph list
                if (graph == null)
                {
                    Debug.LogError("graph " + gxlPath + " could not be loaded.\n");
                }
                else
                {
                    foreach (string s in sortedCSVNames)
                    {
                        
                            if (s.Substring(0, s.Length - 3).Equals(gxlPath.Substring(0,gxlPath.Length - 3)))
                        
                        {
                            // load the CSV into the graph assuming ";" is the delmiter in every CSV


                            if (MetricImporter.Load(graph, s, ';') > 0)
                            {
                                Debug.LogErrorFormat("CSV file {0} has {1} many errors.\n", filename, numberOfErrors);
                            }


                        }
                        
                    }
                    maxRevisionsToLoad--;         
                    graphs.Add(graph);
                }
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