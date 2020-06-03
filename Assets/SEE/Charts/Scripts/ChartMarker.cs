using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Charts.Scripts
{
	/// <summary>
	/// Contains the logic for the markers representing entries linked to objects in the chart.
	/// </summary>
	public class ChartMarker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		/// <summary>
		/// Contains some settings used in this script.
		/// </summary>
		private ChartManager _chartManager;

		//User Variables from ChartManager
		private float _cameraDistance;
		private bool _moveWithRotation;
		private float _cameraFlightTime;
		private float _clickDelay;
		private float _highlightDuration;

		/// <summary>
		/// The <see cref="Material" /> making the object look highlighted.
		/// </summary>
		private Material _buildingHighlightMaterial;

		/// <summary>
		/// The <see cref="Material" /> making the object look accentuated.
		/// </summary>
		private Material _buildingHighlightMaterialAccentuated;

		/// <summary>
		/// Copy of the <see cref="linkedObject" /> with different material to make it look highlighted.
		/// </summary>
		private GameObject _highlightCopy;

		/// <summary>
		/// The <see cref="GameObject" /> in the code city that is connected with this button.
		/// </summary>
		[HideInInspector] public GameObject linkedObject;

		/// <summary>
		/// The toggle linked to this marker.
		/// </summary>
		public ScrollViewToggle ScrollViewToggle { private get; set; }

		/// <summary>
		/// The active <see cref="Camera" /> in the scene.
		/// </summary>
		private Camera _activeCamera;

		/// <summary>
		/// The currently running camera movement <see cref="Coroutine" />.
		/// </summary>
		private Coroutine _cameraMoving;

		/// <summary>
		/// Determines if a second click happened during <see cref="_clickDelay" />.
		/// </summary>
		private bool _waiting;

		/// <summary>
		/// Determines if <see cref="WaitForDoubleClick" /> is currently running.
		/// </summary>
		private bool _runningClick;

		/// <summary>
		/// The currently running <see cref="TimedHighlightRoutine" />.
		/// </summary>
		public Coroutine TimedHighlight { get; private set; }

		/// <summary>
		/// Counts the time <see cref="TimedHighlight" /> has been running for.
		/// </summary>
		public float HighlightTime { get; private set; }

		/// <summary>
		/// Tells if <see cref="TimedHighlight" /> was running when script was deactivated due to minimization
		/// of the chart.
		/// </summary>
		private bool _reactivateHighlight;

		/// <summary>
		/// The <see cref="GameObject" /> making the marker look highlighted when active.
		/// </summary>
		[Header("Highlight Properties"), SerializeField]
		private GameObject markerHighlight;

		/// <summary>
		/// The prefab containing the <see cref="LineRenderer" /> that creates the beam above highlighted
		/// objects.
		/// </summary>
		[SerializeField] private GameObject highlightLine;

		/// <summary>
		/// The length of the beam appearing above highlighted objects.
		/// </summary>
		private float _highlightLineLength;

		/// <summary>
		/// Stores if the marker is accentuated.
		/// </summary>
		private bool _accentuated;

		/// <summary>
		/// A text popup containing useful information about the marker and its <see cref="linkedObject" />.
		/// </summary>
		[Header("Other"), SerializeField] private TextMeshProUGUI infoText;

		/// <summary>
		/// Calls methods for initialization.
		/// </summary>
		private void Awake()
		{
			GetSettingData();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			_cameraDistance = _chartManager.cameraDistance;
			_moveWithRotation = _chartManager.moveWithRotation;
			_cameraFlightTime = _chartManager.cameraFlightTime;
			_clickDelay = _chartManager.clickDelay;
			_highlightDuration = _chartManager.highlightDuration;
			_buildingHighlightMaterial = _chartManager.buildingHighlightMaterial;
			_buildingHighlightMaterialAccentuated =
				_chartManager.buildingHighlightMaterialAccentuated;
			_highlightLineLength = _chartManager.highlightLineLength;
		}

		/// <summary>
		/// Reactivates the highlight if a previous marker linked to the same <see cref="linkedObject" />
		/// highlighted it.
		/// </summary>
		private void Start()
		{
			for (var i = 0; i < linkedObject.transform.childCount; i++)
			{
				var child = linkedObject.transform.GetChild(i);
				if (!child.name.Equals(linkedObject.name + "(Clone)")) continue;
				TriggerTimedHighlight(_chartManager.highlightDuration, false, false);
				break;
			}
		}

		/// <summary>
		/// Adds the time that passed since the last <see cref="Update" /> to the <see cref="HighlightTime" />.
		/// </summary>
		private void Update()
		{
			if (TimedHighlight != null) HighlightTime += Time.deltaTime;
		}

		/// <summary>
		/// Called by Unity when the button assigned to the <see cref="ChartMarker" /> is pressed.
		/// </summary>
		public void ButtonClicked()
		{
			if (_runningClick)
				_waiting = false;
			else
				StartCoroutine(WaitForDoubleClick());
		}

		/// <summary>
		/// Checks if one or two clicks happen in a given interval.
		/// </summary>
		/// <returns></returns>
		private IEnumerator WaitForDoubleClick()
		{
			_runningClick = true;
			_waiting = true;
			yield return new WaitForSeconds(_clickDelay);
			if (_waiting)
			{
				ChartManager.HighlightObject(linkedObject, false);
			}
			else
			{
				ShowLinkedObject();
				HighlightLinkedObjectToggle(true);
			}

			_waiting = false;
			_runningClick = false;
		}

		/// <summary>
		/// Toggles the highlight of the <see cref="linkedObject" /> and this marker.
		/// </summary>
		private void HighlightLinkedObjectToggle(bool highlight)
		{
			if (highlight)
			{
				var highlighted = false;

				for (var i = 0; i < linkedObject.transform.childCount; i++)
					if (linkedObject.transform.GetChild(i).gameObject.name
						.Equals(linkedObject.name + "(Clone)"))
						highlighted = true;

				if (!highlighted)
				{
					_highlightCopy = Instantiate(linkedObject, linkedObject.transform);
					_highlightCopy.tag = "Untagged";
					_highlightCopy.transform.localScale = Vector3.one;
					_highlightCopy.transform.localPosition = Vector3.zero;
					if (_highlightCopy.TryGetComponent<Renderer>(out var render))
						render.material = _buildingHighlightMaterial;
					Instantiate(highlightLine, _highlightCopy.transform)
						.TryGetComponent<LineRenderer>(out var line);
					var linePos = _highlightCopy.transform.localPosition;
					line.SetPositions(new[]
					{
						linePos,
						linePos + new Vector3(0f, _highlightLineLength) /
						_highlightCopy.transform.lossyScale.y
					});
				}
			}
			else
			{
				if (_highlightCopy) Destroy(_highlightCopy);
				_accentuated = false;
			}

			markerHighlight.SetActive(highlight);
			if (ScrollViewToggle) ScrollViewToggle.SetHighlighted(highlight);
		}

		/// <summary>
		/// Toggles the highlights this marker and its <see cref="linkedObject" /> for a given amount of time.
		/// Reactivates it after deactivation if <see cref="ChartManager.selectionMode" /> is toggled on
		/// </summary>
		/// <param name="time">How long the highlight will last.</param>
		/// <param name="reenable">
		/// Overwrites the selection mode to reactivate the highlight after
		/// deactivation.
		/// </param>
		/// <param name="scrollView">If this is triggered by a <see cref="ScrollViewToggle" /> or not.</param>
		public void TriggerTimedHighlight(float time, bool reenable, bool scrollView)
		{
			if (scrollView)
			{
				if (TimedHighlight != null) return;
				HighlightLinkedObjectToggle(!_highlightCopy);
			}
			else
			{
				var reactivate = false;

				if (TimedHighlight != null)
				{
					StopCoroutine(TimedHighlight);
					HighlightLinkedObjectToggle(false);
					TimedHighlight = null;
					if (_chartManager.selectionMode || reenable) reactivate = true;
				}
				else
				{
					reactivate = true;
				}

				if (reactivate) TimedHighlight = StartCoroutine(TimedHighlightRoutine(time));
			}
		}

		/// <summary>
		/// The <see cref="Coroutine" /> stopping the highlight after the given time has passed.
		/// </summary>
		/// <param name="time">The time after which to stop the highlight.</param>
		/// <returns></returns>
		private IEnumerator TimedHighlightRoutine(float time)
		{
			if (time < _highlightDuration)
				HighlightTime = _highlightDuration - time;
			else
				HighlightTime = 0f;

			HighlightLinkedObjectToggle(true);
			yield return new WaitForSeconds(time);
			while (_chartManager.selectionMode) yield return new WaitForEndOfFrame();
			HighlightLinkedObjectToggle(false);
			TimedHighlight = null;
		}

		/// <summary>
		/// Moves the camera to view the <see cref="linkedObject" />.
		/// </summary>
		private void ShowLinkedObject()
		{
			_activeCamera = Camera.main;
			var cameraPos = _activeCamera.transform.position;

			if (_moveWithRotation)
			{
				if (_cameraMoving != null)
				{
					StopCoroutine(_cameraMoving);
					_cameraMoving = null;
				}

				var linkedPos = linkedObject.transform.position;
				var lookPos = linkedPos - cameraPos;
				_cameraMoving = StartCoroutine(MoveCameraTo(
					Vector3.MoveTowards(cameraPos, linkedPos, lookPos.magnitude - _cameraDistance),
					Quaternion.LookRotation(lookPos)));
			}
			else
			{
				if (_cameraMoving != null)
				{
					StopCoroutine(_cameraMoving);
					_cameraMoving = null;
				}

				var pos = linkedObject.transform.position;
				_cameraMoving =
					StartCoroutine(MoveCameraTo(new Vector3(pos.x, cameraPos.y,
						pos.z - _cameraDistance)));
			}
		}

		/// <summary>
		/// Moves the <see cref="Camera" /> smoothly from one position to another and rotates it to look
		/// towards a specified position.
		/// </summary>
		/// <param name="newPos">The target position.</param>
		/// <param name="lookAt">The position to look at.</param>
		/// <returns></returns>
		private IEnumerator MoveCameraTo(Vector3 newPos, Quaternion lookAt)
		{
			var oldPos = _activeCamera.transform.position;
			if (newPos == linkedObject.transform.position) yield break;
			var oldRot = _activeCamera.transform.rotation;
			for (var time = 0f; time <= _cameraFlightTime; time += Time.deltaTime)
			{
				_activeCamera.transform.position =
					Vector3.Lerp(oldPos, newPos, time * (1 / _cameraFlightTime));
				_activeCamera.transform.rotation =
					Quaternion.Slerp(oldRot, lookAt, time * (1 / _cameraFlightTime));
				yield return new WaitForEndOfFrame();
			}

			var cameraPos = _activeCamera.transform;
			cameraPos.rotation = lookAt;
			cameraPos.transform.position = newPos;
		}

		/// <summary>
		/// Moves the camera smoothly from one position to another without rotation.
		/// </summary>
		/// <param name="newPos">The target position.</param>
		/// <returns></returns>
		private IEnumerator MoveCameraTo(Vector3 newPos)
		{
			var oldPos = _activeCamera.transform.position;
			for (float time = 0; time <= _cameraFlightTime; time += Time.deltaTime)
			{
				_activeCamera.transform.position =
					Vector3.Lerp(oldPos, newPos, time * (1 / _cameraFlightTime));
				yield return new WaitForEndOfFrame();
			}

			_activeCamera.transform.position = newPos;
		}

		/// <summary>
		/// Changes the color of the marker and the <see cref="linkedObject" /> to the accentuation color.
		/// </summary>
		public void Accentuate()
		{
			markerHighlight.TryGetComponent<Image>(out var image);
			image.color = _accentuated
				? _chartManager.standardColor
				: _chartManager.accentuationColor;
			_accentuated = !_accentuated;
			if (!_highlightCopy) return;
			_highlightCopy.TryGetComponent<Renderer>(out var render);
			render.material = _accentuated
				? _buildingHighlightMaterialAccentuated
				: _buildingHighlightMaterial;
		}

		/// <summary>
		/// Changes the <see cref="infoText" /> of this marker.
		/// </summary>
		/// <param name="info">The new text.</param>
		public void SetInfoText(string info)
		{
			infoText.text = info;
		}

		/// <summary>
		/// Activates the <see cref="infoText" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerEnter(PointerEventData eventData)
		{
			infoText.gameObject.SetActive(true);
			if (TimedHighlight != null) ChartManager.Accentuate(linkedObject);
		}

		/// <summary>
		/// Deactivates the <see cref="infoText" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerExit(PointerEventData eventData)
		{
			infoText.gameObject.SetActive(false);
			if (_accentuated) ChartManager.Accentuate(linkedObject);
		}

		/// <summary>
		/// If <see cref="TimedHighlight" /> was running, this will be saved to
		/// <see cref="_reactivateHighlight" />.
		/// </summary>
		private void OnDisable()
		{
			if (TimedHighlight != null) _reactivateHighlight = true;
		}

		/// <summary>
		/// Reactivates the highlight if it was running before disable.
		/// </summary>
		private void OnEnable()
		{
			if (!_reactivateHighlight) return;
			TriggerTimedHighlight(_highlightDuration - HighlightTime, true, false);
			_reactivateHighlight = false;
		}

		/// <summary>
		/// Destroys the <see cref="_highlightCopy" /> if it exists.
		/// </summary>
		private void OnDestroy()
		{
			if (_highlightCopy != null) Destroy(_highlightCopy);
			StopAllCoroutines();
		}
	}
}