using Michsky.UI.ModernUIPack;
using SEE.Game.Table;
using SEE.GO;
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
        private bool isFinished;

        /// <summary>
        /// Whether this class has a canceled progress in store that hasn't been fetched yet.
        /// </summary>
        private bool wasCanceled;

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
            InputFieldWithButtons xScaleArea = menuInstance.FindDescendant("XScale").GetComponent<InputFieldWithButtons>();
            InputFieldWithButtons zScaleArea = menuInstance.FindDescendant("ZScale").GetComponent<InputFieldWithButtons>();
            float step = 0.001f;
            float max = 2f;

            xScaleArea.SetMinValue(step);
            xScaleArea.SetMaxValue(max);
            xScaleArea.SetUpAndDownValue(step);
            xScaleArea.AssignValue(table.transform.localScale.x);
            xScaleArea.OnValueChanged.AddListener(xScale =>
            {
                Vector3 newScale = new(xScale, table.transform.localScale.y, zScaleArea.GetValue());
                TryScale(newScale, true, table.transform.localScale.x > xScale);
            });

            zScaleArea.SetMinValue(step);
            zScaleArea.SetMaxValue(max);
            zScaleArea.SetUpAndDownValue(step);
            zScaleArea.AssignValue(table.transform.localScale.z);
            zScaleArea.OnValueChanged.AddListener(zScale =>
            {
                Vector3 newScale = new(xScaleArea.GetValue(), table.transform.localScale.y, zScale);
                TryScale(newScale, false, table.transform.localScale.z > zScale);
            });

            void TryScale(Vector3 newScale, bool scalesX, bool scaleDown)
            {
                if (!scaleDown || GameTableManager.CanScaleDown(table, newScale))
                {
                    GameTableManager.Scale(table, newScale);
                    new ScaleTableNetAction(table.name, newScale).Execute();
                }
                else
                {
                    Vector3 originalScale = table.transform.localScale;
                    (scalesX ? xScaleArea : zScaleArea).AssignValue(scalesX ? originalScale.x : originalScale.z);
                }
            }
        }

        /// <summary>
        /// Instantiates the dialog action buttons for the menu.
        /// </summary>
        private void DialogActionButtons()
        {
            ButtonManagerBasic finish = menuInstance.FindDescendant("Finish").GetComponent<ButtonManagerBasic>();
            finish.clickEvent.AddListener(() =>
            {
                isFinished = true;
            });
            ButtonManagerBasic cancel = menuInstance.FindDescendant("Cancel").GetComponent<ButtonManagerBasic>();
            cancel.clickEvent.AddListener(() =>
            {
                wasCanceled = true;
            });
        }

        /// <summary>
        /// Checks whether the progress is finished.
        /// </summary>
        /// <returns><see cref="isFinished"/></returns>
        public bool TryGetFinish()
        {
            if (isFinished)
            {
                isFinished = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether the progress was canceled.
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
