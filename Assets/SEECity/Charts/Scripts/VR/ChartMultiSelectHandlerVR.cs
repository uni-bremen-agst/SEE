using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartMultiSelectHandlerVR : ChartMultiSelectHandler
	{
		public override void OnPointerDown(PointerEventData eventData)
		{
			selectionRect.gameObject.SetActive(true);
			selectionRect.position = eventData.pointerCurrentRaycast.worldPosition;
			startingPos = selectionRect.position;
			selectionRect.sizeDelta = new Vector2(0f, 0f);
		}

		public override void OnDrag(PointerEventData eventData)
		{
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			//if (startingPos.x < eventData.pointerCurrentRaycast.worldPosition)
		}
	}
}