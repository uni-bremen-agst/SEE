using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu.Table
{
    /// <summary>
    /// This class provides the modify menu for tables.
    /// </summary>
    public class ModifyTableMenu
	{
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string tableMenuPrefab = "Prefabs/UI/Table/ModifyTableMenu";

        /// <summary>
        /// The instance of the menu.
        /// </summary>
        private readonly GameObject menuInstance;

		/// <summary>
		/// The available operations for modifying a table.
		/// </summary>
		public enum ModifyOperation
		{
			None,
			Move,
			Scale,
			Delete,
			Cancel
		}

		/// <summary>
		/// The operation selecty by the user.
		/// </summary>
		private ModifyOperation selectedOperation = ModifyOperation.None;

		/// <summary>
		/// Indicates whether the user has selected an operation.
		/// </summary>
		private bool gotInput = false;

		/// <summary>
		/// Instantiates the menu and displays it.
		/// </summary>
		public ModifyTableMenu()
		{
			menuInstance = PrefabInstantiator.InstantiatePrefab(tableMenuPrefab,
								UICanvas.Canvas.transform, false);

			ButtonManagerBasic move = menuInstance.FindDescendant("Move")
				.GetComponent<ButtonManagerBasic>();
			move.clickEvent.AddListener(() =>
			{
				ConfirmOperation(ModifyOperation.Move);
			});

            ButtonManagerBasic scale = menuInstance.FindDescendant("Scale")
				.GetComponent<ButtonManagerBasic>();
            scale.clickEvent.AddListener(() =>
            {
				ConfirmOperation(ModifyOperation.Scale);
            });

            ButtonManagerBasic delete = menuInstance.FindDescendant("Delete")
				.GetComponent<ButtonManagerBasic>();
            delete.clickEvent.AddListener(() =>
            {
				ConfirmOperation(ModifyOperation.Delete);
            });

			ButtonManagerBasic cancel = menuInstance.FindDescendant("CancelDragger")
				.GetComponent<ButtonManagerBasic>();
			cancel.clickEvent.AddListener(() =>
			{
				ConfirmOperation(ModifyOperation.Cancel);
			});
        }

		/// <summary>
		/// Sets the selected <paramref name="operation"/> and closes the menu.
		/// </summary>
		/// <param name="operation">The operation that was selected by the user.</param>
		private void ConfirmOperation(ModifyOperation operation)
		{
			gotInput = true;
			selectedOperation = operation;
			Destroy();
		}

		/// <summary>
		/// Disables the menu by destroying it.
		/// </summary>
		public void Destroy()
		{
			if (menuInstance != null)
			{
				Destroyer.Destroy(menuInstance);
			}
		}

        /// <summary>
        /// Fetches the user's selection.
        /// </summary>
        /// <param name="operation">The user's chosen <see cref="ModifyOperation"/>.</param>
        /// <returns>True if an operation was selected, false otherwise.
		/// If true, the selected operation is returned in <paramref name="operation"/>.</returns>
        public bool TryGetInput(out ModifyOperation operation)
		{
			if (gotInput)
			{
				operation = selectedOperation;
				return true;
			}
			operation = ModifyOperation.None;
			return false;
		}
	}
}