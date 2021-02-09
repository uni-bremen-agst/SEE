using SEE.Game;
using SEE.GO;
using System;
using UnityEngine;

/// <summary>
/// Creates a new edge through the network on each client.
/// </summary>
public class AddEdgeNetAction : SEE.Net.AbstractAction
{
    /// <summary>
    /// The id of the gameObject from which the edge should be drawn.
    /// </summary>
    public string fromId;

    /// <summary>
    /// The id of the gameObject to which the edge should be drawn.
    /// </summary>
    public string toId;

    /// <summary>
    /// Constructs an AddEdgeNetAction.
    /// </summary>
    /// <param name="fromId">The id of the gameObject from which the edge should be drawn</param>
    /// <param name="toId">The id of the gameObject to which the edge should be drawn</param>
    public AddEdgeNetAction(string fromId, string toId)
    {
        this.fromId = fromId;
        this.toId = toId;
    }

    /// <summary>
    /// Stuff to execute on the Server. Nothing to be done here.
    /// </summary>
    protected override void ExecuteOnServer()
    {
        // Intentionally left blank.
    }

    /// <summary>
    /// Creates the new edge on each client.
    /// </summary>
    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
        {
            GameObject fromGO = GameObject.Find(fromId);
            if (fromGO)
            {
                GameObject toGO = GameObject.Find(toId);
                if (toGO)
                {
                    Transform codeCity = SceneQueries.GetCodeCity(fromGO.transform);
                    if (codeCity)
                    {
                        if (codeCity.gameObject.TryGetComponentOrLog(out SEECity city))
                        {
                            try
                            {
                                city.Renderer.DrawEdge(fromGO, toGO);
                            }

                            catch (Exception e)
                            {
                                Debug.LogError($"The new edge from {fromGO.name} to {toGO.name} could not be created: {e.Message}.\n");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Game node named {fromId} is not contained in a code city.\n");
                    }
                }
                else
                {
                    Debug.LogError($"There is no game node named {toId}.\n");
                }
            }
            else
            {
                Debug.LogError($"There is no game node named {fromId}.\n");
            }
        }
    }
}