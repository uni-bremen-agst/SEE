using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Handles the dragging and minimization of charts.
	/// </summary>
	public class ChartMoveHandler : MonoBehaviour, IDragHandler, IPointerDownHandler,
		IPointerUpHandler
	{
		/// <summary>
		/// Contains user input settings.
		/// </summary>
		private GameManager _gameManager;

		/// <summary>
		/// Contains the position of the chart on the <see cref="Canvas" />.
		/// </summary>
		private RectTransform _chart;

		private RectTransform _screenSize;

		/// <summary>
		/// The time between <see cref="OnPointerDown" /> and <see cref="OnPointerUp" /> to be recognized as
		/// click instead of a drag.
		/// </summary>
		private float _dragDelay;

		/// <summary>
		/// Tracks the time between <see cref="OnPointerDown" /> and <see cref="OnPointerUp" />.
		/// </summary>
		private float _timer;

		/// <summary>
		/// If the pointer is currently down or not.
		/// </summary>
		private bool _pointerDown;

		/// <summary>
		/// If the chart is currently minimized or not.
		/// </summary>
		private bool _minimized;

		[SerializeField] private GameObject _sizeButton = null;

		/// <summary>
		/// Links the <see cref="GameManager" /> and initializes settings with the values from the
		/// <see cref="_gameManager" />.
		/// </summary>
		private void Awake()
		{
			GetSettingData();
			_chart = GameObject.FindGameObjectWithTag("Chart").GetComponent<RectTransform>();
			_screenSize = _chart.transform.parent.parent.GetComponent<RectTransform>();
		}

		private void GetSettingData()
		{
			_gameManager = GameObject.FindGameObjectWithTag("GameManager")
				.GetComponent<GameManager>();
			_dragDelay = _gameManager.DragDelay;
		}

		/// <summary>
		/// Adds the time passed since the last frame to the <see cref="_timer" />
		/// </summary>
		private void Update()
		{
			if (_pointerDown) _timer += Time.deltaTime;
		}

		/// <summary>
		/// Checks the current <see cref="Input.mousePosition" /> and moves the chart to the corresponding
		/// position on the canvas.
		/// </summary>
		/// <param name="eventData">Event payload associated with pointer (mouse / touch) events.</param>
		public void OnDrag(PointerEventData eventData)
		{
			RectTransform pos = GetComponent<RectTransform>();
			Vector2 newPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			if (newPosition.x > 0 &&
			    newPosition.x < _screenSize.sizeDelta.x * _screenSize.lossyScale.x &&
			    newPosition.y > 0 &&
			    newPosition.y < _screenSize.sizeDelta.y * _screenSize.lossyScale.y)
				_chart.position =
					new Vector2(newPosition.x - pos.anchoredPosition.x * pos.lossyScale.x,
						newPosition.y - pos.anchoredPosition.y * pos.lossyScale.y);
		}

		/// <summary>
		/// Starts the pointer down timer.
		/// </summary>
		/// <param name="eventData">Event payload associated with pointer (mouse / touch) events.</param>
		public void OnPointerDown(PointerEventData eventData)
		{
			_timer = 0f;
			_pointerDown = true;
		}

		/// <summary>
		/// Stops the pointer down timer and triggers a click depending on the time the pointer was down for.
		/// </summary>
		/// <param name="eventData">Event payload associated with pointer (mouse / touch) events.</param>
		public void OnPointerUp(PointerEventData eventData)
		{
			_pointerDown = false;
			if (_timer < _dragDelay) ToggleMinimize();
		}

		/// <summary>
		/// Toggles the minimization of the chart.
		/// </summary>
		private void ToggleMinimize()
		{
			ChartContent chart = _chart.GetComponent<ChartContent>();
			chart.LabelsPanel.gameObject.SetActive(_minimized);
			chart.DataPanel.gameObject.SetActive(_minimized);
			_sizeButton.SetActive(_minimized);
			_minimized = !_minimized;
		}
	}
}