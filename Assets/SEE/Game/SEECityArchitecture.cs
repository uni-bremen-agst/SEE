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
    public class SEECityArchitecture : AbstractSEECity
    {

        
        /// <summary>
        /// The loaded graph instance.
        /// </summary>
        public Graph LoadedGraph;



        private ArchitectureRenderer _renderer;
        
        
        /// <summary>
        /// Path to the layout file of the architecture.
        /// </summary>
        [Tooltip("The path to the layout file used for this architectural graph visualization")]
        public DataPath ArchitectureLayoutPath = new DataPath();
        
        /// <summary>
        /// Path to the graph gxl file of the architecture.
        /// </summary>
        [Tooltip("The path to the gxl graph file used for this architecture graph.")]
        public DataPath GXLPath = new DataPath();



        /// <summary>
        /// Settings holders for the architecture elements.
        /// </summary>
        public ArchitectureElementSettings[] ArchitectureElementSettings =
            ArrayUtils.New((int) ArchitectureElementType.Count, _ => new ArchitectureElementSettings());

        

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
            //LoadedGraph?.Destroy();
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
        /// Sets all EdgeRefs and NodeRefs for the loaded architecture graph to the nodes and edges.
        /// We assume that the game objects with a NodeRef/EdgeRef required to be
        /// defined to be immediate children of this SEECityArchitecture. Moreover, we assume a child
        /// game object's name is the ID of the corresponding graph node/edge.
        /// </summary>
        public void AddReferences()
        {
            if (LoadedGraph != null)
            {
                AddNodeEdgeRefs(gameObject);
                Debug.LogFormat("Node and edge references for {0} are resolved.\n", gameObject.name);
            }
            else
            {
                Debug.LogError("No graph loaded.\n");
            }
            
        }

        private void AddNodeEdgeRefs(GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                GameObject go = child.gameObject;
                if (go.TryGetNodeRef(out NodeRef nodeRef))
                {
                    nodeRef.Value = LoadedGraph.GetNode(go.name);
                    if (nodeRef.Value == null)
                    {
                        Debug.LogWarningFormat("Could not resolve node reference {0}.\n", go.name);
                    }
                }else if (go.TryGetEdgeRef(out EdgeRef edgeRef))
                {
                    edgeRef.Value = LoadedGraph.GetEdge(go.name);
                    if (edgeRef.Value == null)
                    {
                        Debug.LogWarningFormat("Could not resolve edge reference {0}.\n", go.name);
                    }
#if UNITY_EDITOR
                    else if (go.CompareTag(Tags.Node) || go.CompareTag(Tags.Edge))
                    {
                        Debug.LogWarningFormat("Game object {0} has neither node nor edge reference.\n", go.name);
                    }
#endif
                    AddNodeEdgeRefs(go);
                }
            }
        }
        
        /// <summary>
        /// Creates a new empty graph for architecture modelling.
        /// </summary>
        public void NewGraph()
        {
            Assert.IsNull(LoadedGraph);
            LoadedGraph = new Graph("Architecture");
            _renderer = new ArchitectureRenderer(this, LoadedGraph);
            _renderer.PrepareNewArchitectureGraph(gameObject);
        }
        
        /// <summary>
        /// Renders the loaded architecture graph.
        /// </summary>
        public void DrawGraph()
        {
            Assert.IsNotNull(LoadedGraph);
            _renderer = new ArchitectureRenderer(this, LoadedGraph);
            _renderer.Draw(gameObject);
        }
    }
}