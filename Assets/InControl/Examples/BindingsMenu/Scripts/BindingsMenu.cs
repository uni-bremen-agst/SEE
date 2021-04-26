using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using InControl;


public class BindingsMenu : MonoBehaviour
{
	public GameObject BindingButtonPrefab;

	readonly List<BindingButton> bindingButtons = new List<BindingButton>();

	VerticalLayoutGroup bindingsLayoutGroup;
	CanvasGroup canvasGroup;
	PlayerActionSet actionSet;


	void Awake()
	{
		// Find the container (layout group) that will contain the bindings buttons.
		bindingsLayoutGroup = GetComponentInChildren<VerticalLayoutGroup>( true );

		// Find the canvas group for this menu.
		canvasGroup = GetComponent<CanvasGroup>();

		// Start disabled and hidden.
		gameObject.SetActive( false );
	}


	public void Show( PlayerActionSet actionSet )
	{
		this.actionSet = actionSet;

		// If trying to show an already visible menu, just bail out.
		if (gameObject.activeSelf)
		{
			Debug.LogWarningFormat( "{0} is already showing.", gameObject.name );
			return;
		}

		// Create buttons for each of the customizable controls.
		CreateBindingButtons();

		// Now that everything is created, enable and show the menu.
		gameObject.SetActive( true );

		// Find the first button and make it the first selected control.
		if (bindingButtons.Count > 0)
		{
			var buttonGameObject = bindingButtons[0].gameObject;
			EventSystem.current.firstSelectedGameObject = buttonGameObject;
			EventSystem.current.SetSelectedGameObject( buttonGameObject );
		}
	}


	public void Hide()
	{
		// If trying to hide an already hidden menu, just bail out.
		if (!gameObject.activeSelf)
		{
			Debug.LogWarningFormat( "{0} is already hidden.", gameObject.name );
			return;
		}

		// Delete the buttons we made for the customizable controls.
		DeleteBindingButtons();

		// Disable and hide the menu.
		gameObject.SetActive( false );
	}


	// This dynamically creates buttons (using the assigned prefab) based on
	// actions in our action set and places the buttons into the vertical layout group.
	// The actual management of the button and rebinding process for each action
	// will be handled by the BindingButton script on each button.
	void CreateBindingButtons()
	{
		Debug.Assert( InputManager.IsSetup, "InputManager.IsSetup == true" );
		Debug.Assert( actionSet != null, "actionSet != null" );

		// Iterate over game actions and create bindings buttons in the menu dynamically.
		foreach (var action in actionSet.Actions)
		{
			// Duplicate the button prefab and add it to the bindings container.
			var go = Instantiate( BindingButtonPrefab, Vector3.zero, Quaternion.identity );
			go.name = "Button - " + action.Name;
			go.transform.SetParent( bindingsLayoutGroup.transform );

			// Set text labels for control and binding.
			var bindingButton = go.GetComponent<BindingButton>();
			bindingButton.Setup( action, canvasGroup );

			// Save it for later.
			bindingButtons.Add( bindingButton );
		}
	}


	void DeleteBindingButtons()
	{
		foreach (var bindingButton in bindingButtons)
		{
			Destroy( bindingButton.gameObject );
		}

		bindingButtons.Clear();
	}


	public void ResetBindings()
	{
		Debug.Assert( InputManager.IsSetup, "InputManager.IsSetup == true" );
		Debug.Assert( actionSet != null, "actionSet != null" );

		actionSet.Reset();
	}
}
