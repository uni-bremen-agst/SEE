using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartDragHandlerVr : ChartDragHandler
	{
		private Transform _parent;
		private Vector3 _distance;

		protected override void Awake()
		{
			base.Awake();
			_parent = transform.parent.GetComponent<ChartContent>().parent.transform;
		}

		public override void OnDrag(PointerEventData eventData)
		{
			if (eventData.pointerCurrentRaycast.worldPosition != Vector3.zero)
				_parent.position = eventData.pointerCurrentRaycast.worldPosition - _distance;
			//TODO: test
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			_distance = eventData.pointerCurrentRaycast.worldPosition - _chart.position;
		}
	}
}