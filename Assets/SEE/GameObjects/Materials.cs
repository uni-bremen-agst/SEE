using SEE.DataModel;
using SEE.Game;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.GO
{
    /// <summary>
    /// Provides default material that can be shared among game objects to
    /// reduce the number of drawing calls. The material does not have
    /// any reflexions to save computation run-time.
    /// </summary>
    public class Materials
    {
        public enum ShaderType
        {
            Opaque,
            Transparent,
            TransparentLine
        }

        public const string OpaqueShaderName = "Custom/OpaquePortalShader";
        public const string TransparentShaderName = "Custom/TransparentPortalShader";
        public const string TransparentLineShaderName = "Custom/TransparentLinePortalShader";

        public const string OpaqueMaterialName = "Materials/OpaquePortalMaterial";
        public const string TransparentMaterialName = "Materials/TransparentPortalMaterial";
        public const string TransparentLineMaterialName = "Materials/TransparentLinePortalMaterial";
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="shaderType"></param>
        /// <param name="colorRange"></param>
        public Materials(ShaderType shaderType, ColorRange colorRange)
        {
            Type = shaderType;
            NumberOfMaterials = colorRange.NumberOfColors;
            Lower = colorRange.lower;
            Higher = colorRange.upper;
            materials = new List<Material[]>() { Init(shaderType, colorRange.NumberOfColors, colorRange.lower, colorRange.upper, 0) };
        }

        /// <summary>
        /// The type of the shaders of this material instance.
        /// </summary>
        public readonly ShaderType Type;

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
        public Material Get(int renderQueueOffset, int degree)
        {
            if (degree < 0 || degree >= NumberOfMaterials)
            {
                throw new Exception("Color degree " + degree + " out of range [0," + (NumberOfMaterials - 1) + "]");
            }
            if (renderQueueOffset >= materials.Count)
            {
                for (int i = materials.Count; i <= renderQueueOffset; i++)
                {
                    materials.Add(Init(Type, NumberOfMaterials, Lower, Higher, i));
                }
            }
            return materials[renderQueueOffset][degree];
        }

        /// <summary>
        /// Creates and returns the materials, one for each different color.
        /// </summary>
        /// <param name="shaderType">the type of the shader to be used to create the material</param>
        /// <param name="numberOfColors">the number of materials with different colors to be created</param>
        /// <param name="lower">the color at the lower end of the color spectrum</param>
        /// <param name="higher">the color at the higher end of the color spectrum</param>
        /// <param name="renderQueueOffset">the offset of the render queue</param>
        /// <returns>materials</returns>
        private static Material[] Init(ShaderType shaderType, uint numberOfColors, Color lower, Color higher, int renderQueueOffset)
        {
            Assert.IsTrue(numberOfColors > 0, "Number of colors must be greater than 0!");
            
            Material[] result = new Material[numberOfColors];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = NewMaterial(shaderType, Color.Lerp(lower, higher, (float)i / (float)(numberOfColors - 1)), renderQueueOffset);
            }
            return result;
        }

        /// <summary>
        /// Creates and returns a new material with the given <paramref name="color"/>.
        /// Reflections are turned off for this material.
        /// </summary>
        /// <param name="shaderType">the type of the shader to be used to create the material</param>
        /// <param name="color">requested color of the new material</param>
        /// <param name="renderQueueOffset">the offset of the render queue</param>
        /// <returns>new material</returns>
        public static Material NewMaterial(ShaderType shaderType, Color color, int renderQueueOffset = 0)
        {
            string materialName = null;

            switch (shaderType)
            {
                case ShaderType.Opaque:          materialName = OpaqueMaterialName;          break;
                case ShaderType.Transparent:     materialName = TransparentMaterialName;     break;
                case ShaderType.TransparentLine: materialName = TransparentLineMaterialName; break;
                default: Assertions.InvalidCodePath();                                       break;
            }

            Material materialPrefab = Resources.Load<Material>(materialName);
            Assert.IsNotNull(materialPrefab, "Material resource '" + materialName + "' could not be found!");

            Material material = new Material(materialPrefab);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + renderQueueOffset;
            material.color = color;
            return material;
        }
    }
}
