using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts.VR
{
	public class ChartMoveHandlerVR : ChartMoveHandler
	{
		[SerializeField] private GameObject _physicalClosed;

		public override void OnDrag(PointerEventData eventData)
		{
			RectTransform pos = GetComponent<RectTransform>();
			_minimizeThis.transform.position = new Vector3(
				transform.position.x - pos.anchoredPosition.x * pos.lossyScale.x,
				transform.position.y - pos.anchoredPosition.y * pos.lossyScale.y,
				transform.position.z);
		}

		protected override void ToggleMinimize()
		{
			_minimizeThis.SetActive(_minimized);
			_physicalClosed.SetActive(!_minimized);
			_minimized = !_minimized;
		}
	}
}