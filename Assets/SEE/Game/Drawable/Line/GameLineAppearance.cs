using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Provides functionality for configuring the visual appearance of drawable lines.
    /// </summary>
    public static class GameLineAppearance
    {
        /// <summary>
        /// Gets the available color kinds.
        /// For dashed lines, <see cref="ColorKind.TwoDashed"/> is included.
        /// For solid lines, only <see cref="ColorKind.Monochrome"/> and
        /// <see cref="ColorKind.Gradient"/> are available.
        /// </summary>
        /// <param name="isDashedLineKind">
        /// Whether the line uses a dashed line kind.
        /// </param>
        /// <returns>The available color kinds.</returns>
        public static IList<ColorKind> GetColorKinds(bool isDashedLineKind)
        {
            if (isDashedLineKind)
            {
                return Enum.GetValues(typeof(ColorKind))
                    .Cast<ColorKind>()
                    .ToList();
            }

            return new List<ColorKind>
            {
                ColorKind.Monochrome,
                ColorKind.Gradient
            };
        }

        /// <summary>
        /// Gets all available line kinds.
        /// </summary>
        /// <returns>All available line kinds.</returns>
        public static IList<LineKind> GetLineKinds()
        {
            return Enum.GetValues(typeof(LineKind))
                .Cast<LineKind>()
                .ToList();
        }

        /// <summary>
        /// Changes the line kind of the given shape.
        /// </summary>
        /// <param name="shape">The shape whose line kind should be changed.</param>
        /// <param name="lineKind">The new line kind.</param>
        /// <param name="tiling">
        /// The texture tiling used for <see cref="LineKind.Dashed"/>.
        /// </param>
        public static void ChangeLineKind(
            GameObject shape,
            LineKind lineKind,
            float tiling)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                LineValueHolder holder = shape.GetComponent<LineValueHolder>();
                LineRenderer renderer = shape.GetComponent<LineRenderer>();

                renderer.sharedMaterial =
                    GetMaterial(renderer.material.color, lineKind);

                SetTextureMode(renderer, lineKind);
                SetRendererTextureScale(renderer, lineKind, tiling);

                holder.LineKind = lineKind;
            }
        }

        /// <summary>
        /// Changes the color kind of the given shape and restores its configured colors.
        /// </summary>
        /// <param name="shape">The shape whose color kind should be changed.</param>
        /// <param name="colorKind">The new color kind.</param>
        /// <param name="conf">
        /// The visual configuration containing the colors to restore.
        /// </param>
        public static void ChangeColorKind(
            GameObject shape,
            ColorKind colorKind,
            ILineVisualConf conf)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                LineValueHolder holder = shape.GetComponent<LineValueHolder>();
                LineRenderer renderer = shape.GetComponent<LineRenderer>();

                if (colorKind == ColorKind.TwoDashed)
                {
                    renderer.startColor = Color.white;
                    renderer.endColor = Color.white;

                    if (renderer.materials.Length == 1)
                    {
                        Material[] materials = new Material[2];
                        materials[0] = renderer.materials[0];
                        materials[1] = GetMaterial(Color.white, LineKind.Solid);
                        renderer.materials = materials;
                    }
                }
                else
                {
                    if (renderer.materials.Length > 1)
                    {
                        Material[] materials = new Material[1];
                        materials[0] = renderer.materials[0];
                        renderer.materials = materials;
                    }

                    if (colorKind == ColorKind.Gradient)
                    {
                        renderer.material.color = Color.white;
                    }
                    else
                    {
                        renderer.startColor = Color.white;
                        renderer.endColor = Color.white;
                    }
                }

                holder.ColorKind = colorKind;

                ChangePrimaryColor(shape, conf.PrimaryColor);
                ChangeSecondaryColor(shape, conf.SecondaryColor);

                if (conf.SecondaryColor == Color.clear)
                {
                    ChangeSecondaryColor(shape, conf.PrimaryColor);
                }
            }
        }

        /// <summary>
        /// Changes the primary color of the given shape according to its current color kind.
        /// </summary>
        /// <param name="shape">The shape whose primary color should be changed.</param>
        /// <param name="color">The new primary color.</param>
        public static void ChangePrimaryColor(GameObject shape, Color color)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                LineRenderer renderer = shape.GetComponent<LineRenderer>();

                switch (shape.GetComponent<LineValueHolder>().ColorKind)
                {
                    case ColorKind.Monochrome:
                        renderer.startColor = renderer.endColor = Color.white;
                        renderer.material.color = color;
                        break;

                    case ColorKind.Gradient:
                        renderer.material.color = Color.white;
                        renderer.startColor = color;
                        break;

                    case ColorKind.TwoDashed:
                        renderer.material.color = color;
                        break;
                }
            }
        }

        /// <summary>
        /// Changes the secondary color of the given shape according to its current color kind.
        /// </summary>
        /// <param name="shape">The shape whose secondary color should be changed.</param>
        /// <param name="color">The new secondary color.</param>
        public static void ChangeSecondaryColor(GameObject shape, Color color)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                LineRenderer renderer = shape.GetComponent<LineRenderer>();

                switch (shape.GetComponent<LineValueHolder>().ColorKind)
                {
                    case ColorKind.Gradient:
                        renderer.material.color = Color.white;
                        renderer.endColor = color;
                        break;

                    case ColorKind.TwoDashed:
                        renderer.materials[1].color = color;
                        break;

                    case ColorKind.Monochrome:
                        renderer.startColor = renderer.endColor = Color.white;
                        break;
                }
            }
        }

        /// <summary>
        /// Sets the texture scale of the given renderer according to the selected line kind.
        /// </summary>
        /// <param name="renderer">The renderer whose texture scale should be updated.</param>
        /// <param name="lineKind">The line kind defining the texture scale.</param>
        /// <param name="tiling">
        /// The custom texture tiling used for <see cref="LineKind.Dashed"/>.
        /// </param>
        internal static void SetRendererTextureScale(
            LineRenderer renderer,
            LineKind lineKind,
            float tiling)
        {
            switch (lineKind)
            {
                case LineKind.Dashed:
                    if (tiling == 0)
                    {
                        tiling = 0.05f;
                    }

                    renderer.textureScale = new Vector2(tiling, 0f);
                    break;

                case LineKind.Dashed25:
                    renderer.textureScale = new Vector2(5f / 3f, 0f);
                    break;

                case LineKind.Dashed50:
                    renderer.textureScale = new Vector2(10f / 3f, 0f);
                    break;

                case LineKind.Dashed75:
                    renderer.textureScale = new Vector2(5f, 0f);
                    break;

                case LineKind.Dashed100:
                    renderer.textureScale = new Vector2(20f / 3f, 0f);
                    break;
            }
        }

        /// <summary>
        /// Sets the texture mode of the given renderer according to the selected line kind.
        /// </summary>
        /// <param name="renderer">The renderer whose texture mode should be updated.</param>
        /// <param name="lineKind">The line kind defining the texture mode.</param>
        internal static void SetTextureMode(
            LineRenderer renderer,
            LineKind lineKind)
        {
            switch (lineKind)
            {
                case LineKind.Dashed
                    or LineKind.Dashed25
                    or LineKind.Dashed50
                    or LineKind.Dashed75
                    or LineKind.Dashed100:
                    renderer.textureMode = LineTextureMode.Tile;
                    break;

                default:
                    renderer.textureMode = LineTextureMode.Stretch;
                    break;
            }
        }

        /// <summary>
        /// Creates a material for the given color and line kind.
        /// </summary>
        /// <param name="color">The color of the material.</param>
        /// <param name="lineKind">The line kind defining the material shader.</param>
        /// <returns>The created material.</returns>
        internal static Material GetMaterial(Color color, LineKind lineKind)
        {
            ColorRange colorRange = new(color, color, 1);

            MaterialsFactory.ShaderType shaderType =
                lineKind == LineKind.Solid
                    ? MaterialsFactory.ShaderType.PortalFreeLine
                    : MaterialsFactory.ShaderType.DrawableDashedLine;

            MaterialsFactory materials = new(shaderType, colorRange);
            return materials.Get(0);
        }
    }
}
