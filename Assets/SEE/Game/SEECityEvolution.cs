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

using System;
using System.Collections.Generic;
using UnityEngine;

using SEE.DataModel;
using SEE.Game.Animation;
using SEE.DataModel.IO;
using SEE.Utils;

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
        /// Factory method to create the used EvolutionRenderer.
        /// </summary>
        /// <returns></returns>
        protected EvolutionRenderer CreateEvolutionRenderer()
        {
            // FIXME: Do we really need to attach the evolution renderer as a component to
            // the game object? That was likely done because EvolutionRenderer derives from
            // MonoBehaviour and MonoBehaviours cannot be created by the new operator.
            EvolutionRenderer result = gameObject.AddComponent<EvolutionRenderer>();
            result.CityEvolution = this;
            return result;
        }

        /// <summary>
        /// Loads the graph data from the GXL files and the metrics from the CSV files contained 
        /// in the directory with path PathPrefix and the metrics.
        /// </summary>
        private List<Graph> LoadData()
        {
            if (String.IsNullOrEmpty(PathPrefix))
            {
                PathPrefix = UnityProject.GetPath() + "..\\Data\\GXL\\animation-clones\\";
                Debug.LogErrorFormat("Path prefix not set. Using default: {0}.\n", PathPrefix);
            }
            GraphsReader graphsReader = new GraphsReader();
            // Load all GXL graphs in directory PathPrefix but not more than maxRevisionsToLoad many.
            graphsReader.Load(this.PathPrefix, this.HierarchicalEdges, maxRevisionsToLoad);

            // TODO: The CSV metric files should be loaded, too.

            return graphsReader.graphs;
        }

        /// <summary>
        /// Yields the graph of the first GXL found in the directory named <code>PathPrefix</code>.
        /// The order is ascending and alphabetic by the GXL filenames located in that directory.
        /// 
        /// Precondition: PathPrefix must be set and denote an existing directory in the
        /// file system containing at least one GXL file.
        /// </summary>
        /// <returns>the loaded graph or null if none could be found</returns>
        public Graph LoadFirstGraph()
        {
            if (String.IsNullOrEmpty(PathPrefix))
            {
                PathPrefix = UnityProject.GetPath() + "..\\Data\\GXL\\animation-clones\\";
                Debug.LogErrorFormat("Path prefix not set. Using default: {0}.\n", PathPrefix);
            }
            GraphReader graphReader = new GraphReader(FirstFilename(this.PathPrefix), this.HierarchicalEdges);
            graphReader.Load();
            return graphReader.GetGraph();
        }

        /// <summary>
        /// Yields the first name of a GXL file in the sorted list of GXL files located
        /// in the given <paramref name="directory"/>.
        /// 
        /// If <paramref name="directory"/> does not contain any GXL file, an exception is
        /// thrown.
        /// </summary>
        /// <param name="directory">directory in which to look up the first GXL file</param>
        /// <returns>first filename</returns>
        private string FirstFilename(string directory)
        {
            foreach (string filename in GraphsReader.GXLFilenames(directory))
            {
                return filename;
            }
            throw new Exception("No GXL files found in " + directory);
        }

        /// <summary>
        /// Called by Unity when this SEECityEvolution instances comes into existence 
        /// and can enter the game for the first time. Loads all graphs, calculates their
        /// layouts, and displays the first graph in the graph series.
        /// </summary>
        void Start()
        {
            evolutionRenderer = CreateEvolutionRenderer();
            evolutionRenderer.AssertNotNull("renderer");
            evolutionRenderer.ShowGraphEvolution(LoadData());
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
    }
}