using System;
using UnityEngine;

namespace SEE.GO
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
        private const string shaderName = "Custom/PortalShader";

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
            if (numberOfColors < 1)
            {
                throw new Exception("Number of colors must be greater than 0.");
            }

            Shader.SetGlobalVector("portalMin", new Vector4(Controls.NavigationAction.TableMinX, Controls.NavigationAction.TableMinZ));
            Shader.SetGlobalVector("portalMax", new Vector4(Controls.NavigationAction.TableMaxX, Controls.NavigationAction.TableMaxZ));
            // Shader to retrieve the default material.
            Shader shader = Shader.Find(shaderName);

            Material[] result = new Material[numberOfColors];

            if (numberOfColors == 1)
            {
                result[0] = NewMaterial(shader, lower);
            }
            else
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = NewMaterial(shader, Color.Lerp(lower, higher, (float)i / (float)(numberOfColors - 1)));
                }
            }
            return result;
        }

        /// <summary>
        /// Creates and returns a new material with the given <paramref name="color"/>.
        /// Reflections are turned off for this material.
        /// </summary>
        /// <param name="shader">shader to be used to create the material</param>
        /// <param name="color">requested color of the new material</param>
        /// <returns>new material</returns>
        private static Material NewMaterial(Shader shader, Color color)
        {
            Material material = new Material(shader);
            // Turn off reflection
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            material.SetFloat("_SpecularHighlights", 0.0f);
            material.color = color;
            return material;
        }

        /// <summary>
        /// Returns the default material for the given <paramref name="degree"/> (always the identical 
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

        /// <summary>
        /// Returns a new material with the given <paramref name="color"/>.
        /// </summary>
        /// <param name="color">color for the material</param>
        /// <returns>new material with given <paramref name="color"/></returns>
        public static Material NewMaterial(Color color)
        {
            Shader shader = Shader.Find(shaderName);
            return NewMaterial(shader, color);
        }
    }
}
