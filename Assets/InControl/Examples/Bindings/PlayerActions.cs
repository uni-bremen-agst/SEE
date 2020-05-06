namespace BindingsExample
{
	using InControl;
	using UnityEngine;


	public class PlayerActions : PlayerActionSet
	{
		public readonly PlayerAction Fire;
		public readonly PlayerAction Jump;
		public readonly PlayerAction Left;
		public readonly PlayerAction Right;
		public readonly PlayerAction Up;
		public readonly PlayerAction Down;
		public readonly PlayerTwoAxisAction Move;


		public PlayerActions()
		{
			Fire = CreatePlayerAction( "Fire" );
			Jump = CreatePlayerAction( "Jump" );
			Left = CreatePlayerAction( "Move Left" );
			Right = CreatePlayerAction( "Move Right" );
			Up = CreatePlayerAction( "Move Up" );
			Down = CreatePlayerAction( "Move Down" );
			Move = CreateTwoAxisPlayerAction( Left, Right, Down, Up );
		}


		public static PlayerActions CreateWithDefaultBindings()
		{
			var playerActions = new PlayerActions();

			// How to set up mutually exclusive keyboard bindings with a modifier key.
			// playerActions.Back.AddDefaultBinding( Key.Shift, Key.Tab );
			// playerActions.Next.AddDefaultBinding( KeyCombo.With( Key.Tab ).AndNot( Key.Shift ) );

			playerActions.Fire.AddDefaultBinding( Key.A );
			playerActions.Fire.AddDefaultBinding( InputControlType.Action1 );
			// playerActions.Fire.AddDefaultBinding( Mouse.LeftButton );

			playerActions.Jump.AddDefaultBinding( Key.Space );
			playerActions.Jump.AddDefaultBinding( InputControlType.Action3 );
			playerActions.Jump.AddDefaultBinding( InputControlType.Back );

			playerActions.Up.AddDefaultBinding( Key.UpArrow );
			playerActions.Down.AddDefaultBinding( Key.DownArrow );
			playerActions.Left.AddDefaultBinding( Key.LeftArrow );
			playerActions.Right.AddDefaultBinding( Key.RightArrow );

			playerActions.Left.AddDefaultBinding( InputControlType.LeftStickLeft );
			playerActions.Right.AddDefaultBinding( InputControlType.LeftStickRight );
			playerActions.Up.AddDefaultBinding( InputControlType.LeftStickUp );
			playerActions.Down.AddDefaultBinding( InputControlType.LeftStickDown );

			playerActions.Left.AddDefaultBinding( InputControlType.DPadLeft );
			playerActions.Right.AddDefaultBinding( InputControlType.DPadRight );
			playerActions.Up.AddDefaultBinding( InputControlType.DPadUp );
			playerActions.Down.AddDefaultBinding( InputControlType.DPadDown );

			playerActions.Up.AddDefaultBinding( Mouse.PositiveY );
			playerActions.Down.AddDefaultBinding( Mouse.NegativeY );
			playerActions.Left.AddDefaultBinding( Mouse.NegativeX );
			playerActions.Right.AddDefaultBinding( Mouse.PositiveX );

			playerActions.ListenOptions.IncludeUnknownControllers = true;
			playerActions.ListenOptions.MaxAllowedBindings = 4;
			//playerActions.ListenOptions.MaxAllowedBindingsPerType = 1;
			//playerActions.ListenOptions.AllowDuplicateBindingsPerSet = true;
			playerActions.ListenOptions.UnsetDuplicateBindingsOnSet = true;
			//playerActions.ListenOptions.IncludeMouseButtons = true;
			//playerActions.ListenOptions.IncludeModifiersAsFirstClassKeys = true;
			//playerActions.ListenOptions.IncludeMouseScrollWheel = true;

			playerActions.ListenOptions.OnBindingFound = ( action, binding ) =>
			{
				if (binding == new KeyBindingSource( Key.Escape ))
				{
					action.StopListeningForBinding();
					return false;
				}

				return true;
			};

			playerActions.ListenOptions.OnBindingAdded += ( action, binding ) => { Debug.Log( "Binding added... " + binding.DeviceName + ": " + binding.Name ); };

			playerActions.ListenOptions.OnBindingRejected += ( action, binding, reason ) => { Debug.Log( "Binding rejected... " + reason ); };

			return playerActions;
		}
	}
}
