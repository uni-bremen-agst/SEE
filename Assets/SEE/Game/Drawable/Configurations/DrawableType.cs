using SEE.Net.Actions.Drawable;
using System;
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
        /// Gets the drawable type of the given object.
        /// </summary>
        /// <param name="obj">The object from which the drawable type is to be determined.</param>
        /// <returns>The drawable type</returns> 
        public static DrawableType Get(GameObject obj)
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
                case Tags.MindMapNode:
                    type = MindMapNodeConf.GetNodeConf(obj);
                    break;
                default:
                    type = null;
                    break;
            }
            return type;
        }

        /// <summary>
        /// Restores the object to the given drawable type configuration.
        /// </summary>
        /// <param name="type">The type to restore.</param>
        /// <param name="drawable">The drawable on which the drawable type should be restored.</param>
        public static void Restore(DrawableType type, GameObject drawable)
        {
            switch(type)
            {
                case LineConf line:
                    GameDrawer.ReDrawLine(drawable, line);
                    new DrawFreehandNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), 
                        line).Execute();
                    break;
                case TextConf text:
                    GameTexter.ReWriteText(drawable, text);
                    new WriteTextNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), 
                        text).Execute();
                    break;
                case ImageConf image:
                    GameImage.RePlaceImage(drawable, image);
                    new AddImageNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), 
                        image).Execute();
                    break;
                case MindMapNodeConf node:
                    GameMindMap.ReCreate(drawable, node);
                    new MindMapCreateNodeNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), 
                        node).Execute();
                    break;
                default:
                    Debug.Log("Can't restore " + type.id);
                    break;
            }
        }

        /// <summary>
        /// Get the prefix for the drawable type.
        /// </summary>
        /// <param name="type">The type for which the prefix is needed.</param>
        /// <returns>The determined prefix.</returns>
        public static string GetPrefix(DrawableType type)
        {
            switch (type)
            {
                case LineConf line:
                    return ValueHolder.LinePrefix;
                case TextConf text:
                    return ValueHolder.TextPrefix;
                case ImageConf image:
                    return ValueHolder.ImagePrefix;
                case MindMapNodeConf node:
                    if (node.id.StartsWith(ValueHolder.MindMapThemePrefix))
                    {
                        return ValueHolder.MindMapThemePrefix;
                    } else if (node.id.StartsWith(ValueHolder.MindMapSubthemePrefix))
                    {
                        return ValueHolder.MindMapSubthemePrefix;
                    } else
                    {
                        return ValueHolder.MindMapLeafPrefix;
                    }
            }
            return "";
        }

        /// <summary>
        /// Gets the sorting order for order by
        /// </summary>
        /// <param name="type">The current drawable type</param>
        /// <returns>The number of the order</returns>
        public static int OrderOnType(DrawableType type)
        {
            switch (type)
            {
                case LineConf line:
                    return 0;
                case TextConf text:
                    return 1;
                case ImageConf image:
                    return 2;
                case MindMapNodeConf node:
                    return 3;
                default:
                    return 4;
            }
        }

        /// <summary>
        /// For sorting the nodes.
        /// It is necessary because the nodes build on each other. 
        /// Therefore, the nodes with lower layers must be restored first.
        /// This method will be used in combination with <see cref="OrderOnType(DrawableType)"/>
        /// </summary>
        /// <param name="type">the drawable type of the chosen object</param>
        /// <returns>the order.</returns>
        public static int OrderMindMap(DrawableType type)
        {
            switch (type)
            {
                case MindMapNodeConf node:
                    return node.layer;
                default:
                    return 0;
            }
        }
    }
}