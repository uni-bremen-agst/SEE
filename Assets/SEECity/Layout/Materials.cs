using System;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Provides default material that can be shared among game objects to
    /// reduce the number of drawing calls. The material does not have
    /// any reflexions to save computation run-time.
    /// </summary>
    public class Materials
    {
        /// <summary>
        /// Name of default shader to obtain the default material.
        /// </summary>
        private const string shaderName = "Diffuse";

        /// <summary>
        /// Creates default numberOfColors materials in the color range from
        /// lower to higher color (linear interpolation).
        /// </summary>
        /// <param name="numberOfColors">the number of materials with different colors to be created</param>
        /// <param name="lower">the color at the lower end of the color spectrum</param>
        /// <param name="higher">the color at the higher end of the color spectrum</param>
        public Materials(int numberOfColors, Color lower, Color higher)
        {
            this.numberOfColors = numberOfColors;
            materials = Init(numberOfColors, lower, higher);
        }

        /// <summary>
        /// The number of materials offered.
        /// </summary>
        /// <returns>number of materials offered</returns>
        public int NumberOfMaterials()
        {
            return numberOfColors;
        }

        /// <summary>
        /// The number of different colors and, thus, the number of
        /// different materials we create: one material for each color.
        /// </summary>
        private readonly int numberOfColors;

        /// <summary>
        /// The different materials. They are all alike except for the color.
        /// We will use a color gradient and one material for each color.
        /// </summary>
        private readonly Material[] materials;

        /// <summary>
        /// Creates and returns the materials, one for each different color.
        /// </summary>
        /// <param name="numberOfColors">the number of materials with different colors to be created</param>
        /// <param name="lower">the color at the lower end of the color spectrum</param>
        /// <param name="higher">the color at the higher end of the color spectrum</param>       
        /// <returns>materials</returns>
        private static Material[] Init(int numberOfColors, Color lower, Color higher)
        {
            // Shader to retrieve the default material.
            Shader shader = Shader.Find(shaderName);

            Material[] result = new Material[numberOfColors];

            for (int i = 0; i < result.Length; i++)
            {
                Material material = new Material(shader);
                // Turn off reflection
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                material.SetFloat("_SpecularHighlights", 0.0f);
                material.color = Color.Lerp(lower, higher, (float)i / (float)(numberOfColors - 1));
                result[i] = material;
            }
            return result;
        }

        /// <summary>
        /// Returns the default material for the given indes (always the identical 
        /// material, no matter how often this method is called). That means, if 
        /// the caller modifies this material, other objects using it will be affected, too.
        /// 
        /// Precondition: 0 <= degree <= numberOfColors-1; otherwise an exception is thrown
        /// </summary>
        /// <param name="degree">index of the material (color) in the range [0, numberOfColors-1]</param>
        /// <returns>default material</returns>
        public Material DefaultMaterial(int degree = 0)
        {
            if (degree < 0 || degree >= numberOfColors)
            {
                throw new Exception("color degree " + degree + " out of range [0," + (numberOfColors - 1) + "]");
            }
            return materials[degree];
        }
    }
}
