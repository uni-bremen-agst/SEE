using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.Game;
using SEE.GO;
using System;

public class AddEdge : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        //Assigning the gameobjects to be connected.
        //Checking whether the two gameobjects are not null and whether the are on the same graph.
        if (Input.GetMouseButtonDown(0) && hoveredObject != null)
        {
            if (from == null)
            {
                from = hoveredObject;
            }
            if (!from.Equals(hoveredObject) && to == null)
            {
                    to = hoveredObject;
            }
            else
            {
                throw new Exception($"The source and target of the edge cannot be the same gameobject!");
            }
        }
        if (from != null && to != null)
        {
                SEECity city;
                city = SceneQueries.GetCodeCity(from.transform).GetComponent<SEECity>();
                city.Renderer.DrawEdge(from, to);
                from = null;
                to = null;
        }
        //Adding the key "F1" in order to delete the first selected gameobject.
        if (Input.GetKeyDown(KeyCode.F1))
        {
            from = null;
        }

    }

    /// <summary>
    /// Var to get the life hovered object.
    /// </summary>
    public GameObject hoveredObject;

    /// <summary>
    /// The source for the edge to be drawn.
    /// </summary>
    public GameObject from;

    /// <summary>
    /// The target of the edge to be drawn.
    /// </summary>
    public GameObject to;

    /// <summary>
    /// An abstract to call the needed function DrawEdge.
    /// </summary>
    private GraphRenderer abstractGraphRenderer;

    /// <summary>
    /// Function to set the source <paramref name="GameObject"/>
    /// </summary>
    /// <param name="GameObject">GameObject as source for the edge</param>
    public void SetHoveredObject(GameObject source)
    {
        hoveredObject = source;
    }

    

}
