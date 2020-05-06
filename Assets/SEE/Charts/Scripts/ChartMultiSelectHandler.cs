using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Charts.Scripts
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
		protected ChartContent chartContent;

		/// <summary>
		/// The position the user started the drag at.
		/// </summary>
		protected Vector3 startingPos;

		/// <summary>
		/// Assigns <see cref="chartContent" />.
		/// </summary>
		private void Awake()
		{
			chartContent = transform.parent.GetComponent<ChartContent>();
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
		/// Resizes the <see cref="selectionRect" /> to make it span from <see cref="startingPos" /> to
		/// <see cref="PointerEventData.position" />.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnDrag(PointerEventData eventData)
		{
			var negative = false;
			var lossyScale = selectionRect.lossyScale;
			var sizeDelta = Vector2.zero;

			if (eventData.position.x - startingPos.x < 0)
			{
				selectionRect.sizeDelta =
					new Vector2(
						Mathf.Abs(eventData.position.x - startingPos.x) /
						selectionRect.lossyScale.x,
						(eventData.position.y - startingPos.y) / lossyScale.y);
				sizeDelta = selectionRect.sizeDelta;
				selectionRect.position = new Vector3(startingPos.x - sizeDelta.x / 2 * lossyScale.x,
					startingPos.y + sizeDelta.y / 2 * lossyScale.y, 0);
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
						startingPos.y - selectionRect.sizeDelta.y / 2 * lossyScale.y, 0);
				}
				else
				{
					selectionRect.sizeDelta =
						new Vector2((eventData.position.x - startingPos.x) / lossyScale.x,
							Mathf.Abs(eventData.position.y - startingPos.y) / lossyScale.y);
					sizeDelta = selectionRect.sizeDelta;
					selectionRect.position =
						new Vector3(startingPos.x + sizeDelta.x / 2 * lossyScale.x,
							startingPos.y - sizeDelta.y / 2 * lossyScale.y, 0);
					negative = true;
				}
			}

			if (negative) return;
			selectionRect.sizeDelta =
				new Vector2((eventData.position.x - startingPos.x) / lossyScale.x,
					(eventData.position.y - startingPos.y) / lossyScale.y);
			sizeDelta = selectionRect.sizeDelta;
			selectionRect.position = new Vector3(startingPos.x + sizeDelta.x / 2 * lossyScale.x,
				startingPos.y + sizeDelta.y / 2 * lossyScale.y, 0);
		}

		/// <summary>
		/// Highlights all markers in <see cref="selectionRect" /> and deactivates it.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnPointerUp(PointerEventData eventData)
		{
			if (startingPos.x < eventData.position.x)
				chartContent.AreaSelection(startingPos, eventData.position,
					startingPos.y < eventData.position.y);
			else
				chartContent.AreaSelection(eventData.position, startingPos,
					startingPos.y > eventData.position.y);

			selectionRect.gameObject.SetActive(false);
		}
	}
}