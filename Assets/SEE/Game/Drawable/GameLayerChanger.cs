using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Notification;
using System;
using TMPro;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class allows to change the order in layer of a <see cref="DrawableType"/>
    /// The <see cref="DrawableType"/> needs a <see cref="OrderInLayerValueHolder"/> component.
    /// </summary>
    public static class GameLayerChanger
    {
        /// <summary>
        /// The state of the layer changer.
        /// Increase when the order in layer was increased.
        /// Decrease when the order in layer was decreased.
        /// </summary>
        [Serializable]
        public enum LayerChangerStates
        {
            Increase,
            Decrease
        }

        /// <summary>
        /// Increase the order in layer of an object with a <see cref="OrderInLayerValueHolder"/> component.
        /// </summary>
        /// <param name="obj">The object whose order in layer is to be changed</param>
        /// <param name="order">The new order of the object.</param>
        /// <param name="showInformation">show a notification when the limit is reached</param>
        /// <param name="useWorldPos">use world position for moving, only needed for sticky note</param>
        public static void Increase(GameObject obj, int order, bool showInformation = true, bool useWorldPos = false)
        {
            if (obj.GetComponent<OrderInLayerValueHolder>() != null 
                || obj.GetComponentInChildren<OrderInLayerValueHolder>() != null)
            {
                OrderInLayerValueHolder holder = obj.GetComponent<OrderInLayerValueHolder>() != null ?
                    obj.GetComponent<OrderInLayerValueHolder>() : obj.GetComponentInChildren<OrderInLayerValueHolder>();
                /// Block that is executed when the desired order in layer exceeds the maximum or is equal.
                if (holder.GetOrderInLayer() >= ValueHolder.currentOrderInLayer)
                {
                    if (showInformation)
                    {
                        ShowNotification.Warn("Maximum layer order", obj.name + 
                            " has reached the maximum layer order: " + holder.GetOrderInLayer());
                    }
                }
                else
                {
                    /// Block to execute if the desired order in the layer is less than the maximum.
                    holder.SetOrderInLayer(order);

                    if (showInformation)
                    {
                        ShowNotification.Info("Increases the order",
                            "The order in layer of the chosen object increases to " + holder.GetOrderInLayer() + ".", 0.8f);
                    }
                    /// For mind map nodes, it's important that the text is also assigned the order.
                    if (obj.CompareTag(Tags.MindMapNode))
                    {
                        obj.GetComponentInChildren<TextMeshPro>().sortingOrder = order;
                    }

                    /// Moves the object along the z-axis.
                    MoveObjectZ(obj, order, useWorldPos);

                    /// Sets the order on the Canvas and TextMeshPro components, if present.
                    if (obj.GetComponent<TextMeshPro>() != null)
                    {
                        obj.GetComponent<TextMeshPro>().sortingOrder = order;
                    }
                    if (obj.GetComponent<Canvas>() != null)
                    {
                        obj.GetComponent<Canvas>().sortingOrder = order;
                    }
                }
            }
        }

        /// <summary>
        /// Decrease the order in layer of an object with a <see cref="OrderInLayerValueHolder"/> component.
        /// </summary>
        /// <param name="obj">The object whose order in layer is to be changed</param>
        /// <param name="order">The new order of the object.</param>
        /// <param name="showInformation">show a notification when the limit is reached</param>
        /// <param name="useWorldPos">use world position for moving, only needed for sticky note</param>
        public static void Decrease(GameObject obj, int order, bool showInformation = true, bool useWorldPos = false)
        {
            if (obj.GetComponent<OrderInLayerValueHolder>() != null 
                || obj.GetComponentInChildren<OrderInLayerValueHolder>() != null)
            {
                OrderInLayerValueHolder holder = obj.GetComponent<OrderInLayerValueHolder>() != null ?
                    obj.GetComponent<OrderInLayerValueHolder>() : obj.GetComponentInChildren<OrderInLayerValueHolder>();
                /// Block that is executed if the minimum would be undercut.
                if (holder.GetOrderInLayer() == 0)
                {
                    if (showInformation)
                    {
                        ShowNotification.Warn("Minimum layer order", obj.name + 
                            " has reached the minimum layer order: " + holder.GetOrderInLayer());
                    }
                }
                else
                {
                    holder.SetOrderInLayer(order);
                    if (showInformation)
                    {
                        ShowNotification.Info("Decreases the order",
                            "The order in layer of the chosen object decreases to " + holder.GetOrderInLayer() + ".", 0.8f);
                    }
                    /// For mind map nodes, it's important that the text is also assigned the order.
                    if (obj.CompareTag(Tags.MindMapNode))
                    {
                        obj.GetComponentInChildren<TextMeshPro>().sortingOrder = order;
                    }

                    /// Moves the object along the z-axis.
                    MoveObjectZ(obj, order, useWorldPos);

                    /// Sets the order on the Canvas and TextMeshPro components, if present.
                    if (obj.GetComponent<TextMeshPro>() != null)
                    {
                        obj.GetComponent<TextMeshPro>().sortingOrder = order;
                    }
                    if (obj.GetComponent<Canvas>() != null)
                    {
                        obj.GetComponent<Canvas>().sortingOrder = order;
                    }
                }
            }
        }

        /// <summary>
        /// Adjusts the order in the layer by moving the object along the z-axis.
        /// </summary>
        /// <param name="obj">The object that is to be moved</param>
        /// <param name="order">The order to calculate the new z position of the object</param>
        /// <param name="useWorldPos">use world position for moving, only needed for sticky note</param>
        private static void MoveObjectZ(GameObject obj, int order, bool useWorldPos)
        {
            Vector3 oldPos = obj.transform.localPosition;

            /// For sticky notes, it's important to use the world space position 
            /// since it is an independent object not dependent on a drawable.
            if (useWorldPos)
            {
                if (obj.GetComponent<OrderInLayerValueHolder>() != null)
                {
                    oldPos = obj.GetComponent<OrderInLayerValueHolder>().GetOriginPosition();
                }
            }

            /// The order distance.
            float multiplyValue = order;
            if (order <= 0)
            {
                multiplyValue = 0.5f;
            }

            /// For sticky notes, it's important to use the original position for the calculation.
            if (useWorldPos)
            {
                obj.transform.position = oldPos - obj.transform.forward * ValueHolder.distanceToDrawable.z 
                    * multiplyValue;
            }
            else
            {
                /// For a <see cref="DrawableType"/> object only change the z-axis.
                obj.transform.localPosition = new Vector3(oldPos.x, oldPos.y, 
                    multiplyValue * -ValueHolder.distanceToDrawable.z);
            }
        }
    }
}