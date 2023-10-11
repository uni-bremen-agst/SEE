using Assets.SEE.Game.UI.Drawable;
using HtmlAgilityPack;
using RTG;
using SEE.Game;
using SEE.Game.Drawable.Configurations;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static SEE.Game.GameDrawer;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class is responsible for creating the game objects for the written texts.
    /// </summary>
    public static class GameTexter
    {
        /// <summary>
        /// Name of the font for the text
        /// </summary>
        private const string DrawableTextFontName = "Fonts/DrawableTextFont";

        /// <summary>
        /// Removes HTML tags from string using char array.
        /// @by Sam Allen, 10.10.2023 https://www.dotnetperls.com/remove-html-tags
        /// </summary>
        /// <param name="source">The string which contains the HTML tags.</param>
        public static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        /// <summary>
        /// Calculates the text width based on the first line.
        /// For the calculation it removes the HTML Tags and checks if there are active font styles for bold, upper case or small caps.
        /// This font styles affects the width.
        /// 
        /// Method based on the method from Stephan_B's comment in
        /// https://forum.unity.com/threads/calculate-width-of-a-text-before-without-assigning-it-to-a-tmp-object.758867/
        /// </summary>
        /// <param name="text">the first line of the text.</param>
        /// <param name="fontAsset">the used font asset.</param>
        /// <param name="fontSize">the used font size.</param>
        /// <param name="style">the used font styles.</param>
        /// <returns>The calculated width for the first line of the text.</returns>
        private static float TextWidthApproximation(string text, TMP_FontAsset fontAsset, float fontSize, FontStyles style)
        {
            string result = StripTagsCharArray(text);

            text = text.ToLower();
            bool htmlBold = text.Contains("<b>");
            bool htmlUpperCase = text.Contains("<uppercase>");
            bool htmlSmallCaps = text.Contains("<smallcaps>");

            // Compute scale of the target point size relative to the sampling point size of the font asset.
            float pointSizeScale = fontSize / (fontAsset.faceInfo.pointSize * fontAsset.faceInfo.scale * 10);
            float emScale = fontSize * 0.001f;

            float styleSpacingAdjustment = (style & FontStyles.Bold) == FontStyles.Bold || htmlBold ? fontAsset.boldSpacing : 0;
            float normalSpacingAdjustment = fontAsset.normalSpacingOffset;
            float width = 0;
            if((style & FontStyles.UpperCase) != 0 || (style & FontStyles.SmallCaps) != 0 || htmlUpperCase || htmlSmallCaps)
            {
                result = result.ToUpper();
            }

            for (int i = 0; i < result.Length; i++)
            {
                char unicode = result[i];
                TMP_Character character;
                // Make sure the given unicode exists in the font asset.
                if (fontAsset.characterLookupTable.TryGetValue(unicode, out character))
                {
                    width += character.glyph.metrics.horizontalAdvance * pointSizeScale + (styleSpacingAdjustment + normalSpacingAdjustment) * emScale;
                }
            }

            return width;
        }

        /// <summary>
        /// This method calculates the width and height of the text.
        /// The width depends on the first line of the text.
        /// The height is approximated by dividing the number of characters in the text by the number of characters in the first line multiplies with 0.1f and the fontSize.
        /// </summary>
        /// <param name="text">The written text.</param>
        /// <param name="fontAsset">The font asset of the text</param>
        /// <param name="fontSize">The font size of the text</param>
        /// <param name="styles">The font styles of the text</param>
        /// <returns></returns>
        public static Vector2 CalculateWidthAndHeight(string text, TMP_FontAsset fontAsset, float fontSize, FontStyles styles)
        {
            string firstLine = text.Split("\n")[0];
            int lengthToFirstLineBreak = firstLine.Length;
            float x = TextWidthApproximation(firstLine, fontAsset, fontSize, styles);

            float height = text.Length / lengthToFirstLineBreak;
            float y = height * 0.1f * fontSize;
            return new Vector2(x, y);
        }

        /// <summary>
        /// This method creates the inital drawable text object
        /// </summary>
        /// <param name="drawable">Is the drawable on that the text should be displayed</param>
        /// <param name="name">The name of the text object</param>
        /// <param name="text">The text that should be displayed</param>
        /// <param name="position">The inital position of the text object</param>
        /// <param name="fontColor">The chosen font color of the text.</param>
        /// <param name="outlineColor">The chosen outline color of the text.</param>
        /// <param name="outlineThickness">The chosen outline thickness of the text.</param>
        /// <param name="fontSize">The chosen font size of the text</param>
        /// <param name="order">The current order in layer</param>
        /// <param name="styles">The chosen font styles for the text</param>
        /// <param name="textObj">The created drawable text object</param>
        private static void Setup(GameObject drawable, string name, string text, Vector3 position, Color fontColor, Color outlineColor, 
            float outlineThickness, float fontSize, int order, FontStyles styles,
            out GameObject textObj)
        {
            if (name.Length > 4)
            {
                textObj = new(name);
            }
            else
            {
                textObj = new("");
                name = ValueHolder.TextPrefix + textObj.GetInstanceID() + DrawableHolder.GetRandomString(4);
                while (GameDrawableFinder.FindChild(drawable, name) != null)
                {
                    name = ValueHolder.TextPrefix + textObj.GetInstanceID() + DrawableHolder.GetRandomString(4);
                }
                textObj.name = name;
            }

            GameObject highestParent, attachedObjects;
            DrawableHolder.Setup(drawable, out highestParent, out attachedObjects);

            textObj.tag = Tags.DText;
            textObj.transform.SetParent(attachedObjects.transform);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            
            tmp.text = text;
            tmp.fontStyle = styles;
            tmp.font = Resources.Load<TMP_FontAsset>(DrawableTextFontName);
            tmp.rectTransform.sizeDelta = CalculateWidthAndHeight(text, tmp.font, fontSize, styles);
            tmp.color = fontColor;
            tmp.faceColor = fontColor;
            tmp.fontSize = fontSize;
            tmp.outlineColor = outlineColor;
            tmp.outlineWidth = outlineThickness;

            textObj.transform.rotation = attachedObjects.transform.rotation;
            textObj.transform.position = position - textObj.transform.forward * ValueHolder.distanceToDrawable.z * order;
            tmp.ForceMeshUpdate(true);
            MeshCollider meshCollider = textObj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = tmp.mesh;
     
            textObj.AddComponent<OrderInLayerValueHolder>().SetOrderInLayer(order);
            tmp.sortingOrder = order;
        }

        /// <summary>
        /// Writes a drawbale text on a drawable.
        /// The name for it is currently empty, because the setup method will create a unique one.
        /// </summary>
        /// <param name="drawable">Is the drawable on that the text should be displayed</param>
        /// <param name="text">The text that should be displayed</param>
        /// <param name="position">The inital position of the text object</param>
        /// <param name="fontColor">The chosen font color of the text.</param>
        /// <param name="outlineColor">The chosen outline color of the text.</param>
        /// <param name="outlineThickness">The chosen outline thickness of the text.</param>
        /// <param name="fontSize">The chosen font size of the text</param>
        /// <param name="order">The current order in layer</param>
        /// <param name="styles">The chosen font styles for the text</param>
        /// <returns>The created drawable text object</returns>
        public static GameObject WriteText(GameObject drawable, string text, Vector3 position, Color fontColor, Color outlineColor, float outlineThickness, float fontSize, int order, FontStyles styles)
        {
            Setup(drawable, "", text, position, fontColor, outlineColor, outlineThickness, fontSize, order, styles, out GameObject textObj);
            ValueHolder.currentOrderInLayer++;
            return textObj;
        }

        /// <summary>
        /// Rewrites a drawbale text on a drawable.
        /// </summary>
        /// <param name="drawable">Is the drawable on that the text should be displayed</param>
        /// <param name="id">The name of the drawable text</param>
        /// <param name="text">The text that should be displayed</param>
        /// <param name="position">The inital position of the text object</param>
        /// <param name="scale">The scale of the text object</param>
        /// <param name="eulerAngles">The euler angles of the text object</param>
        /// <param name="fontColor">The chosen font color of the text.</param>
        /// <param name="outlineColor">The chosen outline color of the text.</param>
        /// <param name="outlineThickness">The chosen outline thickness of the text.</param>
        /// <param name="fontSize">The chosen font size of the text</param>
        /// <param name="order">The current order in layer</param>
        /// <param name="styles">The chosen font styles for the text</param>
        /// <returns>The created drawable text object</returns>
        private static GameObject ReWriteText(GameObject drawable, string id, string text, Vector3 position, Vector3 scale, Vector3 eulerAngles, Color fontColor, Color outlineColor, 
            float outlineThickness, float fontSize, int order, FontStyles styles)
        {
            if (order >= ValueHolder.currentOrderInLayer)
            {
                ValueHolder.currentOrderInLayer = order + 1;
            }
            GameObject textObject;
            if (GameDrawableFinder.FindChild(drawable, id) != null)
            {
                textObject = GameDrawableFinder.FindChild(drawable, id);
                textObject.GetComponent<TextMeshPro>().sortingOrder = order;
            }
            else
            {
                Setup(drawable, id, text, position, fontColor, outlineColor, outlineThickness, fontSize, order, styles, out GameObject textObj);
                textObject = textObj;
                
            }
            textObject.transform.localScale = scale;
            textObject.transform.localEulerAngles = eulerAngles;
            textObject.transform.localPosition = position;
            textObject.GetComponent<OrderInLayerValueHolder>().SetOrderInLayer(order);
            return textObject;

        }

        /// <summary>
        /// Rewrites a given <see cref="Text"/> configuration.
        /// </summary>
        /// <param name="drawable">The drawable on that the text should be displayed.</param>
        /// <param name="text">The text configuration which contains the necressary values.</param>
        /// <returns>The created drawable text object</returns>
        public static GameObject ReWriteText(GameObject drawable, Text text)
        {
            return ReWriteText(drawable,
                text.id,
                text.text,
                text.position,
                text.scale,
                text.eulerAngles,
                text.fontColor,
                text.outlineColor,
                text.outlineThickness,
                text.fontSize,
                text.orderInLayer,
                text.fontStyles);
        }

        /// <summary>
        /// Refreshes the mesh collider of the game object.
        /// It's necressary because the mesh renderer needs some time to calculates the mesh.
        /// </summary>
        /// <param name="textObj">The object which contains the mesh collider.</param>
        public static void RefreshMeshCollider(GameObject textObj)
        {
            if (textObj.GetComponent<MeshCollider>() != null)
            {
                MeshCollider meshCollider = textObj.GetComponent<MeshCollider>();
                meshCollider.enabled = false;
                meshCollider.enabled = true;
            }
        }

    }
}