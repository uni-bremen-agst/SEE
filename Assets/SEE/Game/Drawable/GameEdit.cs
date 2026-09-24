using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Editing;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class manages the editing of a <see cref="DrawableType"/>.
    /// </summary>
    public static class GameEdit
    {
        /// <summary>
        /// This method changes the image color of a image.
        /// </summary>
        /// <param name="imageObj">The image object whose image should be changed.</param>
        /// <param name="color">The new color for the image.</param>
        public static void ChangeImageColor(GameObject imageObj, Color color)
        {
            GameImageEdit.ChangeImageColor(imageObj, color);
        }

        /// <summary>
        /// This method changes all editable values of a drawable image at once.
        /// </summary>
        /// <param name="imageObj">The image object whose values should be changed.</param>
        /// <param name="conf">The configuration which holds the necessary values.</param>
        public static void ChangeImage(GameObject imageObj, ImageConf conf)
        {
            GameImageEdit.ChangeImage(imageObj, conf);
        }

        /// <summary>
        /// This method changes all editable values of a drawable mind map node at once.
        /// </summary>
        /// <param name="node">The node object whose values should be changed.</param>
        /// <param name="conf">The configuration which holds the necessary values.</param>
        public static void ChangeMindMapNode(GameObject node, MindMapNodeConf conf)
        {
            GameMindMapEdit.ChangeMindMapNode(node, conf);
        }
    }
}