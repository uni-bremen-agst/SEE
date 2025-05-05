using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Table;
using SEE.Net.Actions.Table;
using SEE.UI.Drawable;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu.Table
{
    /// <summary>
    /// Provides a menu to scale a table.
    /// </summary>
    public class ScaleTableMenu
    {
        /// <summary>
        /// The prefab for the scale menu.
        /// </summary>
        private const string scaleMenuPrefab = "Prefabs/UI/Table/ScaleMenu";

        /// <summary>
        /// The instance of the menu.
        /// </summary>
        private readonly GameObject menuInstance;

        /// <summary>
        /// Whether this class has a finished scale progress in store that hasn't been fetched yet.
        /// </summary>
        private bool isFinish;

        /// <summary>
        /// Whether this class has a canceled progress in store that hasn't been fetched yet.
        /// </summary>
        private bool wasCanceled;

        /// <summary>
        /// The switch for scaling proportionally or unproportionally.
        /// </summary>
        private SwitchManager switchManager;

        /// <summary>
        /// Instantiates the menu and displays it.
        /// </summary>
        /// <param name="table">The table to be scaled.</param>
        public ScaleTableMenu(GameObject table)
        {
            menuInstance = PrefabInstantiator.InstantiatePrefab(scaleMenuPrefab,
                                UICanvas.Canvas.transform, false);
            InitScale(table);
            DialogActionButtons();
        }

        /// <summary>
        /// Instantiates the x- and z-Scale-Buttons.
        /// </summary>
        /// <param name="table">The table to scale.</param>
        private void InitScale(GameObject table)
        {
            InputFieldWithButtons xScale = GameFinder.FindChild(menuInstance, "XScale").GetComponent<InputFieldWithButtons>();
            InputFieldWithButtons zScale = GameFinder.FindChild(menuInstance, "ZScale").GetComponent<InputFieldWithButtons>();

            xScale.AssignValue(table.transform.localScale.x);
            xScale.OnValueChanged.AddListener(xScale =>
            {
                Vector3 newScale = new(xScale, table.transform.localScale.y, zScale.GetValue());
                GameTableManager.Scale(table, newScale);
                new ScaleTableNetAction(table.name, newScale).Execute();
            });

            zScale.AssignValue(table.transform.localScale.z);
            zScale.OnValueChanged.AddListener(zScale =>
            {
                Vector3 newScale = new(xScale.GetValue(), table.transform.localScale.y, zScale);
                GameTableManager.Scale(table, newScale);
                new ScaleTableNetAction(table.name, newScale).Execute();
            });
        }

        /// <summary>
        /// Instantiates the dialog action buttons for the menu.
        /// </summary>
        private void DialogActionButtons()
        {
            ButtonManagerBasic finish = GameFinder.FindChild(menuInstance, "Finish").GetComponent<ButtonManagerBasic>();
            finish.clickEvent.AddListener(() =>
            {
                isFinish = true;
            });
            ButtonManagerBasic cancel = GameFinder.FindChild(menuInstance, "Cancel").GetComponent<ButtonManagerBasic>();
            cancel.clickEvent.AddListener(() =>
            {
                wasCanceled = true;
            });
        }

        /// <summary>
        /// Checks if the progress is finished.
        /// </summary>
        /// <returns><see cref="isFinish"/></returns>
        public bool TryGetFinish()
        {
            if (isFinish)
            {
                isFinish = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the progress was canceled.
        /// Otherwise it will be false.
        /// </summary>
        /// <returns><see cref="wasCanceled"/></returns>
        public bool TryGetCanceled()
        {
            if (wasCanceled)
            {
                wasCanceled = false;
                Destroy();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Destroys the menu.
        /// </summary>
        public void Destroy()
        {
            if (menuInstance != null)
            {
                Destroyer.Destroy(menuInstance);
            }
        }
    }
}
