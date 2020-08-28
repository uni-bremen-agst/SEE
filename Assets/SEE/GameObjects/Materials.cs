using System;
using System.Collections.Generic;
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
        //private const string ShaderName = "Custom/PortalShader";
        private const string ShaderName = "Custom/PortalShaderTransparent";

        /// <summary>
        /// Creates default numberOfColors materials in the color range from
        /// lower to higher color (linear interpolation).
        /// </summary>
        /// <param name="numberOfColors">the number of materials with different colors to be created</param>
        /// <param name="lower">the color at the lower end of the color spectrum</param>
        /// <param name="higher">the color at the higher end of the color spectrum</param>
        public Materials(int numberOfColors, Color lower, Color higher)
        {
            NumberOfMaterials = numberOfColors;
            Lower = lower;
            Higher = higher;
            materials = new List<Material[]>() { Init(numberOfColors, lower, higher, 0) };
        }

        /// <summary>
        /// The number of different colors and, thus, the number of
        /// different materials we create: one material for each color.
        /// </summary>
        public readonly int NumberOfMaterials;

        /// <summary>
        /// The color at the lower end of the color spectrum.
        /// </summary>
        public readonly Color Lower;

        /// <summary>
        /// The color at the higher end of the color spectrum.
        /// </summary>
        public readonly Color Higher;

        /// <summary>
        /// The different materials. They are all alike except for the color.
        /// We will use a color gradient and one material for each color.
        /// </summary>
        private readonly List<Material[]> materials;

        /// <summary>
        /// Returns the default material for the given <paramref name="degree"/> (always the identical 
        /// material, no matter how often this method is called). That means, if 
        /// the caller modifies this material, other objects using it will be affected, too.
        /// 
        /// Precondition: 0 <= degree <= numberOfColors-1; otherwise an exception is thrown
        /// </summary>
        /// <param name="renderQueueOffset">The offset of the render queue for rendering.
        /// The larger the offset, the later the object will be rendered.</param>
        /// <param name="degree">index of the material (color) in the range [0, numberOfColors-1]</param>
        /// <returns>default material</returns>
        public Material DefaultMaterial(int renderQueueOffset, int degree)
        {
            if (degree < 0 || degree >= NumberOfMaterials)
            {
                throw new Exception("Color degree " + degree + " out of range [0," + (NumberOfMaterials - 1) + "]");
            }
            if (renderQueueOffset >= materials.Count)
            {
                for (int i = materials.Count; i <= renderQueueOffset; i++)
                {
                    materials.Add(Init(NumberOfMaterials, Lower, Higher, i));
                }
            }
            return materials[renderQueueOffset][degree];
        }

        /// <summary>
        /// Returns a new material with the given <paramref name="color"/>.
        /// </summary>
        /// <param name="color">color for the material</param>
        /// <returns>new material with given <paramref name="color"/></returns>
        public static Material NewMaterial(Color color)
        {
            Shader shader = Shader.Find(ShaderName);
            return NewMaterial(shader, color, 0);
        }

        public static void SetGlobalUniforms()
        {
            Vector2 leftFront = Plane.Instance.LeftFrontCorner;
            Shader.SetGlobalVector("portalMin", new Vector4(leftFront.x, leftFront.y));
            Vector2 rightFront = Plane.Instance.RightBackCorner;
            Shader.SetGlobalVector("portalMax", new Vector4(rightFront.x, rightFront.y));
        }

        /// <summary>
        /// Creates and returns the materials, one for each different color.
        /// </summary>
        /// <param name="numberOfColors">the number of materials with different colors to be created</param>
        /// <param name="lower">the color at the lower end of the color spectrum</param>
        /// <param name="higher">the color at the higher end of the color spectrum</param>
        /// <param name="renderQueueOffset">the offset of the render queue</param>
        /// <returns>materials</returns>
        private static Material[] Init(int numberOfColors, Color lower, Color higher, int renderQueueOffset)
        {
            if (numberOfColors < 1)
            {
                throw new Exception("Number of colors must be greater than 0.");
            }
            
            // Shader to retrieve the default material.
            Shader shader = Shader.Find(ShaderName);
            SetGlobalUniforms();

            Material[] result = new Material[numberOfColors];

            if (numberOfColors == 1)
            {
                result[0] = NewMaterial(shader, lower, renderQueueOffset);
            }
            else
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = NewMaterial(shader, Color.Lerp(lower, higher, (float)i / (float)(numberOfColors - 1)), renderQueueOffset);
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
        /// <param name="renderQueueOffset">the offset of the render queue</param>
        /// <returns>new material</returns>
        private static Material NewMaterial(Shader shader, Color color, int renderQueueOffset)
        {
            Material material = new Material(shader);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + renderQueueOffset;
            material.color = color;
            return material;
        }
    }
}
