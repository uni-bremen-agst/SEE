using SEE.DataModel;
using SEE.Game;
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
        private const string LineShaderName = "Custom/PortalShaderTransparentLine";

        /// <summary>
        /// Returns the standard shader for transparent materials that can be culled
        /// if they leave a certain area (portal).
        /// </summary>
        /// <returns>standard portal shader for transparent objects</returns>
        public static Shader PortalShader()
        {
            return Shader.Find(ShaderName);
        }

        public static Shader PortalLineShader()
        {
            return Shader.Find(LineShaderName);
        }

        /// <summary>
        /// Returns a new instance of the standard shader for transparent materials that can be culled
        /// if they leave a certain area (portal). Changes to this instance will not affect the
        /// standard portal shared retrieved by PortalShader().
        /// </summary>
        /// <returns>new instance of standard portal shader for transparent objects</returns>
        public static Shader NewPortalShader()
        {
            return (Shader)GameObject.Instantiate(PortalShader());
        }

        public static Shader NewPortalShaderLine()
        {
            return (Shader)GameObject.Instantiate(PortalLineShader());
        }

        /// <summary>
        /// Creates default numberOfColors materials in the color range from
        /// lower to higher color (linear interpolation).
        /// </summary>
        /// <param name="numberOfColors">the number of materials with different colors to be created</param>
        /// <param name="lower">the color at the lower end of the color spectrum</param>
        /// <param name="higher">the color at the higher end of the color spectrum</param>
        public Materials(Shader shader, ColorRange colorRange)
        {
            this.shader = shader;
            NumberOfMaterials = colorRange.NumberOfColors;
            Lower = colorRange.lower;
            Higher = colorRange.upper;
            materials = new List<Material[]>() { Init(shader, colorRange.NumberOfColors, colorRange.lower, colorRange.upper, 0) };
        }

        /// <summary>
        /// The shader to be used for all materials created here.
        /// </summary>
        private readonly Shader shader;

        /// <summary>
        /// The number of different colors and, thus, the number of
        /// different materials we create: one material for each color.
        /// </summary>
        public readonly uint NumberOfMaterials;

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
                    materials.Add(Init(shader, NumberOfMaterials, Lower, Higher, i));
                }
            }
            return materials[renderQueueOffset][degree];
        }

        /// <summary>
        /// Returns a new material with the given <paramref name="color"/>.
        /// </summary>
        /// <param name="color">color for the material</param>
        /// <returns>new material with given <paramref name="color"/></returns>
        public static Material NewMaterial(Shader shader, Color color)
        {
            // Shader shader = Shader.Find(ShaderName);
            return NewMaterial(shader, color, 0);
        }

        //private static void SetGlobalUniforms()
        //{
        //    // FIXME: We need to support multiple culling planes.
        //    GameObject table = GameObject.FindGameObjectWithTag(Tags.CullingPlane);
        //    if (table != null)
        //    {
        //        Plane plane = table.GetComponent<Plane>();
        //        if (plane != null)
        //        {
        //            //Vector2 leftFront = Plane.Instance.LeftFrontCorner;
        //            Vector2 leftFront = plane.LeftFrontCorner;
        //            Shader.SetGlobalVector("portalMin", new Vector4(leftFront.x, leftFront.y));
        //            //Vector2 rightFront = Plane.Instance.RightBackCorner;
        //            Vector2 rightFront = plane.RightBackCorner;
        //            Shader.SetGlobalVector("portalMax", new Vector4(rightFront.x, rightFront.y));
        //        }
        //        else
        //        {
        //            Debug.LogErrorFormat("No plane attached to culling plane {0}.\n", table.name);
        //        }
        //    }
        //    else
        //    {
        //        Debug.LogErrorFormat("No game object tagged by {0} (serving as culling plane).\n", Tags.CullingPlane);
        //    }
        //}

        /// <summary>
        /// Creates and returns the materials, one for each different color.
        /// </summary>
        /// <param name="numberOfColors">the number of materials with different colors to be created</param>
        /// <param name="lower">the color at the lower end of the color spectrum</param>
        /// <param name="higher">the color at the higher end of the color spectrum</param>
        /// <param name="renderQueueOffset">the offset of the render queue</param>
        /// <returns>materials</returns>
        private static Material[] Init(Shader shader, uint numberOfColors, Color lower, Color higher, int renderQueueOffset)
        {
            if (numberOfColors < 1)
            {
                throw new Exception("Number of colors must be greater than 0.");
            }
            
            // Shader to retrieve the default material.
            // Shader shader = (Shader)GameObject.Instantiate(Resources.Load(ShaderName));
            // Shader shader = Shader.Find(ShaderName);
            // SetGlobalUniforms();

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
