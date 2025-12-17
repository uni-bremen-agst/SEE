using SEE.Net.Actions.Drawable;
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
        public string ID;

        /// <summary>
        /// The position of the drawable type object.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The euler angles of the drawable type object.
        /// </summary>
        public Vector3 EulerAngles;

        /// <summary>
        /// The scale of the text.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// The order in layer for this drawable type object.
        /// </summary>
        public int OrderInLayer;

        /// <summary>
        /// The associated surface page for this drawable type object.
        /// </summary>
        public int AssociatedPage;

        /// <summary>
        /// Gets the drawable type of the given object.
        /// </summary>
        /// <param name="obj">The object from which the drawable type is to be determined.</param>
        /// <returns>The drawable type</returns>
        public static DrawableType Get(GameObject obj)
        {
            DrawableType type = obj.tag switch
            {
                Tags.Line => LineConf.GetLine(obj),
                Tags.DText => TextConf.GetText(obj),
                Tags.Image => ImageConf.GetImageConf(obj),
                Tags.MindMapNode => MindMapNodeConf.GetNodeConf(obj),
                _ => null,
            };
            return type;
        }

        /// <summary>
        /// Edits the object to the given drawable type configuration.
        /// It calls the corresponding <see cref="GameEdit"/> - change method of the respective
        /// drawable type.
        /// </summary>
        /// <param name="objectToEdit">The object to be edited.</param>
        /// <param name="type">The drawable type configuration that should be applied.</param>
        /// <param name="surface">The drawable surface on which the object is displayed.</param>
        public static void Edit(GameObject objectToEdit, DrawableType type, GameObject surface)
        {
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
            switch(type)
            {
                case LineConf line:
                    GameEdit.ChangeLine(objectToEdit, line);
                    new EditLineNetAction(surface.name, surfaceParentName, LineConf.GetLineWithoutRenderPos(objectToEdit)).Execute();
                    break;
                case TextConf text:
                    GameEdit.ChangeText(objectToEdit, text);
                    new EditTextNetAction(surface.name, surfaceParentName, text).Execute();
                    break;
                case ImageConf image:
                    GameEdit.ChangeImage(objectToEdit, image);
                    new EditImageNetAction(surface.name, surfaceParentName, image).Execute();
                    break;
                case MindMapNodeConf node:
                    GameEdit.ChangeMindMapNode(objectToEdit, node);
                    new EditMMNodeNetAction(surface.name, surfaceParentName, node).Execute();
                    break;
                default:
                    Debug.Log($"Can't edit {type.ID}.\n");
                    break;
            }
        }

        /// <summary>
        /// Returns the prefix for the drawable type.
        /// </summary>
        /// <param name="type">The type for which the prefix is needed.</param>
        /// <returns>The determined prefix.</returns>
        public static string GetPrefix(DrawableType type)
        {
            switch (type)
            {
                case LineConf:
                    return ValueHolder.LinePrefix;
                case TextConf:
                    return ValueHolder.TextPrefix;
                case ImageConf:
                    return ValueHolder.ImagePrefix;
                case MindMapNodeConf node:
                    if (node.ID.StartsWith(ValueHolder.MindMapThemePrefix))
                    {
                        return ValueHolder.MindMapThemePrefix;
                    }
                    else if (node.ID.StartsWith(ValueHolder.MindMapSubthemePrefix))
                    {
                        return ValueHolder.MindMapSubthemePrefix;
                    }
                    else
                    {
                        return ValueHolder.MindMapLeafPrefix;
                    }
            }
            return "";
        }

        /// <summary>
        /// Gets the sorting order for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The current drawable type</param>
        /// <returns>The number of the order</returns>
        public static int OrderOnType(DrawableType type)
        {
            return type switch
            {
                LineConf => 0,
                TextConf => 1,
                ImageConf => 2,
                MindMapNodeConf => 3,
                _ => 4,
            };
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
            return type switch
            {
                MindMapNodeConf node => node.Layer,
                _ => 0,
            };
        }

        #region Config I/O

        /// <summary>
        /// Label in the configuration file for the ID.
        /// </summary>
        private const string idLabel = "IDLabel";

        /// <summary>
        /// Label in the configuration file for the position.
        /// </summary>
        private const string positionLabel = "PositionLabel";

        /// <summary>
        /// Label in the configuration file for the scale.
        /// </summary>
        private const string scaleLabel = "ScaleLabel";

        /// <summary>
        /// Label in the configuration file for the order in layer.
        /// </summary>
        private const string orderInLayerLabel = "OrderInLayerLabel";

        /// <summary>
        /// Label in the configuration file for the euler angles.
        /// </summary>
        private const string eulerAnglesLabel = "EulerAnglesLabel";

        /// <summary>
        /// Label in the configuration file for the associated surface page.
        /// </summary>
        private const string associatedPageLabel = "AssociatedPageLabel";

        /// <summary>
        /// Restores the object to the given drawable type configuration.
        /// It calls the corresponding re-creation method of the respective drawable type.
        /// </summary>
        /// <param name="type">The type to restore.</param>
        /// <param name="surface">The drawable surface on which the drawable type should be restored.</param>
        public static GameObject Restore(DrawableType type, GameObject surface)
        {
            GameObject createdObject = null;
            switch (type)
            {
                case LineConf line:
                    createdObject = GameDrawer.ReDrawLine(surface, line);
                    EnsureValidity(type, createdObject);
                    new DrawNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                        line).Execute();
                    break;
                case TextConf text:
                    createdObject = GameTexter.ReWriteText(surface, text);
                    EnsureValidity(type, createdObject);
                    new WriteTextNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                        text).Execute();
                    break;
                case ImageConf image:
                    createdObject = GameImage.RePlaceImage(surface, image);
                    EnsureValidity(type, createdObject);
                    new AddImageNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                        image).Execute();
                    break;
                case MindMapNodeConf node:
                    createdObject = GameMindMap.ReCreate(surface, node);
                    EnsureValidity(type, createdObject);
                    new MindMapCreateNodeNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                        node).Execute();
                    break;
                default:
                    Debug.Log($"Can't restore {type.ID}.\n");
                    break;
            }
            return createdObject;
        }

        /// <summary>
        /// Ensures that the configuration gets the name of the created object.
        /// </summary>
        /// <param name="type">The configuration.</param>
        /// <param name="createdObject">The created object.</param>
        /// <returns>The modified configuration.</returns>
        private static DrawableType EnsureValidity(DrawableType type, GameObject createdObject)
        {
            if (type.ID == "")
            {
                type.ID = createdObject.name;
            }
            return type;
        }

        /// <summary>
        /// Saves this instance's attributes using the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes.</param>
        internal virtual void Save(ConfigWriter writer)
        {
            writer.BeginGroup();
            writer.Save(ID, idLabel);
            writer.Save(AssociatedPage, associatedPageLabel);
            writer.Save(Position, positionLabel);
            writer.Save(EulerAngles, eulerAnglesLabel);
            writer.Save(Scale, scaleLabel);
            writer.Save(OrderInLayer, orderInLayerLabel);
            SaveAttributes(writer);
            writer.EndGroup();
        }

        /// <summary>
        /// Subclasses must implement this so save their attributes. This class takes
        /// care only to begin and end the grouping and to emit the key-value pair
        /// for the 'kind'.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        protected abstract void SaveAttributes(ConfigWriter writer);

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

            /// Try to restore the ID.
            if (attributes.TryGetValue(idLabel, out object name))
            {
                ID = (string)name;
            }
            else
            {
                errors = true;
            }

            /// Try to restore the associated page.
            if (!ConfigIO.Restore(attributes, associatedPageLabel, ref AssociatedPage))
            {
                errors = true;
            }

            /// Try to restore the position.
            Vector3 loadedPosition = Vector3.zero;
            if (ConfigIO.Restore(attributes, positionLabel, ref loadedPosition))
            {
                Position = loadedPosition;
            }
            else
            {
                Position = Vector3.zero;
                errors = true;
            }

            /// Try to restore the euler angles.
            Vector3 loadedEulerAngles = Vector3.zero;
            if (ConfigIO.Restore(attributes, eulerAnglesLabel, ref loadedEulerAngles))
            {
                EulerAngles = loadedEulerAngles;
            }
            else
            {
                EulerAngles = Vector3.zero;
                errors = true;
            }

            /// Try to restore the scale.
            Vector3 loadedScale = Vector3.zero;
            if (ConfigIO.Restore(attributes, scaleLabel, ref loadedScale))
            {
                Scale = loadedScale;
            }
            else
            {
                Scale = Vector3.zero;
                errors = true;
            }

            /// Try to restore the order in layer.
            if (!ConfigIO.Restore(attributes, orderInLayerLabel, ref OrderInLayer))
            {
                errors = true;
            }

            return errors;
        }

        #endregion
    }
}