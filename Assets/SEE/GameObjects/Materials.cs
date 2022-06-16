using System;
using System.Collections.Generic;
using SEE.Game;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

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
        /// Different types of shaders used to draw the materials.
        /// </summary>
        public enum ShaderType
        {
            Opaque,          // fully drawn with no transparency
            TransparentLine, // for lines with transparency
        }

        /// <summary>
        /// Name of the material for opaque objects (located in folder Resources).
        /// </summary>
        private const string OpaqueMaterialName = "Materials/OpaquePortalMaterial";
        /// <summary>
        /// Name of the material for transparent lines (located in folder Resources).
        /// </summary>
        private const string TransparentLineMaterialName = "Materials/TransparentLinePortalMaterial";

        /// <summary>
        /// The id of the shader property for the texture.
        /// </summary>
        private static readonly int TexturePropertyID = Shader.PropertyToID("_Texture");

        /// <summary>
        /// The type of the shaders of this material instance.
        /// </summary>
        private readonly ShaderType shaderType;

        /// <summary>
        /// The number of different colors and, thus, the number of
        /// different materials we create: one material for each color.
        /// </summary>
        public readonly uint NumberOfMaterials;

        /// <summary>
        /// The color at the lower end of the color spectrum.
        /// </summary>
        private readonly Color lowerColor;

        /// <summary>
        /// The color at the higher end of the color spectrum.
        /// </summary>
        private readonly Color higherColor;

        /// <summary>
        /// Texture to be added to the material; can be null in which case no texture is added.
        /// </summary>
        private readonly Texture texture;

        /// <summary>
        /// The different materials. They depend upon two aspects:
        /// the offset in the rendering queue and the number of colors requested.
        /// The first index in the list of <see cref="materials"/> is the offset
        /// in the rendering queue. The second index in a materials array,
        /// which is an element of that list, is the color index.
        /// The entries of the inner material array are all alike except for the color.
        /// We will use a color gradient and one material for each color.
        /// Similarly, <see cref="materials"/>[i] and <see cref="materials"/>[j] will
        /// be alike except for the respective <see cref="Material.renderQueue"/> attribute.
        /// </summary>
        private readonly List<Material[]> materials;

        /// <summary>
        /// Creates materials for the given <paramref name="colorRange"/>, one material
        /// for each color at render queue offset 0, with the associated <paramref name="shaderType"/>.
        /// All created materials are alike except for their color.
        ///
        /// Precondition: <paramref name="colorRange.NumberOfColors"/> must be greater than 0.
        /// </summary>
        /// <param name="shaderType">shader type to be used to draw the new materials</param>
        /// <param name="colorRange">the color range for the new materials</param>
        /// <param name="texture">texture to be added; can be null in which case no texture is added</param>
        public Materials(ShaderType shaderType, ColorRange colorRange, Texture texture = null)
        {
            this.shaderType = shaderType;
            Assert.IsTrue(colorRange.NumberOfColors > 0, "At least one color is needed");
            NumberOfMaterials = colorRange.NumberOfColors;
            lowerColor = colorRange.lower;
            higherColor = colorRange.upper;
            this.texture = texture;
            // materials[0] is set up with the given colorRange for the render-queue offset 0.
            materials = new List<Material[]>() { Init(shaderType, colorRange.NumberOfColors, colorRange.lower, colorRange.upper, texture, 0) };
        }

        /// <summary>
        /// Creates and returns the materials, one for each different color.
        ///
        /// Precondition: <paramref name="numberOfColors"/> > 0.
        /// </summary>
        /// <param name="shaderType">the type of the shader to be used to create the material</param>
        /// <param name="numberOfColors">the number of materials with different colors to be created</param>
        /// <param name="lower">the color at the lower end of the color spectrum</param>
        /// <param name="higher">the color at the higher end of the color spectrum</param>
        /// <param name="texture">texture to be added; can be null in which case no texture is added</param>
        /// <param name="renderQueueOffset">the offset of the render queue</param>
        /// <returns>materials</returns>
        private static Material[] Init(ShaderType shaderType, uint numberOfColors, Color lower, Color higher, Texture texture, int renderQueueOffset)
        {
            Material[] result = new Material[numberOfColors];
            if (numberOfColors == 1)
            {
                result[0] = New(shaderType, Color.Lerp(lower, higher, 0.5f), texture, renderQueueOffset);
            }
            else
            {
                // Assumption: numberOfColors > 1; if numberOfColors == 0, we would divide by zero.
                for (int i = 0; i < result.Length; i++)
                {
                    Color color = Color.Lerp(lower, higher, (float)i / (float)(numberOfColors - 1));
                    result[i] = New(shaderType, color, texture, renderQueueOffset);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the default material for the given <paramref name="index"/> (always the identical
        /// material, no matter how often this method is called). That means, if
        /// the caller modifies this material, other objects using it will be affected, too.
        /// <paramref name="renderQueueOffset"/> specifies the offset of the render queue for rendering.
        /// The larger the offset, the later the object will be rendered. An object drawn later
        /// will cover objects drawn earlier.
        /// Precondition: 0 <= index <= NumberOfMaterials-1 and renderQueueOffset >= 0; otherwise an exception is thrown
        /// </summary>
        /// <param name="renderQueueOffset">offset for the render queue</param>
        /// <param name="index">index of the material (color) in the range [0, NumberOfMaterials-1]</param>
        /// <returns>default material</returns>
        public Material Get(int renderQueueOffset, int index)
        {
            if (index < 0 || index >= NumberOfMaterials)
            {
                throw new Exception($"Color degree {index} out of range [0, {NumberOfMaterials - 1}]");
            }
            if (renderQueueOffset < 0)
            {
                throw new Exception("Render queue offset must not be negative");
            }
            if (renderQueueOffset >= materials.Count)
            {
                // there are no materials for this renderQueueOffset; we need to create these first
                for (int i = materials.Count; i <= renderQueueOffset; i++)
                {
                    materials.Add(Init(shaderType, NumberOfMaterials, lowerColor, higherColor, texture, i));
                }
            }
            return materials[renderQueueOffset][index];
        }

        /// <summary>
        /// Sets the shared material of <paramref name="renderer"/> to the material with given <paramref name="index"/>
        /// and <paramref name="renderQueueOffset"/>. The <paramref name="index"/> will be clamped into
        /// [0, <see cref="NumberOfMaterials"/> - 1].
        /// </summary>
        /// <param name="renderer">renderer whose shared material is to be set</param>
        /// <param name="renderQueueOffset">the offset in the render queue</param>
        /// <param name="index">the index of the material</param>
        public void SetSharedMaterial(Renderer renderer, int index)
        {
            renderer.sharedMaterial = Get(0, Mathf.Clamp(index, 0, (int)NumberOfMaterials - 1));
        }

        /// <summary>
        /// Adds a texture to <paramref name="material"/>.
        /// </summary>
        /// <param name="material">material to which a texture should be added</param>
        private static void AddTexture(Material material)
        {
            if (material.HasProperty(TexturePropertyID))
            {
                if (false)
                {
                    material.SetTexture(TexturePropertyID, NewTexture());
                }
                else
                {
                    const string TextureName = "Textures/TestTexture";
                    Texture texture = Resources.Load<Texture>(TextureName);
                    if (texture == null)
                    {
                        Debug.LogError($"No such texture {TextureName}\n");
                    }
                    else
                    {
                        //Debug.Log($"_Texture: {texture.name}\n");
                        material.SetTexture(TexturePropertyID, texture);
                    }
                }
            }
        }

        /// <summary>
        /// Adds <paramref name="texture"/> to <paramref name="material"/> as <see cref="TexturePropertyName"/>
        /// if <paramref name="material"/> has this property. If not, nothing happens. Likewise, if <paramref name="texture"/>
        /// is null, nothing happens.
        /// </summary>
        /// <param name="material">material to which a texture should be added</param>
        /// <param name="texture">texture to be added; can be null</param>
        private static void AddTexture(Material material, Texture texture)
        {
            if (texture != null && material.HasProperty(TexturePropertyID))
            {
                material.SetTexture(TexturePropertyID, texture);
            }
        }

        /// <summary>
        /// Creates and returns a new texture.
        /// Note: This method is currently used only to demonstrate on how to
        /// create a texture.
        /// </summary>
        /// <returns>a new texture</returns>
        private static Texture NewTexture()
        {
            Texture2D result = new Texture2D(512, 512);

            Color visible = new Color(1, 1, 1, 1);
            Color invisible = new Color(0, 0, 0, 0);

            for (int x = 0; x < result.width; x++)
            {
                for (int y = 0; y < result.height; y++)
                {
                    if (y % 5 == 0)
                    {
                        result.SetPixel(x, y, invisible);
                    }
                    else
                    {
                        result.SetPixel(x, y, visible);
                    }
                }
            }
            result.Apply();
            // SaveTexture(result);
            return result;

            static void SaveTexture(Texture2D result)
            {
                byte[] bytes = result.EncodeToPNG();
                string path = Application.dataPath + "/Resources/Textures/TestTexture.png";
                System.IO.File.WriteAllBytes(path, bytes);
                Debug.Log($"Texture written to {path}.\n");
            }
        }

        /// <summary>
        /// Creates and returns a new material. The material is loaded from a resource file with given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">the name of the file for the material; must be located in a resources folder</param>
        /// <param name="color">the color of the new material</param>
        /// <param name="texture">texture to be added; can be null in which case no texture is added</param>
        /// <param name="renderQueueOffset">the offset of the new material in the render queue</param>
        /// <returns>new material</returns>
        private static Material New(string name, Color color, Texture texture, int renderQueueOffset)
        {
            Material prefab = Resources.Load<Material>(name);
            Assert.IsNotNull(prefab, $"Material resource '{name}' could not be found!");
            Material material = new Material(prefab)
            {
                renderQueue = prefab.renderQueue + renderQueueOffset,
                color = color
            };

            AddTexture(material, texture);
            return material;
        }

        /// <summary>
        /// Creates and returns a new material of given <paramref name="shaderType"/> and
        /// <paramref name="color"/>. This material will be unique and not reused by this
        /// class!
        /// </summary>
        /// <param name="shaderType">the type of the shader to be used to create the
        /// material</param>
        /// <param name="color">requested color of the new material</param>
        /// <param name="texture">texture to be added; can be null in which case no texture is added</param>
        /// <param name="renderQueueOffset">the offset of the render queue</param>
        /// <returns>new material</returns>
        public static Material New(ShaderType shaderType, Color color, Texture texture = null, int renderQueueOffset = 0)
        {
            string name = null;

            switch (shaderType)
            {
                case ShaderType.Opaque:
                    name = OpaqueMaterialName;
                    break;
                case ShaderType.TransparentLine:
                    name = TransparentLineMaterialName;
                    break;
                default:
                    Assertions.InvalidCodePath();
                    break;
            }

            return New(name, color, texture, renderQueueOffset);
        }
    }
}
