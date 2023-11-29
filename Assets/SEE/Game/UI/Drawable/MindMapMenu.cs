using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Utils;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class provides the mind map menu.
    /// </summary>
    public static class MindMapMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string mindMapMenuPrefab = "Prefabs/UI/Drawable/MindMapMenu";

        /// <summary>
        /// The instance for the mind map menu.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// Whether this class has an operation in store that wasn't yet fetched.
        /// </summary>
        private static bool gotOperation;

        /// <summary>
        /// If <see cref="gotOperation"/> is true, this contains the button kind which the player selected.
        /// </summary>
        private static Operation chosenOperation;

        /// <summary>
        /// Contains keywords for the different buttons of the mind map menu.
        /// </summary>
        public enum Operation
        {
            None,
            Theme,
            Subtheme,
            Leaf
        }

        /// <summary>
        /// Creates and adds the necessary handler to the buttons.
        /// </summary>
        public static void Enable()
        {
            instance = PrefabInstantiator.InstantiatePrefab(mindMapMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);

            ButtonManagerBasic theme = GameFinder.FindChild(instance, "Theme").GetComponent<ButtonManagerBasic>();
            theme.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Theme;
            });

            ButtonManagerBasic subtheme = GameFinder.FindChild(instance, "Subtheme").GetComponent<ButtonManagerBasic>();
            subtheme.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Subtheme;
            });

            ButtonManagerBasic leaf = GameFinder.FindChild(instance, "Leaf").GetComponent<ButtonManagerBasic>();
            leaf.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Leaf;
            });
        }

        /// <summary>
        /// Destroy's the menu.
        /// </summary>
        public static void Disable()
        {
            if (instance != null)
            {
                Destroyer.Destroy(instance);
            }
        }

        /// <summary>
        /// If <see cref="gotOperation"/> is true, the <paramref name="operation"/> will be the chosen operation by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="operation">The chosen operation the player confirmed, if that doesn't exist, some dummy value</param>
        /// <returns><see cref="gotOperation"/></returns>
        public static bool TryGetOperation(out Operation operation)
        {
            if (gotOperation)
            {
                operation = chosenOperation;
                gotOperation = false;
                Disable();
                return true;
            }

            operation = Operation.None;
            return false;
        }
    }
}