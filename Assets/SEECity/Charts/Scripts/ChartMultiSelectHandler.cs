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
		/// Needed for access to <see cref="Scripts.ChartContent.AreaSelection" />.
		/// </summary>
		protected ChartContent ChartContent;

		/// <summary>
		/// The position the user started the drag at.
		/// </summary>
		protected Vector3 StartingPos;

		/// <summary>
		/// Assigns <see cref="ChartContent" />.
		/// </summary>
		private void Awake()
		{
			ChartContent = transform.parent.GetComponent<ChartContent>();
		}

		/// <summary>
		/// Activates and sets starting position of <see cref="selectionRect" />.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnPointerDown(PointerEventData eventData)
		{
			selectionRect.gameObject.SetActive(true);
			selectionRect.position = eventData.pressPosition;
			StartingPos = selectionRect.position;
			selectionRect.sizeDelta = new Vector2(0f, 0f);
		}

		/// <summary>
		/// Resizes the <see cref="selectionRect" /> to make it span from <see cref="StartingPos" /> to
		/// <see cref="PointerEventData.position" />.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnDrag(PointerEventData eventData)
		{
			bool negative = false;
			Vector3 lossyScale = selectionRect.lossyScale;

			if (eventData.position.x - StartingPos.x < 0)
			{
				selectionRect.sizeDelta =
					new Vector2(
						Mathf.Abs(eventData.position.x - StartingPos.x) /
						selectionRect.lossyScale.x,
						(eventData.position.y - StartingPos.y) / lossyScale.y);
				selectionRect.position =
					new Vector3(StartingPos.x - selectionRect.sizeDelta.x / 2 * lossyScale.x,
						StartingPos.y + selectionRect.sizeDelta.y / 2 * lossyScale.y, 0);
				negative = true;
			}

			if (eventData.position.y - StartingPos.y < 0)
			{
				if (negative)
				{
					selectionRect.sizeDelta = new Vector2(selectionRect.sizeDelta.x,
						Mathf.Abs(eventData.position.y - StartingPos.y) /
						selectionRect.lossyScale.y);
					selectionRect.position = new Vector3(selectionRect.position.x,
						StartingPos.y - selectionRect.sizeDelta.y / 2 * lossyScale.y, 0);
				}
				else
				{
					selectionRect.sizeDelta =
						new Vector2((eventData.position.x - StartingPos.x) / lossyScale.x,
							Mathf.Abs(eventData.position.y - StartingPos.y) / lossyScale.y);
					selectionRect.position =
						new Vector3(StartingPos.x + selectionRect.sizeDelta.x / 2 * lossyScale.x,
							StartingPos.y - selectionRect.sizeDelta.y / 2 * lossyScale.y, 0);
					negative = true;
				}
			}

			if (!negative)
			{
				selectionRect.sizeDelta =
					new Vector2((eventData.position.x - StartingPos.x) / lossyScale.x,
						(eventData.position.y - StartingPos.y) / lossyScale.y);
				selectionRect.position =
					new Vector3(StartingPos.x + selectionRect.sizeDelta.x / 2 * lossyScale.x,
						StartingPos.y + selectionRect.sizeDelta.y / 2 * lossyScale.y, 0);
			}
		}

		/// <summary>
		/// Highlights all markers in <see cref="selectionRect" /> and deactivates it.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnPointerUp(PointerEventData eventData)
		{
			if (StartingPos.x < eventData.position.x)
				ChartContent.AreaSelection(StartingPos, eventData.position,
					StartingPos.y < eventData.position.y);
			else
				ChartContent.AreaSelection(eventData.position, StartingPos,
					StartingPos.y > eventData.position.y);

			selectionRect.gameObject.SetActive(false);
		}
	}
}