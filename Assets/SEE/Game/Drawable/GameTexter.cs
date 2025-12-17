using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class is responsible for creating the game objects for the written texts.
    /// </summary>
    public static class GameTexter
    {
        /// <summary>
        /// Name of the font for the text.
        /// </summary>
        private const string drawableTextFontName = "Fonts/DrawableTextFont";

        /// <summary>
        /// The shader keyword for the outline color.
        /// </summary>
        public static readonly string OutlineKeyWord = "OUTLINE_ON";

        /// <summary>
        /// Calculates the text width based on the first line.
        /// For the calculation it removes the HTML Tags and checks if there are active font styles for bold, upper case or small caps.
        /// This font styles affects the width.
        ///
        /// Method based on the method from Stephan_B's comment in
        /// https://forum.unity.com/threads/calculate-width-of-a-text-before-without-assigning-it-to-a-tmp-object.758867/#post-5057900
        /// </summary>
        /// <param name="text">the first line of the text.</param>
        /// <param name="fontAsset">the used font asset.</param>
        /// <param name="fontSize">the used font size.</param>
        /// <param name="style">the used font styles.</param>
        /// <returns>The calculated width for the first line of the text.</returns>
        private static float TextWidthApproximation(string text, TMP_FontAsset fontAsset,
            float fontSize, FontStyles style)
        {
            string result = RichTextRemover.RemoveRichText(text);

            text = text.ToLower();
            /// The text characters are widened by bold, uppercase, or small caps.
            bool htmlBold = text.Contains("<b>");
            bool htmlUpperCase = text.Contains("<uppercase>");
            bool htmlSmallCaps = text.Contains("<smallcaps>");

            /// Compute scale of the target point size relative to the sampling point size of the font asset.
            float pointSizeScale = fontSize / (fontAsset.faceInfo.pointSize * fontAsset.faceInfo.scale * 10);
            float emScale = fontSize * 0.001f;

            float styleSpacingAdjustment = (style & FontStyles.Bold) == FontStyles.Bold
                || htmlBold ? fontAsset.boldSpacing : 0;
            float normalSpacingAdjustment = fontAsset.normalSpacingOffset;
            float width = 0;

            /// If the text would originally be in uppercase, write it back in uppercase.
            if ((style & FontStyles.UpperCase) != 0 || (style & FontStyles.SmallCaps) != 0
                || htmlUpperCase || htmlSmallCaps)
            {
                result = result.ToUpper();
            }

            /// Calculates the width based on every character of the first line.
            /// It takes into account whether the text is bold, uppercase, or in small caps.
            for (int i = 0; i < result.Length; i++)
            {
                /// Makes sure the given unicode exists in the font asset.
                if (fontAsset.characterLookupTable.TryGetValue(result[i], out TMP_Character character))
                {
                    width += character.glyph.metrics.horizontalAdvance * pointSizeScale +
                        (styleSpacingAdjustment + normalSpacingAdjustment) * emScale;
                }
            }

            return width;
        }

        /// <summary>
        /// This method calculates the width and height of the text.
        /// The width depends on the longest line of the text.
        /// The height is approximated by the number of lines multiplied by 0.1f and the fontSize.
        /// </summary>
        /// <param name="text">The written text.</param>
        /// <param name="fontAsset">The font asset of the text</param>
        /// <param name="fontSize">The font size of the text</param>
        /// <param name="styles">The font styles of the text</param>
        /// <returns>calculated width and height</returns>
        public static Vector2 CalculateWidthAndHeight(string text, TMP_FontAsset fontAsset,
            float fontSize, FontStyles styles)
        {
            string[] split = text.Split(new string[]{ "\n", "<br>"}, System.StringSplitOptions.None);
            float x = split.ToList().Max(s => TextWidthApproximation(s, fontAsset, fontSize, styles));
            float y = split.Count() * 0.1f * fontSize;
            return new Vector2(x, y);
        }

        /// <summary>
        /// This method creates the inital drawable text object.
        /// </summary>
        /// <param name="surface">The drawable surface on which the text should be displayed</param>
        /// <param name="name">The name of the text object.</param>
        /// <param name="text">The text that should be displayed.</param>
        /// <param name="position">The inital position of the text object.</param>
        /// <param name="fontColor">The chosen font color of the text.</param>
        /// <param name="outlineColor">The chosen outline color of the text.</param>
        /// <param name="outlineThickness">The chosen outline thickness of the text.</param>
        /// <param name="fontSize">The chosen font size of the text.</param>
        /// <param name="order">The current order in layer.</param>
        /// <param name="styles">The chosen font styles for the text.</param>
        /// <param name="associatedPage">The assoiated surface page for this object.</param>
        /// <param name="textObj">The created drawable text object.</param>
        private static void Setup(GameObject surface, string name, string text, Vector3 position,
            Color fontColor, Color outlineColor, bool outlineStatus, float outlineThickness,
            float fontSize, int order, FontStyles styles, int associatedPage,
            out GameObject textObj)
        {
            /// If the object has been created earlier, it already has a name,
            /// and this name is taken from the parameters <paramref name="name"/>.
            if (name.Length > Tags.DText.Length)
            {
                textObj = new(name);
            }
            else
            {
                /// Otherwise, a name for the text will be generated.
                /// For this, the <see cref="ValueHolder.TextPrefix"/> is concatenated with
                /// the object ID along with a random string consisting of four characters.
                textObj = new("");

                name = ValueHolder.TextPrefix + textObj.GetInstanceID() + RandomStrings.GetRandomString(4);
                /// Check if the name is already in use. If so, generate a new name.
                while (GameFinder.FindChild(surface, name) != null)
                {
                    name = ValueHolder.TextPrefix + textObj.GetInstanceID() + RandomStrings.GetRandomString(4);
                }
                textObj.name = name;
            }
            /// Sets up the drawable holder <see cref="DrawableSetupManager"/>.
            DrawableSetupManager.Setup(surface, out _, out GameObject attachedObjects);

            textObj.tag = Tags.DText;

            /// Add the text object to the hierarchy below the attached objects - object of the drawable.
            textObj.transform.SetParent(attachedObjects.transform);

            /// Adds a <see cref="TextMeshPro"/> component to the text object. It holds the text itself.
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();

            /// Sets the values for the text and calculates the rect tranform size.
            tmp.text = text;
            tmp.fontStyle = styles;
            tmp.font = Resources.Load<TMP_FontAsset>(drawableTextFontName);
            tmp.rectTransform.sizeDelta = CalculateWidthAndHeight(text, tmp.font, fontSize, styles);
            tmp.color = fontColor;
            tmp.faceColor = fontColor;
            tmp.fontSize = fontSize;
            tmp.outlineColor = outlineColor;
            tmp.outlineWidth = outlineThickness;
            tmp.alignment = TextAlignmentOptions.Center;

            /// /// Adopt the rotation of the attached object.
            textObj.transform.rotation = attachedObjects.transform.rotation;

            /// Calculates the position and preserve the distance.
            textObj.transform.position = position - textObj.transform.forward * ValueHolder.DistanceToDrawable.z * order;

            /// Forces the updated of the <see cref="TextMeshPro"/>'s mesh.
            tmp.ForceMeshUpdate(true);

            /// Adds a mesh collider and sets the calculates mesh of the Text Mesh Pro.
            MeshCollider meshCollider = textObj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = tmp.mesh;

            /// Adds the order in layer value holder component to the text object and sets the order.
            textObj.AddComponent<OrderInLayerValueHolder>().OrderInLayer = order;
            /// The Text Mesh Pro needs also the order.
            tmp.sortingOrder = order;

            /// Adds a <see cref="AssociatedPageHolder"/> component.
            /// And sets the associated page to the used page.
            textObj.AddComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;
            if (associatedPage != surface.GetComponent<DrawableHolder>().CurrentPage)
            {
                textObj.SetActive(false);
            }

            /// Is needed to fix an issue in the <see cref="TextMeshPro"/> component.
            /// If the outline color is set to black during creation; it is strangely always set to white.
            tmp.outlineColor = tmp.outlineColor;

            /// Enables or disables the outline color.
            ChangeOutlineStatus(textObj, outlineStatus);
        }

        /// <summary>
        /// Writes a drawbale text on a drawable.
        /// The name for it is currently empty, because the setup method will create a unique one.
        /// </summary>
        /// <param name="surface">The drawable surface on which the text should be displayed</param>
        /// <param name="text">The text that should be displayed</param>
        /// <param name="position">The inital position of the text object</param>
        /// <param name="fontColor">The chosen font color of the text.</param>
        /// <param name="outlineColor">The chosen outline color of the text.</param>
        /// <param name="outlineThickness">The chosen outline thickness of the text.</param>
        /// <param name="fontSize">The chosen font size of the text</param>
        /// <param name="order">The current order in layer</param>
        /// <param name="styles">The chosen font styles for the text</param>
        /// <returns>The created drawable text object</returns>
        public static GameObject WriteText(GameObject surface, string text, Vector3 position,
            Color fontColor, Color outlineColor, bool outlineStatus, float outlineThickness, float fontSize, int order, FontStyles styles)
        {
            Setup(surface, "", text, position, fontColor, outlineColor, outlineStatus, outlineThickness, fontSize,
                order, styles, surface.GetComponent<DrawableHolder>().CurrentPage, out GameObject textObj);
            surface.GetComponent<DrawableHolder>().Inc();
            ValueHolder.MaxOrderInLayer++;

            /// Is needed to fix an issue in the <see cref="TextMeshPro"/> component.
            /// If the outline color is set to black during creation, it is strangely always set to white.
            GameEdit.ChangeOutlineColor(textObj, outlineColor);
            return textObj;
        }

        /// <summary>
        /// Rewrites a drawbale text on a drawable.
        /// </summary>
        /// <param name="surface">The drawable surface on which the text should be displayed</param>
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
        private static GameObject ReWriteText(GameObject surface, string id, string text, Vector3 position,
            Vector3 scale, Vector3 eulerAngles, Color fontColor, Color outlineColor, bool outlineStatus,
            float outlineThickness, float fontSize, int order, FontStyles styles, int associatedPage)
        {
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            /// Adjusts the current order in the layer if the
            /// order in layer for the line is greater than or equal to it.
            if (order >= holder.OrderInLayer && associatedPage == holder.CurrentPage)
            {
                holder.OrderInLayer = order + 1;
            }
            if (associatedPage >= holder.MaxPageSize)
            {
                holder.MaxPageSize = associatedPage + 1;
            }
            if (order >= ValueHolder.MaxOrderInLayer)
            {
                ValueHolder.MaxOrderInLayer = order + 1;
            }

            GameObject textObject;

            /// Tries to find the text on the drawable.
            if (GameFinder.FindChild(surface, id) != null)
            {
                textObject = GameFinder.FindChild(surface, id);
                textObject.GetComponent<TextMeshPro>().sortingOrder = order;
            }
            else
            {
                /// Creates the text object.
                Setup(surface, id, text, position, fontColor, outlineColor, outlineStatus, outlineThickness, fontSize, order,
                    styles, associatedPage, out GameObject textObj);
                textObject = textObj;

            }

            /// Restores the old values.
            textObject.transform.localScale = scale;
            textObject.transform.localEulerAngles = eulerAngles;
            textObject.transform.localPosition = position;
            textObject.GetComponent<OrderInLayerValueHolder>().OrderInLayer = order;
            textObject.GetComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;

            /// Is needed to fix an issue in the <see cref="TextMeshPro"/> component.
            /// If the outline color is set to black during creation; it is strangely always set to white.
            GameEdit.ChangeOutlineColor(textObject, outlineColor);

            return textObject;
        }

        /// <summary>
        /// Rewrites a given <see cref="TextConf"/> configuration.
        /// </summary>
        /// <param name="surface">The drawable surface on which the text should be displayed.</param>
        /// <param name="text">The text configuration which contains the necessary values.</param>
        /// <returns>The created drawable text object</returns>
        public static GameObject ReWriteText(GameObject surface, TextConf text)
        {
            return ReWriteText(surface,
                text.ID,
                text.Text,
                text.Position,
                text.Scale,
                text.EulerAngles,
                text.FontColor,
                text.OutlineColor,
                text.IsOutlined,
                text.OutlineThickness,
                text.FontSize,
                text.OrderInLayer,
                text.FontStyles,
                text.AssociatedPage);
        }

        /// <summary>
        /// Refreshes the mesh collider of the game object.
        /// It's necessary because the mesh renderer needs some time to calculates the mesh.
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

        /// <summary>
        /// Changes the status (enabled or disabled) of the outline color.
        /// </summary>
        /// <param name="textObj">The text object which outline status should be changed.</param>
        /// <param name="status">The new status for the outline color.</param>
        public static void ChangeOutlineStatus(GameObject textObj, bool status)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
                LocalKeyword outlineKeyword = new(tmp.fontMaterial.shader, OutlineKeyWord);
                tmp.fontMaterial.SetKeyword(outlineKeyword, status);
            }
        }
    }
}