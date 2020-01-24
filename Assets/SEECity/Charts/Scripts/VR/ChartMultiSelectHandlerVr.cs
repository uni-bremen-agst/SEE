using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartMultiSelectHandlerVr : ChartMultiSelectHandler
	{
		public override void OnPointerDown(PointerEventData eventData)
		{
			selectionRect.gameObject.SetActive(true);
			selectionRect.position = eventData.pointerCurrentRaycast.worldPosition;
            selectionRect.anchoredPosition3D = new Vector3(selectionRect.anchoredPosition.x, selectionRect.anchoredPosition.y, 0);
			startingPos = selectionRect.position;
			selectionRect.sizeDelta = new Vector2(0f, 0f);
		}

		public override void OnDrag(PointerEventData eventData)
		{
            bool negative = false;
            if (eventData.pointerCurrentRaycast.worldPosition.x - startingPos.x < 0)
            {
                selectionRect.sizeDelta = new Vector2(
                    Mathf.Abs(eventData.pointerCurrentRaycast.worldPosition.x - startingPos.x) / selectionRect.lossyScale.x,
                    (eventData.pointerCurrentRaycast.worldPosition.y - startingPos.y) / selectionRect.lossyScale.y);
                selectionRect.position = new Vector3(
                    startingPos.x - selectionRect.sizeDelta.x / 2 /** selectionRect.lossyScale.x*/,
                    startingPos.y + selectionRect.sizeDelta.y / 2 /** selectionRect.lossyScale.y*/,
                    0);
                selectionRect.anchoredPosition3D = new Vector3(selectionRect.anchoredPosition.x, selectionRect.anchoredPosition.y, 0);
                negative = true;
            }

            if (eventData.pointerCurrentRaycast.worldPosition.y - startingPos.y < 0)
            {
                if (negative)
                {
                    selectionRect.sizeDelta = new Vector2(selectionRect.sizeDelta.x,
                        Mathf.Abs(eventData.pointerCurrentRaycast.worldPosition.y - startingPos.y) /
                        selectionRect.lossyScale.y);
                    selectionRect.position = new Vector3(selectionRect.position.x,
                        startingPos.y - selectionRect.sizeDelta.y / 2 *
                        selectionRect.lossyScale.y, 0);
                    selectionRect.anchoredPosition3D = new Vector3(selectionRect.anchoredPosition.x, selectionRect.anchoredPosition.y, 0);
                }
                else
                {
                    selectionRect.sizeDelta = new Vector2(
                        (eventData.pointerCurrentRaycast.worldPosition.x - startingPos.x) / selectionRect.lossyScale.x,
                        Mathf.Abs(eventData.pointerCurrentRaycast.worldPosition.y - startingPos.y) /
                        selectionRect.lossyScale.y);
                    selectionRect.position = new Vector3(
                        startingPos.x + selectionRect.sizeDelta.x / 2 *
                        selectionRect.lossyScale.x,
                        startingPos.y - selectionRect.sizeDelta.y / 2 *
                        selectionRect.lossyScale.y, 0);
                    selectionRect.anchoredPosition3D = new Vector3(selectionRect.anchoredPosition.x, selectionRect.anchoredPosition.y, 0);
                    negative = true;
                }
            }

            if (!negative)
            {
                selectionRect.sizeDelta =
                    new Vector2(
                        (eventData.pointerCurrentRaycast.worldPosition.x - startingPos.x) / selectionRect.lossyScale.x,
                        (eventData.pointerCurrentRaycast.worldPosition.y - startingPos.y) / selectionRect.lossyScale.y);
                selectionRect.position = new Vector3(
                    startingPos.x + selectionRect.sizeDelta.x / 2 * selectionRect.lossyScale.x,
                    startingPos.y + selectionRect.sizeDelta.y / 2 * selectionRect.lossyScale.y,
                    0);
                selectionRect.anchoredPosition3D = new Vector3(selectionRect.anchoredPosition.x, selectionRect.anchoredPosition.y, 0);
            }
        }

		public override void OnPointerUp(PointerEventData eventData)
		{
            if (startingPos.x < eventData.pointerCurrentRaycast.worldPosition.x) _chartContent.AreaSelection(startingPos, eventData.pointerCurrentRaycast.worldPosition, startingPos.y < eventData.pointerCurrentRaycast.worldPosition.y);
            else _chartContent.AreaSelection(eventData.pointerCurrentRaycast.worldPosition, startingPos, startingPos.y > eventData.pointerCurrentRaycast.worldPosition.y);

            selectionRect.gameObject.SetActive(false);
            //if (startingPos.x < eventData.pointerCurrentRaycast.worldPosition)
            //Might have to be chartcontentVR
        }
	}
}