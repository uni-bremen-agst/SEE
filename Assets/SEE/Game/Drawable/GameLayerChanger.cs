using SEE.Controls.Actions;
using SEE.Game;
using SEE.Game.UI.Notification;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public static class GameLayerChanger
    {
        public enum LayerChangerStates
        {
            Increase,
            Decrease
        }
        public static bool Increase(DrawableTypes type, GameObject obj, int order)
        {
            bool result = false;
            switch (type)
            {
                case DrawableTypes.Line:
                    LineRenderer lineRenderer = obj.GetComponent<LineRenderer>();
                    if (lineRenderer.sortingOrder >= DrawableHelper.orderInLayer)
                    {
                        ShowNotification.Warn("Maximum layer order", obj.name + " has reached the maximum layer order: " + lineRenderer.sortingOrder);
                    }
                    else
                    {
                        lineRenderer.sortingOrder = order;
                        SetOrder(obj, order);
                        result = true;
                    }
                    break;

                default:
                    break;
            }
            return result;
        }

        public static bool Decrease(DrawableTypes type, GameObject obj, int order)
        {
            bool result = false;
            switch (type)
            {
                case DrawableTypes.Line:
                    LineRenderer lineRenderer = obj.GetComponent<LineRenderer>();
                    if (lineRenderer.sortingOrder == 0)
                    {
                        ShowNotification.Warn("Minimum layer order", obj.name + " has reached the minimum layer order: " + lineRenderer.sortingOrder);
                    }
                    else
                    {
                        lineRenderer.sortingOrder = order;
                        SetOrder(obj, order);
                        result = true;
                    }
                    break;

                default:
                    break;
            }
            return result;
        }

        public static void SetOrder(GameObject obj, int order)
        {
            Vector3 oldPos = obj.transform.localPosition;
            float multiplyValue = order;
            if (order == 0)
            {
                multiplyValue = 0.5f;
            }
            obj.transform.localPosition = new Vector3(oldPos.x, oldPos.y, multiplyValue * -DrawableHelper.distanceToBoard.z);;
        }
    }
}