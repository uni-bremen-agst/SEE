using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.UI.PopupMenu
{
    /// <summary>
    /// A popup menu that can be used to display a list of actions to the user.
    /// </summary>
    public class PopupMenu : PlatformDependentComponent
    {
        /// <summary>
        /// Path to the prefab that should be used as the popup menu.
        /// </summary>
        private const string MenuPrefabPath = "Prefabs/UI/PopupMenu";

        /// <summary>
        /// The root transform of the popup menu.
        /// </summary>
        private RectTransform Menu;

        /// <summary>
        /// The canvas group of the popup menu.
        /// </summary>
        private CanvasGroup MenuCanvasGroup;

        /// <summary>
        /// The transform under which the actions are listed.
        /// </summary>
        private RectTransform ActionList;

        /// <summary>
        /// A queue of actions that were added before the menu was started.
        /// These actions will be added to the menu once it is started.
        /// </summary>
        private readonly Queue<PopupMenuAction> ActionsBeforeStart = new();

        /// <summary>
        /// Whether the menu should currently be shown.
        /// </summary>
        private bool ShouldShowMenu;

        /// <summary>
        /// Duration of the animation that is used to show or hide the menu.
        /// </summary>
        private const float AnimationDuration = 0.5f;

        protected override void StartDesktop()
        {
            // Instantiate the menu.
            Menu = (RectTransform)PrefabInstantiator.InstantiatePrefab(MenuPrefabPath, Canvas.transform, false).transform;
            MenuCanvasGroup = Menu.gameObject.MustGetComponent<CanvasGroup>();
            ActionList = (RectTransform)Menu.Find("Action List");

            // The menu should be hidden when the user moves the mouse away from it.
            PointerHelper pointerHelper = Menu.gameObject.MustGetComponent<PointerHelper>();
            pointerHelper.ExitEvent.AddListener(_ => HideMenu().Forget());

            // We hide the menu by default.
            Menu.gameObject.SetActive(false);

            // We add all actions that were added before the menu was started.
            while (ActionsBeforeStart.Count > 0)
            {
                AddAction(ActionsBeforeStart.Dequeue());
            }
        }

        /// <summary>
        /// Adds a new <paramref name="action"/> to the menu.
        /// </summary>
        /// <param name="action">The action to be added.</param>
        public void AddAction(PopupMenuAction action)
        {
            if (Menu is null)
            {
                ActionsBeforeStart.Enqueue(action);
                return;
            }

            // TODO: Respect priority
            GameObject actionItem = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/PopupMenuButton", ActionList, false);
            ButtonManagerBasic button = actionItem.MustGetComponent<ButtonManagerBasic>();
            button.buttonText = action.Name;
            button.clickEvent.AddListener(() =>
            {
                action.Action();
                HideMenu().Forget();
            });
            if (action.IconGlyph != default)
            {
                actionItem.transform.Find("Icon").gameObject.MustGetComponent<TextMeshProUGUI>().text = action.IconGlyph.ToString();
            }
        }

        /// <summary>
        /// Adds all given <paramref name="actions"/> to the menu.
        /// </summary>
        /// <param name="actions">The actions to be added.</param>
        public void AddActions(IEnumerable<PopupMenuAction> actions)
        {
            foreach (PopupMenuAction action in actions)
            {
                AddAction(action);
            }
        }

        /// <summary>
        /// Removes all actions from the menu.
        /// </summary>
        public void ClearActions()
        {
            if (Menu is null)
            {
                ActionsBeforeStart.Clear();
                return;
            }

            foreach (Transform child in ActionList)
            {
                Destroyer.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Moves this menu to the given <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position to which the menu should be moved.</param>
        public void MoveTo(Vector2 position)
        {
            // TODO: If the menu is too close to the bottom, expand it upwards instead.
            Menu.position = position;
        }

        /// <summary>
        /// Activates the menu and fades it in.
        /// This asynchronous method will return once the menu is fully shown.
        /// </summary>
        public async UniTaskVoid ShowMenu()
        {
            ShouldShowMenu = true;
            Menu.gameObject.SetActive(true);
            Menu.localScale = Vector3.zero;
            await UniTask.WhenAll(Menu.DOScale(1, AnimationDuration).AsyncWaitForCompletion().AsUniTask(),
                                  MenuCanvasGroup.DOFade(1, AnimationDuration / 2).AsyncWaitForCompletion().AsUniTask());
        }

        /// <summary>
        /// Hides the menu and fades it out.
        /// This asynchronous method will return once the menu is fully hidden and deactivated.
        /// </summary>
        public async UniTaskVoid HideMenu()
        {
            ShouldShowMenu = false;
            // We use a fade effect rather than DOScale because it looks better.
            await MenuCanvasGroup.DOFade(0, AnimationDuration).AsyncWaitForCompletion();
            if (!ShouldShowMenu)
            {
                Menu.gameObject.SetActive(false);
            }
        }
    }
}
