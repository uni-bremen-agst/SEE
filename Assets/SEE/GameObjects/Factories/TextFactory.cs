﻿using System.Collections.Generic;
using System.Linq;
using SEE.Game;
using SEE.Game.City;
using TMPro;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A factory for text objects that rotate towards the camera.
    /// The created texts respect a portal (culling area).
    /// </summary>
    internal static class TextFactory
    {
        /// <summary>
        /// Color of the text.
        /// </summary>
        private static readonly Color textColorDefault = Color.white;

        /// <summary>
        /// Name of the font used for the text. This must be a font with portal information.
        /// </summary>
        private const string portalFontName = "Fonts & Materials/LiberationSans SDF - Portal";

        /// <summary>
        /// The font asset used for all texts created by this <see cref="TextFactory"/>.
        /// </summary>
        private static readonly TMP_FontAsset fontAsset = NewFont();

        /// <summary>
        /// Retrieves the font asset <see cref="portalFontName"/> that should be used for all texts
        /// created by this <see cref="TextFactory"/>. If this font asset is not found, a default font
        /// asset will be used instead and an error message will be logged.
        /// </summary>
        /// <returns>font asset to be used for all texts</returns>
        private static TMP_FontAsset NewFont()
        {
            TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>(portalFontName);
            if (fontAsset == null)
            {
                Debug.LogError($"Font {portalFontName} not found. Using default font.\n");
                fontAsset = TMP_Settings.defaultFontAsset;
            }
            return fontAsset;
        }

        /// <summary>
        /// This will apply an outline effect across <em>all</em> TextMeshPro instances with the same
        /// shared material as the given one.
        /// </summary>
        /// <param name="outline">Whether the outline should be enabled or disabled.
        /// If this is false, all other parameters besides <paramref name="tm"/> will be ignored.</param>
        /// <param name="tm">The <see cref="TextMeshPro"/> instance whose material we should apply the effect on.
        /// Note that this will affect every TextMeshPro instance with the same material.</param>
        /// <param name="thickness">The thickness of the outline. Default: 0.1</param>
        /// <param name="outlineColor">The color of the outline. Default: white</param>
        public static void SetOutline(bool outline, TextMeshPro tm, float thickness = 0.1f, Color? outlineColor = null)
        {
            string[] keywords = tm.fontSharedMaterial.shaderKeywords ?? new string[] { };
            if (outline)
            {
                tm.fontSharedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor ?? Color.black);
                tm.fontSharedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, thickness);
                tm.fontSharedMaterial.shaderKeywords = keywords.Append(ShaderUtilities.Keyword_Outline).ToArray();
            }
            else
            {
                // Remove outline activation keyword
                tm.fontSharedMaterial.shaderKeywords = keywords
                                                       .Where(x => !x.Equals(ShaderUtilities.Keyword_Outline))
                                                       .ToArray();
            }
            tm.UpdateMeshPadding();
        }

        /// <summary>
        /// Returns a game object showing the given <paramref name="text"/> at given <paramref name="position"/>
        /// with given <paramref name="fontSize"/>.
        /// The text rotates towards the main camera and respects a portal (culling area).
        /// </summary>
        /// <param name="city">the city in which the text is drawn; this is needed to create a material
        /// for the portal of the city</param>
        /// <param name="text">the text to be drawn</param>
        /// <param name="position">the center position at which to draw the text</param>
        /// <param name="fontSize">the size of the font with which the text is drawn</param>
        /// <param name="lift">if true, the text will be lifted up by its extent; that is, its y position is actually
        /// the bottom line (position.y + extents.y)</param>
        /// <param name="textColor">the color of the text (default: black)</param>
        /// <returns>the game object representing the text</returns>
        public static GameObject GetTextWithSize(AbstractSEECity city, string text, Vector3 position, float fontSize, bool lift = true,
                                                 Color? textColor = null)
        {
            CreateText(city, text, position, textColor, out TextMeshPro tm, out GameObject textObject);

            tm.fontSize = fontSize;

            Finalize(lift, tm, textObject);
            return textObject;
        }

        /// <summary>
        /// Returns a game object showing the given <paramref name="text"/> at given <paramref name="position"/>
        /// with given <paramref name="width"/>
        /// The text rotates towards the main camera.
        /// </summary>
        /// <param name="city">the city in which the text is drawn; this is needed to create a material
        /// for the portal of the city</param>
        /// <param name="text">the text to be drawn</param>
        /// <param name="position">the center position at which to draw the text</param>
        /// <param name="width">the width of the rectangle enclosing the text (essentially,
        /// the text width); the font size will be chosen appropriately</param>
        /// <param name="lift">if true, the text will be lifted up by its extent; that is, its y position is actually
        /// the bottom line (position.y + extents.y)</param>
        /// <param name="textColor">the color of the text (default: <see cref="textColorDefault"/>)</param>
        /// <returns>the game object representing the text</returns>
        public static GameObject GetTextWithWidth(AbstractSEECity city, string text, Vector3 position, float width, bool lift = true,
            Color? textColor = null)
        {
            CreateText(city, text, position, textColor, out TextMeshPro tm, out GameObject textObject);

            RectTransform rect = tm.GetComponent<RectTransform>();
            // We set width and height of the rectangle and leave the actual size to Unity,
            // which will select a font that matches our size constraints.
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width);

            tm.enableAutoSizing = true;
            tm.fontSizeMin = 0.0f;
            tm.fontSizeMax = 5;

            Finalize(lift, tm, textObject);
            return textObject;
        }

        /// <summary>
        /// Finalizes the text by lifting it up (if <paramref name="lift"/> is true) and disabling shadow casting mode.
        /// </summary>
        /// <param name="lift">if true, the <paramref name="textObject"/> will be lifted up by its extent; that is,
        /// its y position is actually the bottom line (position.y + extents.y)</param>
        /// <param name="tm">the <see cref="TextMeshPro"/> component attached to <paramref name="textObject"/>;
        /// relevant for the lifting</param>
        /// <param name="textObject">the object representing the text (holding <paramref name="tm"/>)</param>
        private static void Finalize(bool lift, TextMeshPro tm, GameObject textObject)
        {
            // No shading as this might be expensive and even distract.
            DisableShading(textObject);

            if (lift)
            {
                LiftText(tm, textObject);
            }
        }

        /// <summary>
        /// Lifts the <paramref name="textObject"/> up by the y extent of <paramref name="tm"/>.
        /// </summary>
        /// <param name="tm">the <see cref="TextMeshPro"/> component attached to <paramref name="textObject"/>;
        /// relevant for the lifting</param>
        /// <param name="textObject">the object representing the text (holding <paramref name="tm"/>)</param>
        private static void LiftText(TextMeshPro tm, GameObject textObject)
        {
            // may need to be called before retrieving the bounds to make sure they are up to date
            tm.ForceMeshUpdate();
            // unlike other types of game objects, the renderer does not allow us to retrieve the
            // extents of the text; we need to use tm.textBounds instead
            textObject.transform.position += tm.textBounds.extents.y * Vector3.up;
        }

        /// <summary>
        /// Disables shadow casting mode for the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">game objects whose shadow casting should be turned off</param>
        private static void DisableShading(GameObject gameObject)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// Creates a new GameObject with a TextMeshPro component at the given <paramref name="position"/>.
        /// The text will have the given <paramref name="text"/> and <paramref name="textColor"/> and will be
        /// center aligned.
        /// </summary>
        /// <param name="city">the city in which the text is drawn; this is needed to create a material
        /// for the portal of the city</param>
        /// <param name="text">The text to be used</param>
        /// <param name="position">the center position at which to create the GameObject</param>
        /// <param name="textColor">the color of the text</param>
        /// <param name="tm">the TextMeshPro component which will be attached to <paramref name="textObject"/></param>
        /// <param name="textObject">the GameObject containing the <see cref="TextMeshPro"/> component <paramref name="tm"/></param>
        private static void CreateText(AbstractSEECity city, string text, Vector3 position, Color? textColor, out TextMeshPro tm,
            out GameObject textObject)
        {
            textObject = new GameObject("Text " + text)
            {
                tag = Tags.Text
            };
            textObject.transform.position = position;

            tm = textObject.AddComponent<TextMeshPro>();
            tm.font = fontAsset;
            tm.text = text;
            tm.color = textColor ?? textColorDefault;
            tm.alignment = TextAlignmentOptions.Center;
            AssignFontMaterial(city, tm);
        }

        /// <summary>
        /// A mapping of seen cities to the materials used for their texts. Every text created
        /// for a particular city (key) will receive the same material (value).
        /// </summary>
        private static readonly Dictionary<AbstractSEECity, Material> seenCities = new();

        /// <summary>
        /// Assigns a font material to <paramref name="tm"/> based on <paramref name="city"/>.
        /// This is necessary because we want all texts in the same city to share the same material, so that
        /// they can be culled according to the city's portal.
        ///
        /// If <paramref name="city"/> has not been seen before, a new material is created based on <see cref="fontAsset"/>
        /// and assigned to <paramref name="tm"/> (and added to <see cref="seenCities"/>).
        ///
        /// If <paramref name="city"/> has been seen before, its material stored in <see cref="seenCities"/> will be used.
        /// </summary>
        /// <param name="city">the city in which to create the text</param>
        /// <param name="tm">the text whose fontMaterial might need to be set</param>
        private static void AssignFontMaterial(AbstractSEECity city, TextMeshPro tm)
        {
            if (!seenCities.TryGetValue(city, out Material material))
            {
                material = new Material(fontAsset.material);
                seenCities[city] = material;
            }
            tm.fontMaterial = material;
        }
    }
}
