using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using InControl;


public class BindingButton : MonoBehaviour
{
	// This is a reference to the action this button will be managing bindings for.
	PlayerAction action;

	// We also need to keep a reference to the canvas group (the bindings menu)
	// so we can make it non-interactive while rebinding controls.
	CanvasGroup canvasGroup;

	// The button control itself. We use this to hook into its click event
	// and to do some animation of its appearance while listening for bindings.
	Button button;

	// This will be a reference to the text on the right side of the button
	// that displays the binding name. We also animate some ellipses on it
	// while listening to provide some UI feedback.
	Text bindingText;


	public void Setup( PlayerAction action, CanvasGroup canvasGroup )
	{
		this.action = action;
		this.canvasGroup = canvasGroup;

		button = GetComponent<Button>();
		button.onClick.AddListener( OnClick );

		// Find the control text and set it to the action name, e.g. "Attack"
		var controlText = transform.Find( "Control" ).GetComponent<Text>();
		controlText.text = action.Name;

		// Find the current control binding text, set it to the current binding
		// name and hold onto the field so we can update it later.
		bindingText = transform.Find( "Binding" ).GetComponent<Text>();
		bindingText.text = GetBindingName();

		// Set up the listen options for this action. For this example, we limit
		// the action to a single binding and we'll unset duplicate bindings, so
		// we can "steal" bindings from other controls without having to clear
		// them first. We also set up some callbacks into the listening process
		// so we can manage the process and update the UI accordingly.
		action.ListenOptions = new BindingListenOptions
		{
			MaxAllowedBindings = 1,
			UnsetDuplicateBindingsOnSet = true,
			IncludeModifiersAsFirstClassKeys = true,
			OnBindingFound = OnBindingFound,
			OnBindingRejected = OnBindingRejected,
			OnBindingEnded = OnBindingEnded,
		};

		action.OnBindingsChanged -= OnBindingsChanged;
		action.OnBindingsChanged += OnBindingsChanged;
	}


	void OnClick()
	{
		// We shouldn't be able to get a click call here since we make the
		// parent canvas group non-interactive, but just to be safe we check
		// the action isn't listening already.
		if (!action.IsListeningForBinding)
		{
			// Make the parent canvas group (the whole bindings menu) non-interactive.
			canvasGroup.interactable = false;

			// Start listening for a new binding.
			action.ListenForBinding();

			// Start some animations to give UI feedback to the user.
			StartCoroutine( AnimateButtonText() );
			StartCoroutine( AnimateButtonColor() );
		}
	}


	void Update()
	{
		// Check whether we need to cancel the listening process.
		// Note: we could probably do something more elegant than using a
		// global singleton to get at the menu actions, but that's
		// beyond the scope of this example.
		if (MenuManager.Instance.MenuActions.Cancel.IsPressed)
		{
			action.StopListeningForBinding();
		}
	}


	static bool OnBindingFound( PlayerAction action, BindingSource binding )
	{
		// If you need to ignore a specific binding, just return false and the
		// system will keep listening.
		// if (binding == new DeviceBindingSource( InputControlType.Action2 ))
		// {
		// 	return false;
		// }

		// Check whether we need to cancel this listening process. We do it both
		// here and in Update() since there's a chance OnBindingFound() could be
		// triggered by the cancel press before Update() has a chance to cancel.
		if (MenuManager.Instance.MenuActions.Cancel.IsPressed)
		{
			action.StopListeningForBinding();
			return false;
		}

		// This allows the binding process to be cancelled with a global button like ESC,
		// but generally the cancellation process can be triggered by another control
		// like MenuActions.Cancel in this example.
		if (binding == new KeyBindingSource( Key.Escape ))
		{
			action.StopListeningForBinding();
			return false;
		}

		return true;
	}


	// This is called if a binding is rejected while listening, usually due to the
	// rules set up in the listen options. It's unlikely to get called in this
	// example since we have UnsetDuplicateBindingsOnSet set to true.
	static void OnBindingRejected( PlayerAction action, BindingSource binding, BindingSourceRejectionType rejectionType )
	{
		// This isn't strictly necessary, but maybe we could do a brief flashing red
		// indicator or warning message to tell the user what's going on. We don't
		// actually need to stop listening either, but we do for this example.
		action.StopListeningForBinding();
	}


	// This is called when the binding listening process has finished so we can
	// do some cleanup, update the UI and make the menu interactive again.
	void OnBindingEnded( PlayerAction action )
	{
		bindingText.text = GetBindingName();
		canvasGroup.interactable = true;
	}


	// This is called when any of the bindings on the action changes, specifically
	// when the binding is "stolen" from another control due to the UnsetDuplicateBindingsOnSet
	// listen option, it ensures its binding text gets updated accordingly.
	void OnBindingsChanged()
	{
		bindingText.text = GetBindingName();
	}


	// This is a utility method to get the binding name.
	// In this example, we have MaxAllowedBindings = 1 set on the listen options
	// so we can make certain assumptions. As long as there is at least 1 binding
	// we just return the name of the first one, otherwise "N/A".
	string GetBindingName()
	{
		if (action.Bindings.Count > 0)
		{
			return action.Bindings[0].Name;
		}

		return "N/A";
	}


	// This little coroutine animates an ellipsis "..." in the binding text to
	// show the user we're listening for a binding.
	public IEnumerator AnimateButtonText()
	{
		while (action.IsListeningForBinding)
		{
			var count = Mathf.FloorToInt( Time.realtimeSinceStartup * 5 % 4 );
			bindingText.text = new string( '.', count );
			yield return new WaitForEndOfFrame();
		}
	}


	// This little coroutine animates the button background color to show the
	// user we're listening for a binding.
	public IEnumerator AnimateButtonColor()
	{
		var savedColors = button.colors;

		while (action.IsListeningForBinding)
		{
			var colors = button.colors;
			colors.disabledColor = new Color( 1, 1, 1, Mathf.Sin( Time.realtimeSinceStartup * 8.0f ) * 0.1f + 0.6f );
			button.colors = colors;
			yield return new WaitForEndOfFrame();
		}

		button.colors = savedColors;
	}
}
