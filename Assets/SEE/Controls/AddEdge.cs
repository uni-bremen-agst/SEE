using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.Game;

public class AddEdge : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && hoveredObject != null)
        {
            if (from == null)
            {
                from = hoveredObject;
                Debug.Log(from);
            }
            if (!from.Equals(hoveredObject) && to == null)
            {
                to = hoveredObject;
                Debug.Log(to);
            }
        }
        if (from != null && to != null)
        {
            SEECity city;
            city = SceneQueries.GetCodeCity(from.transform).GetComponent<SEECity>();
            Debug.Log(city);
            city.Renderer.DrawEdge(from, to);
            from = null;
            to = null;
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
