﻿//Copyright 2020 Florian Garbade

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

using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.Evolution;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// A SEECityEvolution combines all necessary components for the animations
    /// of an evolving SEECity.
    /// </summary>
    public class SEECityEvolution : AbstractSEECity
    {
        /// <summary>
        /// Sets the maximum number of revsions to load.
        /// </summary>
        public int maxRevisionsToLoad = 500;  // serialized by Unity

        /// <summary>
        /// The renderer for rendering the evolution of the graph series.
        /// </summary>
        private EvolutionRenderer evolutionRenderer;  // not serialized by Unity; will be set in Start()

        /// <summary>
        /// The height of posts used as markers for new and deleted elements.
        /// </summary>
        [Tooltip("The height of posts used as markers for new and deleted elements (>=0).")]
        public float MarkerHeight = 0.2f;

        /// <summary>
        /// The width (x and z lengths) of posts used as markers for new and deleted elements.
        /// </summary>
        [Tooltip("The width (x and z lengths) of posts used as markers for new and deleted elements (>=0).")]
        public float MarkerWidth = 0.01f;
        
        /// <summary>
        /// Factory method to create the used EvolutionRenderer.
        /// </summary>
        /// <returns></returns>
        protected EvolutionRenderer CreateEvolutionRenderer()
        {
            if (!gameObject.TryGetComponent<EvolutionRenderer>(out EvolutionRenderer result))
            {
                result = gameObject.AddComponent<EvolutionRenderer>();
            }
            result.CityEvolution = this;
            return result;
        }

        /// <summary>
        /// Loads the graph data from the GXL files and the metrics from the CSV files contained 
        /// in the directory with path PathPrefix and the metrics.
        /// </summary>
        private List<Graph> LoadData()
        {
            GraphsReader graphsReader = new GraphsReader();
            // Load all GXL graphs and CSV files in directory PathPrefix but not more than maxRevisionsToLoad many.
            graphsReader.Load(PathPrefix, HierarchicalEdges, maxRevisionsToLoad);
            return graphsReader.graphs;
        }

        /// <summary>
        /// Yields the graph of the first GXL found in the directory named <code>PathPrefix</code>.
        /// The order is ascending and alphabetic by the GXL filenames located in that directory.
        /// If the first GXL file has a corresponding CSV with additional metrics, this CSV file
        /// will be read, too, and the node metrics added to the graph.
        /// 
        /// Precondition: PathPrefix must be set and denote an existing directory in the
        /// file system containing at least one GXL file.
        /// </summary>
        /// <returns>the loaded graph or null if none could be found</returns>
        public Graph LoadFirstGraph()
        {
            GraphsReader reader = new GraphsReader();
            reader.Load(PathPrefix, HierarchicalEdges, 1);


             

            List<Graph> graphs = reader.graphs;
            if (graphs.Count == 0)
            {
                return null;
            }
            else
            {
                Graph graph = graphs.First<Graph>();
                
                
                graph = RelevantGraph(graph);
                LoadDataForGraphListing(graph);
                return graph;
            }
        }

        /// <summary>
        /// Draws the given <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph to be drawn</param>
        public void DrawGraph(Graph graph)
        {
           
            DrawGraphs(new List<Graph>() { graph });
        }

        /// <summary>
        /// Called by Unity when this SEECityEvolution instances comes into existence 
        /// and can enter the game for the first time. Loads all graphs, calculates their
        /// layouts, and displays the first graph in the graph series.
        /// </summary>
        private void Awake()
        {
            DrawGraphs(LoadData());
            // We assume this SEECityEvolution instance is a component of a game object
            // to which an AnimationInteraction component is attached. This AniminationInteraction
            // component must know the evolution renderer.
            {
                AnimationInteraction animationInteraction = gameObject.GetComponent<AnimationInteraction>();
                if (animationInteraction == null)
                {
                    Debug.LogErrorFormat("The game object {0} this SEECityEvolution component is attached to must have a component AnimationInteraction attached to it, too.", gameObject.name);
                }
                else
                {
                    animationInteraction.EvolutionRenderer = evolutionRenderer;
                }
            }
        }

        /// <summary>
        /// Creates <see cref="evolutionRenderer"/> and shows the complete graph
        /// evolution for given <paramref name="graphs"/> using it.
        /// </summary>
        /// <param name="graphs">the series of graph to be drawn</param>
        private void DrawGraphs(List<Graph> graphs)
        {   
            foreach(Graph g in graphs)
            {
                Graph graph= RelevantGraph(g);
                LoadDataForGraphListing(graph);
            }

            evolutionRenderer = CreateEvolutionRenderer();
            evolutionRenderer.ShowGraphEvolution(graphs);
        }
    }
}