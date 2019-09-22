using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A block modifier for cubes as buildings. It is intended to be attached
    /// to the container game object containing a cube as block representation.
    /// It deals with the specifics of cubes with respect to size and scaling.
    /// </summary>
    public class CubeModifier : BlockModifier
    {
        public override Vector3 GetExtent()
        {
            // Nodes represented by cubes have a renderer from which we can derive the
            // extent.
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds.extents;
            }
            else
            {
                Debug.LogErrorFormat("Node {0} (tag: {1}) without renderer.\n", gameObject.name, gameObject.tag);
                return Vector3.one;
            }
        }
    }
}
