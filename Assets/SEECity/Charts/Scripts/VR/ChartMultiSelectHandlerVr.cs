using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartMultiSelectHandlerVr : ChartMultiSelectHandler
	{
		public override void OnPointerDown(PointerEventData eventData)
		{
			selectionRect.gameObject.SetActive(true);
			selectionRect.anchoredPosition = eventData.position;
            selectionRect.anchoredPosition3D = new Vector3(selectionRect.anchoredPosition.x, selectionRect.anchoredPosition.y, 0);
			startingPos = selectionRect.anchoredPosition;
			selectionRect.sizeDelta = new Vector2(1f, 1f);
            Debug.Log(startingPos);
            Debug.Log(eventData.position);
		}

        public override void OnDrag(PointerEventData eventData)
        {
            //bool negative = false;
            //if (eventData.position.x - startingPos.x < 0)
            //{
            //    selectionRect.sizeDelta = new Vector2(
            //        Mathf.Abs(eventData.position.x - startingPos.x), eventData.position.y - startingPos.y);
            //    selectionRect.anchoredPosition = new Vector3(
            //        startingPos.x - selectionRect.sizeDelta.x / 2 * selectionRect.lossyScale.x,
            //        startingPos.y + selectionRect.sizeDelta.y / 2 * selectionRect.lossyScale.y,
            //        0);
            //    negative = true;
            //}

            //if (eventData.position.y - startingPos.y < 0)
            //{
            //    if (negative)
            //    {
            //        selectionRect.sizeDelta = new Vector2(selectionRect.sizeDelta.x,
            //            Mathf.Abs(eventData.position.y - startingPos.y));
            //        selectionRect.anchoredPosition = new Vector3(selectionRect.position.x,
            //            startingPos.y - selectionRect.sizeDelta.y / 2 *
            //            selectionRect.lossyScale.y, 0);
            //    }
            //    else
            //    {
            //        selectionRect.sizeDelta = new Vector2(
            //            (eventData.position.x - startingPos.x),
            //            Mathf.Abs(eventData.position.y - startingPos.y));
            //        selectionRect.anchoredPosition = new Vector3(
            //            startingPos.x + selectionRect.sizeDelta.x / 2 *
            //            selectionRect.lossyScale.x,
            //            startingPos.y - selectionRect.sizeDelta.y / 2 *
            //            selectionRect.lossyScale.y, 0);
            //        negative = true;
            //    }
            //}

            //if (!negative)
            //{
            //    selectionRect.sizeDelta =
            //        new Vector2(
            //            (eventData.position.x - startingPos.x),
            //            (eventData.position.y - startingPos.y));
            //    selectionRect.anchoredPosition = new Vector3(
            //        startingPos.x + selectionRect.sizeDelta.x / 2 * selectionRect.lossyScale.x,
            //        startingPos.y + selectionRect.sizeDelta.y / 2 * selectionRect.lossyScale.y,
            //        0);
            //}
        }

        public override void OnPointerUp(PointerEventData eventData)
		{
            if (startingPos.x < eventData.pointerCurrentRaycast.worldPosition.x) _chartContent.AreaSelection(startingPos, eventData.pointerCurrentRaycast.worldPosition, startingPos.y < eventData.pointerCurrentRaycast.worldPosition.y);
            else _chartContent.AreaSelection(eventData.pointerCurrentRaycast.worldPosition, startingPos, startingPos.y > eventData.pointerCurrentRaycast.worldPosition.y);

            selectionRect.gameObject.SetActive(false);
        }
	}
}