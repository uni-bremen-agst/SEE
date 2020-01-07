using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Handles selection of multiple markers in selection mode.
	/// </summary>
	public class MultiSelectHandler : MonoBehaviour, IPointerDownHandler, IDragHandler,
		IPointerUpHandler
	{
		/// <summary>
		/// The rectangle used to visualize the selection process for the user.
		/// </summary>
		[SerializeField] private RectTransform _selectionRect;

		/// <summary>
		/// Needed for access to <see cref="ChartContent.AreaSelection" />.
		/// </summary>
		[SerializeField] private ChartContent _chartContent;

		/// <summary>
		/// The position the user started the drag at.
		/// </summary>
		private Vector3 _startingPos;

		/// <summary>
		/// Activates and sets starting position of <see cref="_selectionRect" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerDown(PointerEventData eventData)
		{
			_selectionRect.gameObject.SetActive(true);
			_selectionRect.position = Input.mousePosition;
			_startingPos = _selectionRect.position;
			_selectionRect.sizeDelta = new Vector2(0f, 0f);
		}

		/// <summary>
		/// Resizes the <see cref="_selectionRect" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnDrag(PointerEventData eventData)
		{
			bool negative = false;
			if (Input.mousePosition.x - _startingPos.x < 0)
			{
				_selectionRect.sizeDelta = new Vector2(
					Mathf.Abs(Input.mousePosition.x - _startingPos.x) / _selectionRect.lossyScale.x,
					(Input.mousePosition.y - _startingPos.y) / _selectionRect.lossyScale.y);
				_selectionRect.position = new Vector3(
					_startingPos.x - _selectionRect.sizeDelta.x / 2 * _selectionRect.lossyScale.x,
					_startingPos.y + _selectionRect.sizeDelta.y / 2 * _selectionRect.lossyScale.y,
					Input.mousePosition.z);
				negative = true;
			}

			if (Input.mousePosition.y - _startingPos.y < 0)
			{
				if (negative)
				{
					_selectionRect.sizeDelta = new Vector2(_selectionRect.sizeDelta.x,
						Mathf.Abs(Input.mousePosition.y - _startingPos.y) /
						_selectionRect.lossyScale.y);
					_selectionRect.position = new Vector3(_selectionRect.position.x,
						_startingPos.y - _selectionRect.sizeDelta.y / 2 *
						_selectionRect.lossyScale.y, Input.mousePosition.z);
				}
				else
				{
					_selectionRect.sizeDelta = new Vector2(
						(Input.mousePosition.x - _startingPos.x) / _selectionRect.lossyScale.x,
						Mathf.Abs(Input.mousePosition.y - _startingPos.y) /
						_selectionRect.lossyScale.y);
					_selectionRect.position = new Vector3(
						_startingPos.x + _selectionRect.sizeDelta.x / 2 *
						_selectionRect.lossyScale.x,
						_startingPos.y - _selectionRect.sizeDelta.y / 2 *
						_selectionRect.lossyScale.y, Input.mousePosition.z);
					negative = true;
				}
			}

			if (!negative)
			{
				_selectionRect.sizeDelta =
					new Vector2(
						(Input.mousePosition.x - _startingPos.x) / _selectionRect.lossyScale.x,
						(Input.mousePosition.y - _startingPos.y) / _selectionRect.lossyScale.y);
				_selectionRect.position = new Vector3(
					_startingPos.x + _selectionRect.sizeDelta.x / 2 * _selectionRect.lossyScale.x,
					_startingPos.y + _selectionRect.sizeDelta.y / 2 * _selectionRect.lossyScale.y,
					Input.mousePosition.z);
			}
		}

		/// <summary>
		/// Highlights all markers in <see cref="_selectionRect" /> and deactivates it.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerUp(PointerEventData eventData)
		{
			Vector2 finalPos = Input.mousePosition;
			if (_startingPos.x < finalPos.x)
				_chartContent.AreaSelection(_startingPos, finalPos, _startingPos.y < finalPos.y);
			else
				_chartContent.AreaSelection(finalPos, _startingPos, _startingPos.y > finalPos.y);

			_selectionRect.gameObject.SetActive(false);
		}
	}
}