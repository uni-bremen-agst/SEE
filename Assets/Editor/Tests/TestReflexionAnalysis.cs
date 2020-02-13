using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// Shared code of unit tests for ReflexionAnalysis.
    /// </summary>
    internal class TestReflexionAnalysis : Observer
    {
        protected const string call = "call";
        protected Graph impl;
        protected Graph arch;
        protected Graph mapping;
        protected Reflexion reflexion;
        protected SEELogger logger = new SEE.SEELogger();

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        protected HashSet<string> HierarchicalEdges;

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        protected static HashSet<string> Hierarchical_Edge_Types()
        {
            HashSet<string> result = new HashSet<string>
            {
                "Enclosing",
                "Belongs_To",
                "Part_Of",
                "Defined_In"
            };
            return result;
        }

        /// <summary>
        /// List of edges changed for a single reflexion-analysis run.
        /// </summary>
        protected List<EdgeChange> edgeChanges = new List<EdgeChange>();
        /// <summary>
        /// List of edges propagated from the implementation onto the architecture for 
        /// a single reflexion-analysis run.
        /// </summary>
        protected List<PropagatedEdge> propagatedEdges = new List<PropagatedEdge>();
        /// <summary>
        /// List of edges removed for a single reflexion-analysis run.
        /// </summary>
        protected List<RemovedEdge> removedEdges = new List<RemovedEdge>();

        /// <summary>
        /// Re-sets the event chaches edgeChanges, propagatedEdges, and removedEdges to
        /// to their initial value (empty).
        /// </summary>
        protected void ResetEvents()
        {
            edgeChanges     = new List<EdgeChange>();
            propagatedEdges = new List<PropagatedEdge>();
            removedEdges    = new List<RemovedEdge>();
        }

        /// <summary>
        /// Dumps the results collected in edgeChanges, propagatedEdges, and removedEdges
        /// to standard output.
        /// </summary>
        protected void DumpEvents()
        {
            Debug.Log("DEPENDENCIES PROPAGATED TO ARCHITECTURE\n");
            foreach (PropagatedEdge e in propagatedEdges)
            {
                Debug.LogFormat("propagated {0}\n", e.propagatedEdge.ToString());
            }
            Debug.Log("DEPENDENCIES CHANGED IN ARCHITECTURE\n");
            foreach (EdgeChange e in edgeChanges)
            {
                Debug.LogFormat("changed {0} from {1} to {2}\n", e.edge.ToString(), e.oldState, e.newState);
            }
            Debug.Log("DEPENDENCIES REMOVED FROM ARCHITECTURE\n");
            foreach (RemovedEdge e in removedEdges)
            {
                Debug.LogFormat("removed {0}\n", e.edge.ToString());
            }
        }

        /// <summary>
        /// Creates HierarchicalEdges and re-sets all event caches.
        /// </summary>
        [SetUp]
        protected virtual void Setup()
        {
            HierarchicalEdges = Hierarchical_Edge_Types();
            ResetEvents();
        }

        /// <summary>
        /// Resets all attributes to null.
        /// </summary>
        [TearDown]
        protected virtual void Teardown()
        {
            impl = null;
            arch = null;
            mapping = null;
            reflexion = null;
            HierarchicalEdges = null;
            logger = null;
            edgeChanges = null;
            propagatedEdges = null;
            removedEdges = null;
        }

        protected static Node NewNode(Graph graph, string linkname, string type = "Routine")
        {
            Node result = new Node();
            result.LinkName = linkname;
            result.SourceName = linkname;
            result.Type = type;
            graph.AddNode(result);
            return result;
        }

        protected static Edge NewEdge(Graph graph, Node from, Node to, string type)
        {
            Edge result = new Edge();
            result.Type = type;
            result.Source = from;
            result.Target = to;
            graph.AddEdge(result);
            return result;
        }

        /// <summary>
        /// Callback of reflexion analysis. Will be called by reflexion analysis on every
        /// state change. Collects the events in the respective change-event lists
        /// edgeChanges, propagatedEdges, removedEdges.
        /// </summary>
        /// <param name="changeEvent">the event that occurred</param>
        public virtual void Update(ChangeEvent changeEvent)
        {
            if (changeEvent is EdgeChange)
            {
                edgeChanges.Add(changeEvent as EdgeChange);
            }
            else if (changeEvent is PropagatedEdge)
            {
                propagatedEdges.Add(changeEvent as PropagatedEdge);
            }
            else if (changeEvent is RemovedEdge)
            {
                removedEdges.Add(changeEvent as RemovedEdge);
            }
            else
            {
                Debug.LogErrorFormat("UNHANDLED CALLBACK: {0}\n", changeEvent.ToString());
            }
        }
    }
}