using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartMultiSelectHandlerVr : ChartMultiSelectHandler
	{
		[SerializeField] private RectTransform reference;
		private Vector3 _referencePos;

		public override void OnPointerDown(PointerEventData eventData)
		{
			selectionRect.gameObject.SetActive(true);
			selectionRect.position = eventData.pointerCurrentRaycast.worldPosition;
			selectionRect.anchoredPosition3D = new Vector3(selectionRect.anchoredPosition.x,
				selectionRect.anchoredPosition.y, 0);
			StartingPos = selectionRect.anchoredPosition;
			selectionRect.sizeDelta = new Vector2(0f, 0f);
			reference.anchoredPosition3D = selectionRect.anchoredPosition3D;
		}

		public override void OnDrag(PointerEventData eventData)
		{
			bool negative = false;
			reference.position = eventData.pointerCurrentRaycast.worldPosition;
			reference.anchoredPosition3D = new Vector3(reference.anchoredPosition.x,
				reference.anchoredPosition.y, 0);
			_referencePos = reference.anchoredPosition;
			Vector3 lossyScale = selectionRect.lossyScale;

			if (_referencePos.x - StartingPos.x < 0)
			{
				selectionRect.sizeDelta =
					new Vector2(Mathf.Abs(_referencePos.x - StartingPos.x) / lossyScale.x,
						(_referencePos.y - StartingPos.y) / lossyScale.y);
				selectionRect.position = new Vector3(
					StartingPos.x - selectionRect.sizeDelta.x / 2 * lossyScale.x,
					StartingPos.y + selectionRect.sizeDelta.y / 2 * lossyScale.y, 0);
				negative = true;
			}

			if (_referencePos.y - StartingPos.y < 0)
			{
				if (negative)
				{
					selectionRect.sizeDelta = new Vector2(selectionRect.sizeDelta.x,
						Mathf.Abs(_referencePos.y - StartingPos.y) / lossyScale.y);
					selectionRect.position = new Vector3(selectionRect.position.x,
						StartingPos.y - selectionRect.sizeDelta.y / 2 * lossyScale.y, 0);
				}
				else
				{
					selectionRect.sizeDelta =
						new Vector2((_referencePos.x - StartingPos.x) / lossyScale.x,
							Mathf.Abs(_referencePos.y - StartingPos.y) / lossyScale.y);
					selectionRect.position =
						new Vector3(StartingPos.x + selectionRect.sizeDelta.x / 2 * lossyScale.x,
							StartingPos.y - selectionRect.sizeDelta.y / 2 * lossyScale.y, 0);
					negative = true;
				}
			}

			if (!negative)
			{
				selectionRect.sizeDelta =
					new Vector2((_referencePos.x - StartingPos.x) / lossyScale.x,
						(_referencePos.y - StartingPos.y) / lossyScale.y);
				selectionRect.position =
					new Vector3(StartingPos.x + selectionRect.sizeDelta.x / 2 * lossyScale.x,
						StartingPos.y + selectionRect.sizeDelta.y / 2 * lossyScale.y, 0);
			}
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			if (StartingPos.x < _referencePos.x)
				ChartContent.AreaSelection(StartingPos, _referencePos,
					StartingPos.y < _referencePos.y);
			else
				ChartContent.AreaSelection(_referencePos, StartingPos,
					StartingPos.y > _referencePos.y);

			selectionRect.gameObject.SetActive(false);
		}
	}
}