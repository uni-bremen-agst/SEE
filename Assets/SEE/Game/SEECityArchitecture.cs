using System;
using System.Collections.Generic;
using System.IO;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.Architecture;
using SEE.GO;
using SEE.Layout.IO;
using SEE.Utils;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    
    /// <summary>
    /// Manages settings for modelling software architectures for the reflexion analysis.
    /// </summary>
    public class SEECityArchitecture : SEECity
    {


        public new ArchitectureRenderer Renderer
        {
            get
            {
                if (architectureRenderer == null)
                {
                    architectureRenderer = new ArchitectureRenderer(this, LoadedGraph);
                }

                return architectureRenderer;
            }
        }


        private new ArchitectureRenderer architectureRenderer;
        
        /// <summary>
        /// Path to the layout file of the architecture.
        /// </summary>
        [Tooltip("The path to the layout file used for this architectural graph visualization")]
        public DataPath ArchitectureLayoutPath = new DataPath();


        /// <summary>
        /// Settings holders for the architecture elements.
        /// </summary>
        public readonly ArchitectureElementSettings[] ArchitectureElementSettings =
            ArrayUtils.New((int) ArchitectureElementType.Count, _ => new ArchitectureElementSettings());


        /// <summary>
        /// 
        /// </summary>
        public int NODE_COUNTER = 0;

        /// <summary>
        /// Re-renders the architecture graph without deleting the resetting the loaded graph.
        /// </summary>
        public void ReDrawGraph()
        {
            Assert.IsNotNull(LoadedGraph);
            DeleteGraphGameObjects();
            DrawGraph();
        }
        
        /// <summary>
        /// Loads the architecture graph from the GXL File with GXLPath.
        /// DrawGraph() must be used to actually render the graph afterwards.
        /// </summary>
        public void LoadGraph()
        {
            if (string.IsNullOrEmpty(GXLPath.Path))
            {
                Debug.LogError("Empty graph path.\n");
                return;
            }

            if (LoadedGraph != null)
            {
                Reset();
            }
            LoadedGraph = LoadGraph(GXLPath.Path);
        }

        /// <summary>
        /// Resets the loaded graph data and deletes all game objects related to that graph.
        /// </summary>
        public void ResetGraph()
        {
            base.Reset();
            LoadedGraph = null;
        }
        
        /// <summary>
        /// Saves the architecture graph to the GXL file with GXLPath.
        /// </summary>
        public void SaveGraph()
        {
            if (string.IsNullOrEmpty(GXLPath.Path))
            {
                Debug.LogError("Empty graph path.\n");
                return;
            }

            if (LoadedGraph != null)
            {
                foreach (string hierarchicalEdge in HierarchicalEdges)
                {
                    GraphWriter.Save(GXLPath.Path, LoadedGraph, hierarchicalEdge);
                    break;
                }
            }
        }

        protected override void Awake()
        {
            string filename = GXLPath.Path;
            if (LoadedGraph != null)
            {
                Debug.Log("SEECityArchitecture.Awake: graph is already loaded.\n");
            }
            else if (!string.IsNullOrEmpty(filename))
            {
                LoadedGraph = LoadGraph(filename);
                
                // Make sure that the artificial root node is added again.
                ArchitectureRenderer renderer = Renderer;
                renderer.AddArtificialRootNode(LoadedGraph);
                if (LoadedGraph != null)
                {
                    SetNodeEdgeRefs(LoadedGraph, gameObject);
                }
                else
                {
                    Debug.LogError($"SEECityArchitecture.Awake: Could not laod GXL file {filename} of architecture {name}.\n");
                }
            }
            else
            {
                Debug.LogError($"SEECityArchitecture.Awake: GXL file of architecture {name} is undefined.\n");
            }
            RemoveTransparency();
        }

        /// <summary>
        /// Saves the layout of the current architecture graph.
        /// </summary>
        public void SaveLayout()
        {
            if (string.IsNullOrEmpty(ArchitectureLayoutPath.Path))
            {
                Debug.LogError("Architecture layout path is empty.\n");
                return;
            }

            if (Filenames.HasExtension(ArchitectureLayoutPath.Path, Filenames.GVLExtension))
            {
                GVLWriter.Save(ArchitectureLayoutPath.Path, LoadedGraph.Name, AllNodeDescendants(gameObject));
            }
            else
            {
                SLDWriter.Save(ArchitectureLayoutPath.Path, AllNodeDescendants(gameObject));
            }
        }

        /// <summary>
        /// Creates a new empty graph for architecture modelling.
        /// </summary>
        public void NewGraph()
        {
            Assert.IsNull(LoadedGraph);
            LoadedGraph = new Graph("Architecture");
            ArchitectureRenderer renderer = Renderer;
            renderer.PrepareNewArchitectureGraph(gameObject);
        }
        
        /// <summary>
        /// Renders the loaded architecture graph.
        /// </summary>
        public void DrawGraph()
        {
            Assert.IsNotNull(LoadedGraph);
            ArchitectureRenderer renderer = Renderer;
            renderer.Draw(gameObject);
        }
    }
}