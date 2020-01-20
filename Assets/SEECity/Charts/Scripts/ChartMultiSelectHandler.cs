using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Handles selection of multiple markers in selection mode.
	/// </summary>
	public class ChartMultiSelectHandler : MonoBehaviour, IPointerDownHandler, IDragHandler,
		IPointerUpHandler
	{
		/// <summary>
		/// The rectangle used to visualize the selection process for the user.
		/// </summary>
		[SerializeField] protected RectTransform _selectionRect = null;

		/// <summary>
		/// Needed for access to <see cref="ChartContent.AreaSelection" />.
		/// </summary>
		private ChartContent _chartContent;

		/// <summary>
		/// The position the user started the drag at.
		/// </summary>
		protected Vector3 _startingPos;

        /// <summary>
        /// TODO
        /// </summary>
		private void Awake()
		{
			_chartContent = transform.parent.GetComponent<ChartContent>();
		}

		/// <summary>
		/// Activates and sets starting position of <see cref="_selectionRect" />.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnPointerDown(PointerEventData eventData)
		{
			_selectionRect.gameObject.SetActive(true);
			_selectionRect.position = eventData.pressPosition;
			_startingPos = _selectionRect.position;
			_selectionRect.sizeDelta = new Vector2(0f, 0f);
		}

		/// <summary>
		/// Resizes the <see cref="_selectionRect" />.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnDrag(PointerEventData eventData)
		{
			bool negative = false;
			if (eventData.position.x - _startingPos.x < 0)
			{
				_selectionRect.sizeDelta = new Vector2(
					Mathf.Abs(eventData.position.x - _startingPos.x) / _selectionRect.lossyScale.x,
					(eventData.position.y - _startingPos.y) / _selectionRect.lossyScale.y);
				_selectionRect.position = new Vector3(
					_startingPos.x - _selectionRect.sizeDelta.x / 2 * _selectionRect.lossyScale.x,
					_startingPos.y + _selectionRect.sizeDelta.y / 2 * _selectionRect.lossyScale.y,
					0);
				negative = true;
			}

			if (eventData.position.y - _startingPos.y < 0)
			{
				if (negative)
				{
					_selectionRect.sizeDelta = new Vector2(_selectionRect.sizeDelta.x,
						Mathf.Abs(eventData.position.y - _startingPos.y) /
						_selectionRect.lossyScale.y);
					_selectionRect.position = new Vector3(_selectionRect.position.x,
						_startingPos.y - _selectionRect.sizeDelta.y / 2 *
						_selectionRect.lossyScale.y, 0);
				}
				else
				{
					_selectionRect.sizeDelta = new Vector2(
						(eventData.position.x - _startingPos.x) / _selectionRect.lossyScale.x,
						Mathf.Abs(eventData.position.y - _startingPos.y) /
						_selectionRect.lossyScale.y);
					_selectionRect.position = new Vector3(
						_startingPos.x + _selectionRect.sizeDelta.x / 2 *
						_selectionRect.lossyScale.x,
						_startingPos.y - _selectionRect.sizeDelta.y / 2 *
						_selectionRect.lossyScale.y, 0);
					negative = true;
				}
			}

			if (!negative)
			{
				_selectionRect.sizeDelta =
					new Vector2(
						(eventData.position.x - _startingPos.x) / _selectionRect.lossyScale.x,
						(eventData.position.y - _startingPos.y) / _selectionRect.lossyScale.y);
				_selectionRect.position = new Vector3(
					_startingPos.x + _selectionRect.sizeDelta.x / 2 * _selectionRect.lossyScale.x,
					_startingPos.y + _selectionRect.sizeDelta.y / 2 * _selectionRect.lossyScale.y,
					0);
			}
		}

		/// <summary>
		/// Highlights all markers in <see cref="_selectionRect" /> and deactivates it.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnPointerUp(PointerEventData eventData)
		{
			if (_startingPos.x < eventData.position.x)
				_chartContent.AreaSelection(_startingPos, eventData.position,
					_startingPos.y < eventData.position.y);
			else
				_chartContent.AreaSelection(eventData.position, _startingPos,
					_startingPos.y > eventData.position.y);

			_selectionRect.gameObject.SetActive(false);
		}
	}
}