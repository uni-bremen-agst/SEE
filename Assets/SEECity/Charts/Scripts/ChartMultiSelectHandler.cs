using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts
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
		[SerializeField] protected RectTransform selectionRect;

		/// <summary>
		/// Needed for access to <see cref="ChartContent.AreaSelection" />.
		/// </summary>
		protected ChartContent _chartContent;

		/// <summary>
		/// The position the user started the drag at.
		/// </summary>
		protected Vector3 startingPos;

		/// <summary>
		/// TODO
		/// </summary>
		private void Awake()
		{
			_chartContent = transform.parent.GetComponent<ChartContent>();
		}

		/// <summary>
		/// Activates and sets starting position of <see cref="selectionRect" />.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnPointerDown(PointerEventData eventData)
		{
			selectionRect.gameObject.SetActive(true);
			selectionRect.position = eventData.pressPosition;
			startingPos = selectionRect.position;
			selectionRect.sizeDelta = new Vector2(0f, 0f);
		}

		/// <summary>
		/// Resizes the <see cref="selectionRect" />.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnDrag(PointerEventData eventData)
		{
			bool negative = false;
			if (eventData.position.x - startingPos.x < 0)
			{
				selectionRect.sizeDelta = new Vector2(
					Mathf.Abs(eventData.position.x - startingPos.x) / selectionRect.lossyScale.x,
					(eventData.position.y - startingPos.y) / selectionRect.lossyScale.y);
				selectionRect.position = new Vector3(
					startingPos.x - selectionRect.sizeDelta.x / 2 * selectionRect.lossyScale.x,
					startingPos.y + selectionRect.sizeDelta.y / 2 * selectionRect.lossyScale.y,
					0);
				negative = true;
			}

			if (eventData.position.y - startingPos.y < 0)
			{
				if (negative)
				{
					selectionRect.sizeDelta = new Vector2(selectionRect.sizeDelta.x,
						Mathf.Abs(eventData.position.y - startingPos.y) /
						selectionRect.lossyScale.y);
					selectionRect.position = new Vector3(selectionRect.position.x,
						startingPos.y - selectionRect.sizeDelta.y / 2 *
						selectionRect.lossyScale.y, 0);
				}
				else
				{
					selectionRect.sizeDelta = new Vector2(
						(eventData.position.x - startingPos.x) / selectionRect.lossyScale.x,
						Mathf.Abs(eventData.position.y - startingPos.y) /
						selectionRect.lossyScale.y);
					selectionRect.position = new Vector3(
						startingPos.x + selectionRect.sizeDelta.x / 2 *
						selectionRect.lossyScale.x,
						startingPos.y - selectionRect.sizeDelta.y / 2 *
						selectionRect.lossyScale.y, 0);
					negative = true;
				}
			}

			if (!negative)
			{
				selectionRect.sizeDelta =
					new Vector2(
						(eventData.position.x - startingPos.x) / selectionRect.lossyScale.x,
						(eventData.position.y - startingPos.y) / selectionRect.lossyScale.y);
				selectionRect.position = new Vector3(
					startingPos.x + selectionRect.sizeDelta.x / 2 * selectionRect.lossyScale.x,
					startingPos.y + selectionRect.sizeDelta.y / 2 * selectionRect.lossyScale.y,
					0);
			}
		}

		/// <summary>
		/// Highlights all markers in <see cref="selectionRect" /> and deactivates it.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnPointerUp(PointerEventData eventData)
		{
			if (startingPos.x < eventData.position.x)
				_chartContent.AreaSelection(startingPos, eventData.position,
					startingPos.y < eventData.position.y);
			else
				_chartContent.AreaSelection(eventData.position, startingPos,
					startingPos.y > eventData.position.y);

			selectionRect.gameObject.SetActive(false);
		}
	}
}