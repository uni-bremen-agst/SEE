using SEE.Controls.Actions;
using SEE.Game;
using SEE.Game.UI.Notification;
using System;
using System.Collections;
using SEE.Game.Drawable.Configurations;
using TMPro;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This GameLayerChanger allows to change the order in layer of a <see cref="DrawableType"/>
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
            if (obj.GetComponent<OrderInLayerValueHolder>() != null || obj.GetComponentInChildren<OrderInLayerValueHolder>() != null)
            {
                OrderInLayerValueHolder holder = obj.GetComponent<OrderInLayerValueHolder>() != null ? 
                    obj.GetComponent<OrderInLayerValueHolder>() : obj.GetComponentInChildren<OrderInLayerValueHolder>();
                if (holder.GetOrderInLayer() >= ValueHolder.currentOrderInLayer)
                {
                    if (showInformation)
                    {
                        ShowNotification.Warn("Maximum layer order", obj.name + " has reached the maximum layer order: " + holder.GetOrderInLayer());
                    }
                }
                else
                {
                    holder.SetOrderInLayer(order);
                    MoveObjectZ(obj, order, useWorldPos);
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
            if (obj.GetComponent<OrderInLayerValueHolder>() != null || obj.GetComponentInChildren<OrderInLayerValueHolder>() != null)
            {
                OrderInLayerValueHolder holder = obj.GetComponent<OrderInLayerValueHolder>() != null ?
                    obj.GetComponent<OrderInLayerValueHolder>() : obj.GetComponentInChildren<OrderInLayerValueHolder>();
                if (holder.GetOrderInLayer() == 0)
                {
                    if (showInformation)
                    {
                        ShowNotification.Warn("Minimum layer order", obj.name + " has reached the minimum layer order: " + holder.GetOrderInLayer());
                    }
                }
                else
                {
                    holder.SetOrderInLayer(order);
                    MoveObjectZ(obj, order, useWorldPos);
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
        /// This method implements the order in layers by moving the object on the z axis.
        /// </summary>
        /// <param name="obj">The object that is to be moved</param>
        /// <param name="order">The order to calculate the new z position of the object</param>
        /// <param name="useWorldPos">use world position for moving, only needed for sticky note</param>
        private static void MoveObjectZ(GameObject obj, int order, bool useWorldPos)
        {
            Vector3 oldPos = obj.transform.localPosition;
            if (useWorldPos)
            {
                if (obj.GetComponent<OrderInLayerValueHolder>() != null)
                {
                    oldPos = obj.GetComponent<OrderInLayerValueHolder>().GetOriginPosition();
                }
            }
            float multiplyValue = order;
            if (order <= 0)
            {
                multiplyValue = 0.5f;
            }
            if (useWorldPos)
            {
                obj.transform.position = oldPos - obj.transform.forward * ValueHolder.distanceToDrawable.z * multiplyValue;
            }
            else
            {
                obj.transform.localPosition = new Vector3(oldPos.x, oldPos.y, multiplyValue * -ValueHolder.distanceToDrawable.z);
            }
        }
    }
}