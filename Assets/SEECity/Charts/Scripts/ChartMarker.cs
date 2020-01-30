using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts
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
		/// Copy of the <see cref="linkedObject" /> with different material to make it look highlighted.
		/// </summary>
		private GameObject _highlightCopy;

		/// <summary>
		/// The <see cref="GameObject" /> in the code city that is connected with this button.
		/// </summary>
		[HideInInspector] public GameObject linkedObject;

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
		/// The lenght of the beam appearing above highlighted objects.
		/// </summary>
		private float _highlightLineLength;

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
			_highlightLineLength = _chartManager.highlightLineLength;
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
				TriggerTimedHighlight(_highlightDuration);
			}
			else
			{
				ShowLinkedObject();
				HighlightLinkedObjectToggle(true); //TODO: Deactivate when user switches target.
			}

			_waiting = false;
			_runningClick = false;
		}

		/// <summary>
		/// Highlights the <see cref="linkedObject" />.
		/// </summary>
		private void HighlightLinkedObjectToggle(bool highlight)
		{
			if (highlight)
			{
				_highlightCopy = Instantiate(linkedObject, linkedObject.transform);
				_highlightCopy.tag = "Untagged";
				_highlightCopy.GetComponent<Renderer>().material = _buildingHighlightMaterial;
				LineRenderer line = Instantiate(highlightLine, _highlightCopy.transform)
					.GetComponent<LineRenderer>();
				Vector3 linePos = _highlightCopy.transform.localPosition;
				line.SetPositions(new[] {linePos, linePos + new Vector3(0f, _highlightLineLength)});
			}
			else
			{
				if (_highlightCopy != null) Destroy(_highlightCopy);
			}

			markerHighlight.SetActive(highlight);
		}

		/// <summary>
		/// Highlights this marker and its <see cref="linkedObject" /> for a given amount of time.
		/// </summary>
		/// <param name="time">How long the highlight will last.</param>
		public void TriggerTimedHighlight(float time)
		{
			bool reactivate = false;

			if (TimedHighlight != null)
			{
				StopCoroutine(TimedHighlight);
				HighlightLinkedObjectToggle(false);
				TimedHighlight = null;
				if (_chartManager.selectionMode) reactivate = true;
			}

			if (!reactivate) TimedHighlight = StartCoroutine(TimedHighlightRoutine(time));
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
			_activeCamera = Camera.main.GetComponent<Camera>();
			//TODO: Change to active camera and not just main camera.
			Vector3 cameraPos = _activeCamera.transform.position;

			if (_moveWithRotation)
			{
				if (_cameraMoving != null)
				{
					StopCoroutine(_cameraMoving);
					_cameraMoving = null;
				}

				Vector3 lookPos = linkedObject.transform.position - cameraPos;
				_cameraMoving = StartCoroutine(MoveCameraTo(
					Vector3.MoveTowards(cameraPos, linkedObject.transform.position,
						lookPos.magnitude - _cameraDistance), Quaternion.LookRotation(lookPos)));
			}
			else
			{
				if (_cameraMoving != null)
				{
					StopCoroutine(_cameraMoving);
					_cameraMoving = null;
				}

				Vector3 pos = linkedObject.transform.position;
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
			Vector3 oldPos = _activeCamera.transform.position;
			if (newPos != linkedObject.transform.position)
			{
				Quaternion oldRot = _activeCamera.transform.rotation;
				for (float time = 0f; time <= _cameraFlightTime; time += Time.deltaTime)
				{
					_activeCamera.transform.position =
						Vector3.Lerp(oldPos, newPos, time * (1 / _cameraFlightTime));
					_activeCamera.transform.rotation =
						Quaternion.Slerp(oldRot, lookAt, time * (1 / _cameraFlightTime));
					yield return new WaitForEndOfFrame();
				}

				_activeCamera.transform.rotation = lookAt;
				_activeCamera.transform.position = newPos;
			}
		}

		/// <summary>
		/// Moves the camera smoothly from one position to another without rotation.
		/// </summary>
		/// <param name="newPos">The target position.</param>
		/// <returns></returns>
		private IEnumerator MoveCameraTo(Vector3 newPos)
		{
			Vector3 oldPos = _activeCamera.transform.position;
			for (float time = 0; time <= _cameraFlightTime; time += Time.deltaTime)
			{
				_activeCamera.transform.position =
					Vector3.Lerp(oldPos, newPos, time * (1 / _cameraFlightTime));
				yield return new WaitForEndOfFrame();
			}

			_activeCamera.transform.position = newPos;
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
		}

		/// <summary>
		/// Deactivates the <see cref="infoText" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerExit(PointerEventData eventData)
		{
			infoText.gameObject.SetActive(false);
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
			if (_reactivateHighlight)
			{
				TriggerTimedHighlight(_highlightDuration - HighlightTime);
				_reactivateHighlight = false;
			}
		}

		/// <summary>
		/// Destroys the <see cref="_highlightCopy" /> if it exists.
		/// </summary>
		private void OnDestroy()
		{
			if (_highlightCopy != null) Destroy(_highlightCopy);
		}
	}
}