using UnityEngine;

namespace SEE.Layout
{
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
