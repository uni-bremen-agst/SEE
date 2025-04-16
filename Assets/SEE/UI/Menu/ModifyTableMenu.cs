using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu
{
	/// <summary>
	///
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
		///
		/// </summary>
		public ModifyTableMenu()
		{
			menuInstance = PrefabInstantiator.InstantiatePrefab(tableMenuPrefab,
								UICanvas.Canvas.transform, false);

			ButtonManagerBasic move = GameFinder.FindChild(menuInstance, "Move")
				.GetComponent<ButtonManagerBasic>();
			move.clickEvent.AddListener(() =>
			{

			});

            ButtonManagerBasic rotate = GameFinder.FindChild(menuInstance, "Rotate")
				.GetComponent<ButtonManagerBasic>();
            rotate.clickEvent.AddListener(() =>
            {

            });

            ButtonManagerBasic scale = GameFinder.FindChild(menuInstance, "Scale")
				.GetComponent<ButtonManagerBasic>();
            scale.clickEvent.AddListener(() =>
            {

            });

            ButtonManagerBasic delete = GameFinder.FindChild(menuInstance, "Delete")
				.GetComponent<ButtonManagerBasic>();
            delete.clickEvent.AddListener(() =>
            {

            });
        }

		public void Destroy()
		{
			if (menuInstance != null)
			{
				Destroyer.Destroy(menuInstance);
			}
		}
	}
}