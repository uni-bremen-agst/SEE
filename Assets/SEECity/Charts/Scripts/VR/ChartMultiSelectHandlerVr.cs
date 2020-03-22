using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Charts.Scripts.VR
{
	/// <summary>
	/// The VR version of <see cref="ChartMultiSelectHandler" />.
	/// </summary>
	public class ChartMultiSelectHandlerVr : ChartMultiSelectHandler
	{
		/// <summary>
		/// An object helping to transform world coordinated to chart coordinates.
		/// </summary>
		[SerializeField] private RectTransform reference;

		/// <summary>
		/// The position of the <see cref="reference" />.
		/// </summary>
		private Vector3 _referencePos;

		/// <summary>
		/// Sets the starting positions of <see cref="ChartMultiSelectHandler.selectionRect" /> and
		/// <see cref="reference" />.
		/// </summary>
		/// <param name="eventData">Contains position data of the pointer.</param>
		public override void OnPointerDown(PointerEventData eventData)
		{
			selectionRect.gameObject.SetActive(true);
			selectionRect.position = eventData.pointerCurrentRaycast.worldPosition;
			selectionRect.anchoredPosition3D = new Vector3(selectionRect.anchoredPosition.x,
				selectionRect.anchoredPosition.y, 0);
			startingPos = selectionRect.anchoredPosition;
			selectionRect.sizeDelta = new Vector2(0f, 0f);
			reference.anchoredPosition3D = selectionRect.anchoredPosition3D;
		}

		/// <summary>
		/// Updates the <see cref="_referencePos" /> and the
		/// <see cref="ChartMultiSelectHandler.selectionRect" />.
		/// </summary>
		/// <param name="eventData">Contains position data of the pointer.</param>
		public override void OnDrag(PointerEventData eventData)
		{
			var negative = false;
			var sizeDelta = Vector2.zero;
			reference.position = eventData.pointerCurrentRaycast.worldPosition;
			reference.anchoredPosition3D = new Vector3(reference.anchoredPosition.x,
				reference.anchoredPosition.y, 0);
			_referencePos = reference.anchoredPosition;

			if (_referencePos.x - startingPos.x < 0)
			{
				selectionRect.sizeDelta = new Vector2(Mathf.Abs(_referencePos.x - startingPos.x),
					_referencePos.y - startingPos.y);
				sizeDelta = selectionRect.sizeDelta;
				selectionRect.anchoredPosition = new Vector3(startingPos.x - sizeDelta.x / 2,
					startingPos.y + sizeDelta.y / 2, 0);
				negative = true;
			}

			if (_referencePos.y - startingPos.y < 0)
			{
				if (negative)
				{
					selectionRect.sizeDelta = new Vector2(selectionRect.sizeDelta.x,
						Mathf.Abs(_referencePos.y - startingPos.y));
					selectionRect.anchoredPosition = new Vector3(selectionRect.anchoredPosition.x,
						startingPos.y - selectionRect.sizeDelta.y / 2, 0);
				}
				else
				{
					selectionRect.sizeDelta = new Vector2(_referencePos.x - startingPos.x,
						Mathf.Abs(_referencePos.y - startingPos.y));
					sizeDelta = selectionRect.sizeDelta;
					selectionRect.anchoredPosition = new Vector3(startingPos.x + sizeDelta.x / 2,
						startingPos.y - sizeDelta.y / 2, 0);
					negative = true;
				}
			}

			if (negative) return;
			selectionRect.sizeDelta = new Vector2(_referencePos.x - startingPos.x,
				_referencePos.y - startingPos.y);
			sizeDelta = selectionRect.sizeDelta;
			selectionRect.anchoredPosition = new Vector3(startingPos.x + sizeDelta.x / 2,
				startingPos.y + sizeDelta.y / 2, 0);
		}

		/// <summary>
		/// Checks the area that has been dragged and sends it to <see cref="ChartContentVr.AreaSelection" />.
		/// </summary>
		/// <param name="eventData">Contains position data of the pointer.</param>
		public override void OnPointerUp(PointerEventData eventData)
		{
			if (startingPos.x < _referencePos.x)
				chartContent.AreaSelection(startingPos, _referencePos,
					startingPos.y < _referencePos.y);
			else
				chartContent.AreaSelection(_referencePos, startingPos,
					startingPos.y > _referencePos.y);

			selectionRect.gameObject.SetActive(false);
		}
	}
}