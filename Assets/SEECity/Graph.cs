using System;
using System.Collections.Generic;
using UnityEngine;

// A graph with nodes and edges (both derived from GameObject).
public class Graph
{
    // The list of graph nodes indexed by their unique linkname
    private Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();
    //private List<GameObject> nodes = new List<GameObject>();

    // The list of graph edges.
    private List<GameObject> edges = new List<GameObject>();

    // Deletes the edges of the graph.
    public void DeleteEdges()
    {
        DeleteGameObjects(edges);
        edges.Clear();
    }

    // Deletes the nodes of the graph.
    public void DeleteNodes()
    {
        foreach (KeyValuePair<string, GameObject> entry in nodes)
        {
            UnityEngine.Object.DestroyImmediate(entry.Value);
        }
        nodes.Clear();
    }

    // Deletes all nodes and edges of the graph.
    public void Delete()
    {
        DeleteNodes();
        DeleteEdges();
    }

    // Deletes all given objects immediately.
    private void DeleteGameObjects(List<GameObject> objects)
    {
        foreach (GameObject o in objects)
        {
            UnityEngine.Object.DestroyImmediate(o);
        }
    }

    // Adds an edge to the graph.
    public void AddEdge(GameObject edge)
    {
        edges.Add(edge);
    }

    // Adds a node to the graph.
    public void AddNode(string nodeID, GameObject node)
    {
        nodes.Add(nodeID, node);
    }

    // The number of nodes.
    public int NodeCount()
    {
        return nodes.Count;
    }

    public GameObject GetNode(string nodeID)
    {
        GameObject result;
        if (!nodes.TryGetValue(nodeID, out result))
        {
            throw new Exception("Unknown node id " + nodeID);
        }
        return result;
    }
}
