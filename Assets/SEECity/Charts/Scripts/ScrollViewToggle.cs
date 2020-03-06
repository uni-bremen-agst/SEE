using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEECity.Charts.Scripts
{
	/// <summary>
	/// Contains the logic for entries in the content selection scroll view.
	/// </summary>
	public class ScrollViewToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		/// <summary>
		/// Contains methods to highlight objects.
		/// </summary>
		private ChartManager _chartManager;

		/// <summary>
		/// The linked chart. Also contains methods to refresh the chart.
		/// </summary>
		private ChartContent _chartContent;

		/// <summary>
		/// If the user is currently pointing on this <see cref="GameObject" />
		/// </summary>
		private bool _pointedOn;

		/// <summary>
		/// The parent to this <see cref="ScrollViewToggle" />.
		/// </summary>
		public ScrollViewToggle Parent { private get; set; }

		/// <summary>
		/// Contains all children to this <see cref="ScrollViewToggle" />.
		/// </summary>
		private readonly List<ScrollViewToggle> _children = new List<ScrollViewToggle>();

		/// <summary>
		/// The running <see cref="UpdateStatus" /> <see cref="Coroutine" />.
		/// </summary>
		public Coroutine StatusUpdate { private get; set; }

		/// <summary>
		/// Contains the name of the <see cref="LinkedObject" /> in the UI.
		/// </summary>
		[SerializeField] private TextMeshProUGUI label;

		/// <summary>
		/// The UI element the user can click on to change the state of
		/// <see cref="UnityEngine.UI.Toggle.isOn" />.
		/// </summary>
		[SerializeField] private Toggle toggle;

		/// <summary>
		/// Contains properties for adding objects to charts.
		/// </summary>
		public NodeHighlights LinkedObject { private get; set; }

		/// <summary>
		/// Called by <see cref="ChartContent" /> after creation to pass some values and initialize attributes.
		/// </summary>
		/// <param name="script"></param>
		public void Initialize(ChartContent script)
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			_chartContent = script;
			toggle.isOn = Parent == null || (bool) LinkedObject.showInChart[_chartContent];
		}

		/// <summary>
		/// Changes the UI label of this entry.
		/// </summary>
		/// <param name="text"></param>
		public void SetLabel(string text)
		{
			label.text = text;
		}

		/// <summary>
		/// Mainly called by Unity. Activates or deactivates a marker in the linked chart, depending on the
		/// status of <see cref="UnityEngine.UI.Toggle.isOn" />.
		/// </summary>
		public void Toggle()
		{
			if (Parent == null)
			{
				if (StatusUpdate != null) return;
				var active = toggle.isOn;
				foreach (ScrollViewToggle child in _children) child.Toggle(active);
			}
			else
			{
				LinkedObject.showInChart[_chartContent] = toggle.isOn;
				if (Parent.StatusUpdate == null)
					Parent.StatusUpdate = StartCoroutine(Parent.UpdateStatus());
				if (_chartContent.drawing == null)
					_chartContent.drawing = StartCoroutine(_chartContent.QueueDraw());
			}
		}

		/// <summary>
		/// Activates or deactivates a marker in the linked chart.
		/// </summary>
		/// <param name="active">If the marker will be activated</param>
		public void Toggle(bool active)
		{
			toggle.isOn = active;
			//SetHighlighted(active);
		}

		/// <summary>
		/// Updates the status on parent markers depending on the values of it's children.
		/// </summary>
		/// <returns></returns>
		private IEnumerator UpdateStatus()
		{
			yield return new WaitForSeconds(0.2f);
			var active = true;
			foreach (ScrollViewToggle child in _children)
				if (!child.GetStatus())
				{
					Toggle(false);
					active = false;
					break;
				}

			if (active) Toggle(true);

			StatusUpdate = null;
		}

		/// <summary>
		/// Used to check if a marker for the <see cref="LinkedObject" /> will be added to the linked chart.
		/// </summary>
		/// <returns>The status of the <see cref="LinkedObject" />.</returns>
		private bool GetStatus()
		{
			return (bool) LinkedObject.showInChart[_chartContent];
		}

		/// <summary>
		/// Adds a <see cref="ScrollViewToggle" /> as a child of this <see cref="ScrollViewToggle" />.
		/// </summary>
		/// <param name="child">The new child.</param>
		public void AddChild(ScrollViewToggle child)
		{
			_children.Add(child);
		}

		public void SetHighlighted(bool highlighted)
		{
			var highlightToggle = GetComponent<Toggle>();
			var colors = highlightToggle.colors;
			colors.normalColor = highlighted ? Color.yellow : Color.white;
			highlightToggle.colors = colors;
		}

		/// <summary>
		/// Highlights the <see cref="LinkedObject" /> when the user points on this <see cref="GameObject" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerEnter(PointerEventData eventData)
		{
			if (_chartManager == null || LinkedObject == null) return;
			_pointedOn = true;
			_chartManager.HighlightObject(LinkedObject.gameObject);
		}

		/// <summary>
		/// Stops highlighting the <see cref="LinkedObject" /> when the user stops pointing on it.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerExit(PointerEventData eventData)
		{
			if (!_pointedOn) return;
			_chartManager.HighlightObject(LinkedObject.gameObject);
			SetHighlighted(false);
			_pointedOn = false;
		}

		/// <summary>
		/// If the <see cref="GameObject" /> was still pointed on, the highlight of the
		/// <see cref="LinkedObject" /> will be stopped.
		/// </summary>
		private void OnDestroy()
		{
			if (_pointedOn && LinkedObject != null)
				_chartManager.HighlightObject(LinkedObject.gameObject);
		}
	}
}