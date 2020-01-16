using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts.VR
{
	public class ChartMultiSelectHandlerVR : ChartMultiSelectHandler
	{
		public override void OnPointerDown(PointerEventData eventData)
		{
			_selectionRect.gameObject.SetActive(true);
			_selectionRect.position = eventData.pointerCurrentRaycast.worldPosition;
			_startingPos = _selectionRect.position;
			_selectionRect.sizeDelta = new Vector2(0f, 0f);
		}

		public override void OnPointerUp(PointerEventData eventData)
		{

		}
	}
}