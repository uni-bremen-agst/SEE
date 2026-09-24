using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Editing;
using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.Game.Drawable.Text
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
        /// This method creates the inital drawable text object.
        /// </summary>
        /// <param name="surface">The drawable surface on which the text should be displayed.</param>
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
                while (GameFinder.FindAttachedOrLocalDescendant(surface, name) != null)
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
            tmp.rectTransform.sizeDelta = GameTextGeometry.CalculateWidthAndHeight(text, tmp.font, fontSize, styles);
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
            GameTextEdit.ChangeOutlineStatus(textObj, outlineStatus);
        }

        /// <summary>
        /// Writes a drawbale text on a drawable.
        /// The name for it is currently empty, because the setup method will create a unique one.
        /// </summary>
        /// <param name="surface">The drawable surface on which the text should be displayed.</param>
        /// <param name="text">The text that should be displayed.</param>
        /// <param name="position">The inital position of the text object.</param>
        /// <param name="fontColor">The chosen font color of the text.</param>
        /// <param name="outlineColor">The chosen outline color of the text.</param>
        /// <param name="outlineThickness">The chosen outline thickness of the text.</param>
        /// <param name="fontSize">The chosen font size of the text.</param>
        /// <param name="order">The current order in layer.</param>
        /// <param name="styles">The chosen font styles for the text.</param>
        /// <returns>The created drawable text object.</returns>
        public static GameObject WriteText(GameObject surface, string text, Vector3 position,
            Color fontColor, Color outlineColor, bool outlineStatus, float outlineThickness, float fontSize, int order, FontStyles styles)
        {
            Setup(surface, "", text, position, fontColor, outlineColor, outlineStatus, outlineThickness, fontSize,
                order, styles, surface.GetComponent<DrawableHolder>().CurrentPage, out GameObject textObj);
            surface.GetComponent<DrawableHolder>().Inc();
            ValueHolder.MaxOrderInLayer++;

            /// Is needed to fix an issue in the <see cref="TextMeshPro"/> component.
            /// If the outline color is set to black during creation, it is strangely always set to white.
            GameTextEdit.ChangeOutlineColor(textObj, outlineColor);
            return textObj;
        }

        /// <summary>
        /// Rewrites a drawbale text on a drawable.
        /// </summary>
        /// <param name="surface">The drawable surface on which the text should be displayed.</param>
        /// <param name="id">The name of the drawable text.</param>
        /// <param name="text">The text that should be displayed.</param>
        /// <param name="position">The inital position of the text object.</param>
        /// <param name="scale">The scale of the text object.</param>
        /// <param name="eulerAngles">The euler angles of the text object.</param>
        /// <param name="fontColor">The chosen font color of the text.</param>
        /// <param name="outlineColor">The chosen outline color of the text.</param>
        /// <param name="outlineThickness">The chosen outline thickness of the text.</param>
        /// <param name="fontSize">The chosen font size of the text.</param>
        /// <param name="order">The current order in layer.</param>
        /// <param name="styles">The chosen font styles for the text.</param>
        /// <returns>The created drawable text object.</returns>
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
            if (GameFinder.FindAttachedOrLocalDescendant(surface, id) != null)
            {
                textObject = GameFinder.FindAttachedOrLocalDescendant(surface, id);
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
            GameTextEdit.ChangeOutlineColor(textObject, outlineColor);

            return textObject;
        }

        /// <summary>
        /// Rewrites a given <see cref="TextConf"/> configuration.
        /// </summary>
        /// <param name="surface">The drawable surface on which the text should be displayed.</param>
        /// <param name="text">The text configuration which contains the necessary values.</param>
        /// <returns>The created drawable text object.</returns>
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
    }
}
