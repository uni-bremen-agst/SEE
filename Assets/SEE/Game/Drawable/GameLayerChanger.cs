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
        public static void Increase(GameObject obj, int order)
        {
            if (obj.GetComponent<OrderInLayerValueHolder>() != null)
            {
                OrderInLayerValueHolder holder = obj.GetComponent<OrderInLayerValueHolder>();
                if (holder.GetOrderInLayer() >= ValueHolder.currentOrderInLayer)
                {
                    ShowNotification.Warn("Maximum layer order", obj.name + " has reached the maximum layer order: " + holder.GetOrderInLayer());
                }
                else
                {
                    holder.SetOrderInLayer(order);
                    MoveObjectZ(obj, order);
                    if (obj.GetComponent<TextMeshPro>() != null)
                    {
                        obj.GetComponent<TextMeshPro>().sortingOrder = order;
                    }
                }
            }
        }

        /// <summary>
        /// Decrease the order in layer of an object with a <see cref="OrderInLayerValueHolder"/> component.
        /// </summary>
        /// <param name="obj">The object whose order in layer is to be changed</param>
        /// <param name="order">The new order of the object.</param>
        public static void Decrease(GameObject obj, int order)
        {
            if (obj.GetComponent<OrderInLayerValueHolder>() != null)
            {
                OrderInLayerValueHolder holder = obj.GetComponent<OrderInLayerValueHolder>();
                if (holder.GetOrderInLayer() == 0)
                {
                    ShowNotification.Warn("Minimum layer order", obj.name + " has reached the minimum layer order: " + holder.GetOrderInLayer());
                }
                else
                {
                    holder.SetOrderInLayer(order);
                    MoveObjectZ(obj, order);
                    if (obj.GetComponent<TextMeshPro>() != null)
                    {
                        obj.GetComponent<TextMeshPro>().sortingOrder = order;
                    }
                }
            }
        }

        /// <summary>
        /// This method implements the order in layers by moving the object on the z axis.
        /// </summary>
        /// <param name="obj">The object that is to be moved</param>
        /// <param name="order">The order to calculate the new z position of the object</param>
        private static void MoveObjectZ(GameObject obj, int order)
        {
            Vector3 oldPos = obj.transform.localPosition;
            float multiplyValue = order;
            if (order == 0)
            {
                multiplyValue = 0.5f;
            }
            obj.transform.localPosition = new Vector3(oldPos.x, oldPos.y, multiplyValue * -ValueHolder.distanceToDrawable.z);;
        }
    }
}