using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Provides default material that can be shared among game objects to
    /// reduce the number of drawing calls. The material does not have
    /// any reflexions to save computation run-time.
    /// </summary>
    internal class Materials
    {
        private const string shaderName = "Diffuse";
        private static readonly Shader shader = Shader.Find(shaderName);
        private static Material defaultMaterial = Init();
        private static Material Init()
        {
            Material result = new Material(shader);
            // Turn off reflection
            result.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            result.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            result.SetFloat("_SpecularHighlights", 0.0f);
            return result;
        }

        /// <summary>
        /// Returns the default material (always the identical material, no matter
        /// how often this method is called). That means, if the caller modifies
        /// this material, other objects using it will be affected, too.
        /// </summary>
        /// <returns>default material</returns>
        public static Material DefaultMaterial()
        {
            return defaultMaterial;
        }
    }
}
