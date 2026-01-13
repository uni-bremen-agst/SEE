using System;
using System.Collections.Generic;
using SEE.Game;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.GO.Factories
{
    /// <summary>
    /// Provides default material that can be shared among game objects to
    /// reduce the number of drawing calls. The material does not have
    /// any reflexions to save computation run-time.
    /// </summary>
    public class MaterialsFactory
    {
        /// <summary>
        /// Different types of shaders used to draw the materials.
        /// </summary>
        public enum ShaderType
        {
            TransparentLine    = 1, // for lines with transparency (LineRenderer)
            TransparentEdge    = 2, // for edge meshes with transparency (MeshRenderer)
            OpaqueMetallic     = 3, // for opaque meshes with a more realistic metallic effect
            PortalFree         = 4, // not limited by a portal (seen everywhere)
            DrawableLine       = 5, // for drawable lines
            DrawableDashedLine = 6, // for drawable dashed lines
            Sprite             = 7, // for sprites (planes with textures with transparency) visible within portal
        }

        /// <summary>
        /// Name of the material for transparent lines using LineRenderer (located in folder Resources).
        /// </summary>
        private const string transparentLineMaterialName = "Materials/TransparentLinePortalMaterial";
        /// <summary>
        /// Name of the material for transparent 3D edges (located in folder Resources).
        /// </summary>
        private const string transparentEdgeMaterialName = "Materials/TransparentEdgePortalMaterial";
        /// <summary>
        /// Name of the material for opaque, metallic meshes (located in folder Resources).
        /// </summary>
        private const string opaqueMetallicMaterialName = "Materials/PortalMaterial";
        /// <summary>
        /// Name of the material for materials seen everywhere, i.e., not only within a portal
        /// (located in folder Resources).
        /// </summary>
        private const string portalFreeMaterialName = "Materials/PortalFreeMaterial";
        /// <summary>
        /// Name of the material for materials seen everywhere, i.e., not only within a portal
        /// (located in folder Resources).
        /// </summary>
        private const string drawableLineMaterialName = "Materials/DrawableLineMaterial";
        /// <summary>
        /// Name of the material for materials seen everywhere, i.e., not only within a portal
        /// (located in folder Resources).
        /// </summary>
        private const string drawableDashedLineMaterialName = "Materials/DrawableDashedLineMaterial";
        /// <summary>
        /// Name of the material for sprites with transparency within a portal (located in Resources folder).
        /// </summary>
        private const string spriteMaterialName = "Materials/TransparentSpritePortalMaterial";

        /// <summary>
        /// The id of the shader property for the texture.
        /// </summary>
        private static readonly int texturePropertyID = Shader.PropertyToID("_MainTex");

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
        /// <param name="shaderType">Shader type to be used to draw the new materials.</param>
        /// <param name="colorRange">The color range for the new materials.</param>
        /// <param name="texture">Texture to be added; can be null in which case no texture is added.</param>
        public MaterialsFactory(ShaderType shaderType, ColorRange colorRange, Texture texture = null)
        {
            this.shaderType = shaderType;
            Assert.IsTrue(colorRange.NumberOfColors > 0, "At least one color is needed");
            NumberOfMaterials = colorRange.NumberOfColors;
            lowerColor = colorRange.Lower;
            higherColor = colorRange.Upper;
            this.texture = texture;
            // materials[0] is set up with the given colorRange for the render-queue offset 0.
            materials = new List<Material[]>() { Init(shaderType, colorRange.NumberOfColors, colorRange.Lower, colorRange.Upper, texture, 0) };
        }

        /// <summary>
        /// Creates and returns the materials, one for each different color.
        ///
        /// Precondition: <paramref name="numberOfColors"/> > 0.
        /// </summary>
        /// <param name="shaderType">The type of the shader to be used to create the material.</param>
        /// <param name="numberOfColors">The number of materials with different colors to be created.</param>
        /// <param name="lower">The color at the lower end of the color spectrum.</param>
        /// <param name="higher">The color at the higher end of the color spectrum.</param>
        /// <param name="texture">Texture to be added; can be null in which case no texture is added.</param>
        /// <param name="renderQueueOffset">The offset of the render queue.</param>
        /// <returns>Materials.</returns>
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
        /// <param name="renderer">Renderer whose shared material is to be set.</param>
        /// <param name="index">The index of the material.</param>
        public void SetSharedMaterial(Renderer renderer, int index)
        {
            renderer.sharedMaterial = Get(0, Mathf.Clamp(index, 0, (int)NumberOfMaterials - 1));
        }

        /// <summary>
        /// Adds <paramref name="texture"/> to <paramref name="material"/> as <see cref="TexturePropertyName"/>
        /// if <paramref name="material"/> has this property. If not, nothing happens. Likewise, if <paramref name="texture"/>
        /// is null, nothing happens.
        /// </summary>
        /// <param name="material">Material to which a texture should be added.</param>
        /// <param name="texture">Texture to be added; can be null.</param>
        private static void AddTexture(Material material, Texture texture)
        {
            if (texture != null && material.HasProperty(texturePropertyID))
            {
                material.SetTexture(texturePropertyID, texture);
            }
        }

        /// <summary>
        /// Creates and returns a new material. The material is loaded from a resource file with given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the file for the material; must be located in a resources folder.</param>
        /// <param name="color">The color of the new material.</param>
        /// <param name="texture">Texture to be added; can be null in which case no texture is added.</param>
        /// <param name="renderQueueOffset">The offset of the new material in the render queue.</param>
        /// <returns>New material.</returns>
        private static Material New(string name, Color color, Texture texture, int renderQueueOffset)
        {
            Material prefab = Resources.Load<Material>(name);
            Assert.IsNotNull(prefab, $"Material resource '{name}' could not be found!");
            Material material = new(prefab)
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
        /// <param name="shaderType">The type of the shader to be used to create the
        /// material.</param>
        /// <param name="color">Requested color of the new material.</param>
        /// <param name="texture">Texture to be added; can be null in which case no texture is added.</param>
        /// <param name="renderQueueOffset">The offset of the render queue.</param>
        /// <returns>New material.</returns>
        public static Material New(ShaderType shaderType, Color color, Texture texture = null, int renderQueueOffset = 0)
        {
            string name = null;

            switch (shaderType)
            {
                case ShaderType.TransparentLine:
                    name = transparentLineMaterialName;
                    break;
                case ShaderType.TransparentEdge:
                    name = transparentEdgeMaterialName;
                    break;
                case ShaderType.OpaqueMetallic:
                    name = opaqueMetallicMaterialName;
                    break;
                case ShaderType.PortalFree:
                    name = portalFreeMaterialName;
                    break;
                case ShaderType.DrawableLine:
                    name = drawableLineMaterialName;
                    break;
                case ShaderType.DrawableDashedLine:
                    name = drawableDashedLineMaterialName;
                    break;
                case ShaderType.Sprite:
                    name = spriteMaterialName;
                    break;
                default:
                    Assertions.InvalidCodePath();
                    break;
            }

            return New(name, color, texture, renderQueueOffset);
        }

        /// <summary>
        /// Dumps all properties of the given <paramref name="material"/> as well as
        /// those of its shader to the debug log. Can be used for debugging.
        /// </summary>
        /// <param name="material">Materials whose properties are to be dumped.</param>
        private static void DumpProperties(Material material)
        {
            Debug.Log($"Dumping properties for Material: {material.name}\n");

            foreach (string propertyName in material.GetPropertyNames(MaterialPropertyType.Vector))
            {
                Debug.Log($"Material Property: {propertyName}\n");
            }

            Shader shader = material.shader;
            if (shader == null)
            {
                Debug.LogError("Material has no shader.\n");
                return;
            }

            int propertyCount = shader.GetPropertyCount();
            Debug.Log($"Shader '{shader.name}' has {propertyCount} properties.\n");

            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = shader.GetPropertyName(i);
                UnityEngine.Rendering.ShaderPropertyType propertyType = shader.GetPropertyType(i);

                string propertyValue = "N/A";

                // Get the value based on the property type
                switch (propertyType)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        propertyValue = material.GetColor(propertyName).ToString();
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        propertyValue = material.GetVector(propertyName).ToString();
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        propertyValue = material.GetFloat(propertyName).ToString();
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Int:
                        propertyValue = material.GetInt(propertyName).ToString();
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        Texture texture = material.GetTexture(propertyName);
                        propertyValue = (texture != null) ? texture.name : "None";
                        break;
                    default:
                        Debug.LogWarning($"Unhandled property type: {propertyType}\n");
                        break;
                }

                Debug.Log($" Shader Property {i}: Name='{propertyName}', Type='{propertyType}', Value='{propertyValue}'\n");
            }
        }
    }
}
