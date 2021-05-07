using InControl;


// This represents some fictional actions which will appear in the bindings menu,
// and are the controls that the player will be able to customize.
public class GameActions : PlayerActionSet
{
	public readonly PlayerAction Up;
	public readonly PlayerAction Down;
	public readonly PlayerAction Left;
	public readonly PlayerAction Right;
	public readonly PlayerTwoAxisAction Move;

	public readonly PlayerAction Attack;
	public readonly PlayerAction Defend;


	public GameActions()
	{
		Up = CreatePlayerAction( "Move Up" );
		Down = CreatePlayerAction( "Move Down" );
		Left = CreatePlayerAction( "Move Left" );
		Right = CreatePlayerAction( "Move Right" );
		Move = CreateTwoAxisPlayerAction( Left, Right, Down, Up );

		Attack = CreatePlayerAction( "Attack" );
		Defend = CreatePlayerAction( "Defend" );
	}


	public static GameActions CreateWithDefaultBindings()
	{
		var actions = new GameActions();

		actions.Up.AddDefaultBinding( Key.UpArrow );
		actions.Down.AddDefaultBinding( Key.DownArrow );
		actions.Left.AddDefaultBinding( Key.LeftArrow );
		actions.Right.AddDefaultBinding( Key.RightArrow );

		actions.Attack.AddDefaultBinding( Key.Space );
		actions.Defend.AddDefaultBinding( Key.LeftAlt );

		return actions;
	}
}
