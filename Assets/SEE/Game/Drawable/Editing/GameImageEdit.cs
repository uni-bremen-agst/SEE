using SEE.Game.Drawable.Configurations;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.Drawable.Editing
{
    /// <summary>
    /// Provides editing operations for drawable images.
    /// </summary>
    internal static class GameImageEdit
    {
        /// <summary>
        /// Changes the color of a drawable image.
        /// </summary>
        /// <param name="imageObj">The image whose color should be changed.</param>
        /// <param name="color">The new image color.</param>
        internal static void ChangeImageColor(GameObject imageObj, Color color)
        {
            if (imageObj.CompareTag(Tags.Image))
            {
                imageObj.GetComponent<Image>().color = color;
            }
        }

        /// <summary>
        /// Changes all editable values of a drawable image.
        /// </summary>
        /// <param name="imageObj">The image whose values should be changed.</param>
        /// <param name="conf">Contains the new values.</param>
        internal static void ChangeImage(GameObject imageObj, ImageConf conf)
        {
            if (imageObj.CompareTag(Tags.Image))
            {
                GameLayerChanger.SetOrderInLayer(imageObj, conf.OrderInLayer);
                ChangeImageColor(imageObj, conf.ImageColor);
                GameMoveRotator.SetRotateY(imageObj, conf.EulerAngles.y);
            }
        }
    }
}
