using SEE.Net.Actions.Drawable;
using SEE.Utils;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class of the drawable types.
    /// </summary>
    [Serializable]
    public abstract class DrawableType
    {
        /// <summary>
        /// The name of the drawable type object.
        /// </summary>
        public string id;

        /// <summary>
        /// The position of the drawable type object.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The euler angles of the drawable type object.
        /// </summary>
        public Vector3 eulerAngles;

        /// <summary>
        /// The scale of the text.
        /// </summary>
        public Vector3 scale;

        /// <summary>
        /// The order in layer for this drawable type object.
        /// </summary>
        public int orderInLayer;

        /// <summary>
        /// Label in the configuration file for the id of a line.
        /// </summary>
        private const string IDLabel = "IDLabel";

        /// <summary>
        /// Label in the configuration file for the position of a line.
        /// </summary>
        private const string PositionLabel = "PositionLabel";

        /// <summary>
        /// Label in the configuration file for the scale of a line.
        /// </summary>
        private const string ScaleLabel = "ScaleLabel";

        /// <summary>
        /// Label in the configuration file for the order in layer of a line.
        /// </summary>
        private const string OrderInLayerLabel = "OrderInLayerLabel";

        /// <summary>
        /// Label in the configuration file for the euler angles of a line.
        /// </summary>
        private const string EulerAnglesLabel = "EulerAnglesLabel";

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
        /// It calls the corresponding re-creation method of the respective drawable type.
        /// </summary>
        /// <param name="type">The type to restore.</param>
        /// <param name="drawable">The drawable on which the drawable type should be restored.</param>
        public static void Restore(DrawableType type, GameObject drawable)
        {
            switch(type)
            {
                case LineConf line:
                    GameDrawer.ReDrawLine(drawable, line);
                    new DrawNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), 
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
        /// Edits the object to the given drawable type configuration.
        /// It calls the corresponding <see cref="GameEdit"/> - Change method of the respective drawable type.
        /// </summary>
        /// <param name="objectToEdit">The object to be edit.</param>
        /// <param name="type">The drawable type configuration that should be applied.</param>
        /// <param name="drawable">The drawable on that the object is displayed.</param>
        public static void Edit(GameObject objectToEdit, DrawableType type, GameObject drawable) 
        {
            string drawableParent = GameFinder.GetDrawableParentName(drawable);
            switch(type)
            {
                case LineConf line:
                    GameEdit.ChangeLine(objectToEdit, line);
                    new EditLineNetAction(drawable.name, drawableParent, line).Execute();
                    break;
                case TextConf text:
                    GameEdit.ChangeText(objectToEdit, text);
                    new EditTextNetAction(drawable.name, drawableParent, text).Execute();
                    break;
                case ImageConf image:
                    GameEdit.ChangeImage(objectToEdit, image);
                    new EditImageNetAction(drawable.name, drawableParent, image).Execute();
                    break;
                case MindMapNodeConf node:
                    GameEdit.ChangeMindMapNode(objectToEdit, node);
                    new EditMMNodeNetAction(drawable.name, drawableParent, node).Execute();
                    break;
                default:
                    Debug.Log("Can't edit " + type.id);
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

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        internal virtual void Save(ConfigWriter writer)
        {
            writer.Save(id, IDLabel);
            writer.Save(position, PositionLabel);
            writer.Save(eulerAngles, EulerAnglesLabel);
            writer.Save(scale, ScaleLabel);
            writer.Save(orderInLayer, OrderInLayerLabel);
        }

        /// <summary>
        /// Given the representation of a <see cref="DrawableType"/> as created by the <see cref="ConfigWriter"/>, this
        /// method parses the attributes from that representation and puts them into this <see cref="DrawableType"/>
        /// instance.
        /// </summary>
        /// <param name="attributes">A list of labels (strings) of attributes and their values (objects). This
        /// has to be the representation of a <see cref="DrawableType"/> as created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the <see cref="DrawableType"/> was loaded without errors.</returns>
        internal virtual bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = false;

            /// Try to restores the id.
            if (attributes.TryGetValue(IDLabel, out object name))
            {
                id = (string)name;
            }
            else
            {
                errors = true;
            }

            /// Try to restores the position.
            Vector3 loadedPosition = Vector3.zero;
            if (ConfigIO.Restore(attributes, PositionLabel, ref loadedPosition))
            {
                position = loadedPosition;
            }
            else
            {
                position = Vector3.zero;
                errors = true;
            }

            /// Try to restores the euler angles.
            Vector3 loadedEulerAngles = Vector3.zero;
            if (ConfigIO.Restore(attributes, EulerAnglesLabel, ref loadedEulerAngles))
            {
                eulerAngles = loadedEulerAngles;
            }
            else
            {
                eulerAngles = Vector3.zero;
                errors = true;
            }

            /// Try to restores the scale.
            Vector3 loadedScale = Vector3.zero;
            if (ConfigIO.Restore(attributes, ScaleLabel, ref loadedScale))
            {
                scale = loadedScale;
            }
            else
            {
                scale = Vector3.zero;
                errors = true;
            }

            /// Try to restores the order in layer.
            if (!ConfigIO.Restore(attributes, OrderInLayerLabel, ref orderInLayer))
            {
                errors = true;
            }

            return errors;
        }
    }
}