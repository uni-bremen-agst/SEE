using SEE.DataModel;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A factory for cubes as visual representations of graph nodes in the scene.
    /// Cubes are used for both leaves (as an alternative to CScape buildings) and
    /// inner nodes (e.g., for the streets in EvoStreets), but because they are
    /// used for inner nodes, too, they must provide SetLineWidth() even though
    /// it does not do anything.
    /// </summary>
    public class CubeFactory : InnerNodeFactory
    {
        public override GameObject NewBlock(int style)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.tag = Tags.Node;
            Renderer renderer = result.GetComponent<Renderer>();
            // Object should not cast shadows: too expensive and may hide information,
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Assigns a material to the object.
            renderer.sharedMaterial = materials.DefaultMaterial(Mathf.Clamp(style, 0, NumberOfStyles()-1));

            // Add collider so that we can interact with the object.
            result.AddComponent<BoxCollider>();

            // Object should be static so that we save rendering time at run-time.
            result.isStatic = true;
            return result;
        }
    }
}

