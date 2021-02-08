using SEE.Game;
using System;
using UnityEngine;
/// <summary>
/// Creates a new edge throw the network on each client
/// </summary>
public class AddEdgeNetAction : SEE.Net.AbstractAction
{
    /// <summary>
    /// The id of the gameObject from which the edge should be drawn
    /// </summary>
    public string fromId;

    /// <summary>
    /// The id of the gameObject to which the edge should be drawn
    /// </summary>
    public string toId;


    /// <summary>
    /// Constructs a AddEdgeNetAction
    /// </summary>
    /// <param name="fromId">The id of the gameObject from which the edge should be drawn</param>
    /// <param name="toId">The id of the gameObject to which the edge should be drawn</param>
    public AddEdgeNetAction(string fromId, string toId)
    {
        this.fromId = fromId;
        this.toId = toId;
    }

    /// <summary>
    /// Stuff to execute on the Server
    /// </summary>
    protected override void ExecuteOnServer()
    {
    }
    /// <summary>
    /// Creates a new Edge on each client
    /// </summary>
    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
        {
            GameObject fromGO = GameObject.Find(fromId);
            GameObject toGO = GameObject.Find(toId);
            SEECity city = null;
            SceneQueries.GetCodeCity(fromGO.transform)?.gameObject.TryGetComponent(out city);
            if(city != null && toGO != null && fromGO != null)
            {
                try
                {
                    city.GetComponent<GraphRenderer>().DrawEdge(fromGO, toGO);
                }
                
                catch (Exception e)
                {
                    Debug.LogError($"The new edge from {fromGO.name} to {GameObject.Find(toId).name} could not be created: {e.Message}.\n");
                }
            }
        }
    }

}