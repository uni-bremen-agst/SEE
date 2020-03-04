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

//using SEE.Animation.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.DataModel
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
        /// Loads all GXL files (limited to <paramref name="maxRevisionsToLoad"/> many 
        /// files) from <paramref name="directory"/> and and saves all loaded graph data.
        /// </summary>
        /// <param name="directory">the directory path where the GXL file are located in</param>
        /// <param name="hierarchicalEdgeTypes">the set of edge-type names for edges considered to represent nesting</param>
        /// <param name="maxRevisionsToLoad">the upper limit of files to be loaded</param>
        public void Load(string directory, HashSet<string> hierarchicalEdgeTypes, int maxRevisionsToLoad)
        {
            graphs.Clear();
            if (String.IsNullOrEmpty(directory))
            {
                throw new Exception("Directory not set.");
            }
            SEE.Performance p = SEE.Performance.Begin("Loading GXL files from " + directory);

            // get all gxl files sorted by numbers in their name
            IEnumerable<string> sortedGraphNames = Directory
                .GetFiles(directory, "*.gxl", SearchOption.TopDirectoryOnly)
                .Where(e => !string.IsNullOrEmpty(e));

            if (sortedGraphNames.Count<string>() == 0)
            {
                throw new Exception("Directory '" + directory + "' has no GXL files.");
            }
            sortedGraphNames = sortedGraphNames.Distinct().NumericalSort();

            // for all found gxl files load and save the graph data
            foreach (string gxlPath in sortedGraphNames)
            {
                // load graph
                GraphReader graphCreator = new GraphReader(gxlPath, hierarchicalEdgeTypes, "ROOT", new SEELogger());
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

    /// <summary>
    /// Extension for IEnumerable<string>, that sorts by numbers in the string.
    /// For example {a-1, a-11, a-2} becomes {a-1, a-2, a-11}.
    /// </summary>
    internal static class NumericalSortExtension
    {
        /// <summary>
        /// Sorts the given IEnumerable<string> by numbers contained in the string.
        /// For example {a-1, a-11, a-2} becomes {a-1, a-2, a-11}.
        /// </summary>
        /// <param name="list">An IEnumerable<string> to be sorted</param>
        /// <returns>The passed list sorted by numbers</returns>
        public static IEnumerable<string> NumericalSort(this IEnumerable<string> list)
        {
            int maxLen = list.Select(s => s.Length).Max();

            return list.Select(s => new
            {
                OrgStr = s,
                SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'))
            })
            .OrderBy(x => x.SortStr)
            .Select(x => x.OrgStr);
        }
    }
}