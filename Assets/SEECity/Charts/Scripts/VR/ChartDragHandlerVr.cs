using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Charts.Scripts.VR
{
	/// <summary>
	/// The VR version of <see cref="ChartDragHandler" />.
	/// </summary>
	public class ChartDragHandlerVr : ChartDragHandler
	{
		/// <summary>
		/// The transform of the object to drag.
		/// </summary>
		private Transform _parent;

		/// <summary>
		/// The distance between the pointer and the middle of the chart.
		/// </summary>
		private Vector3 _distance;

		/// <summary>
		/// Finds the <see cref="_parent" />.
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
			_parent = transform.parent.GetComponent<ChartContent>().parent.transform;
		}

		/// <summary>
		/// Moves the chart to the new position of the pointer if the raycast hit something.
		/// </summary>
		/// <param name="eventData">Contains position data of the pointer.</param>
		public override void OnDrag(PointerEventData eventData)
		{
			if (eventData.pointerCurrentRaycast.worldPosition != Vector3.zero)
				_parent.position = eventData.pointerCurrentRaycast.worldPosition - _distance;
		}

		/// <summary>
		/// Saves the distance between the pointer and the middle of the chart.
		/// </summary>
		/// <param name="eventData">Contains position data of the pointer.</param>
		public override void OnPointerDown(PointerEventData eventData)
		{
			_distance = eventData.pointerCurrentRaycast.worldPosition - chart.position;
		}
	}
}