using System;
using System.Collections;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class of the drawable types.
    /// </summary>
    [Serializable]
    public class DrawableType
    {
        /// <summary>
        /// The name of the line.
        /// </summary>
        public string id;

        /// <summary>
        /// This class gets the drawable type of the given object.
        /// </summary>
        /// <param name="obj">The object from which the drawable type is to be determined.</param>
        /// <returns>The drawable type</returns> 
        public DrawableType Get(GameObject obj)
        {
            DrawableType type;
            switch(obj.tag)
            {
                case Tags.Line:
                    type = LineConf.GetLine(obj);
                    break;
                case Tags.DText:
                    type = TextConf.GetText(obj);
                    break;
                case Tags.Image:
                    type = ImageConf.GetImageConf(obj);
                    break;
                default:
                    type = null;
                    break;
            }
            return type;
        }
    }
}