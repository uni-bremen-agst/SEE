using UnityEngine;
using SEE.Game;
using SEE.GO;
using System;

namespace SEE.Controls
{
    /// <summary>
    /// Action to create an edge between two selected nodes.
    /// </summary>
    public class AddEdge : MonoBehaviour
    {
        void Update()
        {
            // Assigning the game objects to be connected.
            // Checking whether the two game objects are not null and whether they are 
            // actually nodes.
            if (Input.GetMouseButtonDown(0) && hoveredObject != null && hoveredObject.HasNodeRef())
            {
                if (from == null)
                {
                    from = hoveredObject;
                }
                else if (to == null)
                {
                    to = hoveredObject;
                }
            }
            // Note: from == to may be possible.
            if (from != null && to != null)
            {
                Transform cityObject = SceneQueries.GetCodeCity(from.transform);
                if (cityObject != null)
                {
                    if (cityObject.TryGetComponent(out SEECity city))
                    {
                        try
                        {
                            city.Renderer.DrawEdge(from, to);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"The new edge from {from.name} to {to.name} could not be created: {e.Message}.\n");
                        }
                        from = null;
                        to = null;
                    }
                }
            }
            // Adding the key "F1" in order to delete the selected gameobjects.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                from = null;
                to = null;
            }
        }

        /// <summary>
        /// The currently hovered object.
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
        /// Set the currently hovered game node to <paramref name="node"/>
        /// that will be either the source or the target of the edge to be
        /// drawn. Which role it will play (source or target) depends upon
        /// the order (the first one hovered will be the source).
        /// </summary>
        /// <param name="node">GameObject as source for the edge</param>
        public void SetHoveredObject(GameObject node)
        {
            hoveredObject = node;
        }
    }
}