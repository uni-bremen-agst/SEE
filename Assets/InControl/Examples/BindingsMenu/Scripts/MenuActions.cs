using InControl;


// This represents the actions that can be used to navigate and use the UI.
// It's a good idea to have a separate action set for your UI interactions.
// It isolates controls that are not customizable by the player, so even if
// they bind weird things to the game controls, the UI is still workable and
// they can fix or reset the game controls.
public class MenuActions : PlayerActionSet
{
	public readonly PlayerAction Up;
	public readonly PlayerAction Down;
	public readonly PlayerAction Left;
	public readonly PlayerAction Right;
	public readonly PlayerTwoAxisAction Move;

	public readonly PlayerAction Submit;
	public readonly PlayerAction Cancel;

	// We don't actually use this in the example, but likely there is a
	// control to trigger the menu while in game.
	// public readonly PlayerAction OpenMenu;


	public MenuActions()
	{
		Up = CreatePlayerAction( "Move Up" );
		Down = CreatePlayerAction( "Move Down" );
		Left = CreatePlayerAction( "Move Left" );
		Right = CreatePlayerAction( "Move Right" );
		Move = CreateTwoAxisPlayerAction( Left, Right, Down, Up );

		Submit = CreatePlayerAction( "Submit" );
		Cancel = CreatePlayerAction( "Cancel" );

		// OpenMenu = CreatePlayerAction( "OpenMenu" );
	}


	public static MenuActions CreateWithDefaultBindings()
	{
		var actions = new MenuActions();

		actions.Up.AddDefaultBinding( Key.UpArrow );
		actions.Down.AddDefaultBinding( Key.DownArrow );
		actions.Left.AddDefaultBinding( Key.LeftArrow );
		actions.Right.AddDefaultBinding( Key.RightArrow );

		actions.Left.AddDefaultBinding( InputControlType.LeftStickLeft );
		actions.Right.AddDefaultBinding( InputControlType.LeftStickRight );
		actions.Up.AddDefaultBinding( InputControlType.LeftStickUp );
		actions.Down.AddDefaultBinding( InputControlType.LeftStickDown );

		actions.Left.AddDefaultBinding( InputControlType.DPadLeft );
		actions.Right.AddDefaultBinding( InputControlType.DPadRight );
		actions.Up.AddDefaultBinding( InputControlType.DPadUp );
		actions.Down.AddDefaultBinding( InputControlType.DPadDown );

		actions.Submit.AddDefaultBinding( Key.Return );
		actions.Submit.AddDefaultBinding( Key.Space );
		actions.Submit.AddDefaultBinding( InputControlType.Action1 );

		actions.Cancel.AddDefaultBinding( Key.Escape );
		actions.Cancel.AddDefaultBinding( InputControlType.Action2 );

		// actions.OpenMenu.AddDefaultBinding( Key.Escape );
		// actions.OpenMenu.AddDefaultBinding( InputControlType.Command );

		return actions;
	}
}
