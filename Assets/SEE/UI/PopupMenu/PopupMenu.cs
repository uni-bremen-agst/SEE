using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private const string menuPrefabPath = "Prefabs/UI/PopupMenu";

        /// <summary>
        /// The root transform of the popup menu.
        /// </summary>
        private RectTransform menu;

        /// <summary>
        /// The canvas group of the popup menu.
        /// </summary>
        private CanvasGroup menuCanvasGroup;

        /// <summary>
        /// The transform under which the entries are listed.
        /// </summary>
        private RectTransform entryList;

        /// <summary>
        /// The content size fitter of the popup menu.
        /// </summary>
        private ContentSizeFitter contentSizeFitter;

        /// <summary>
        /// A queue of entries that were added before the menu was started.
        /// These entries will be added to the menu once it is started.
        /// </summary>
        private readonly Queue<PopupMenuEntry> entriesBeforeStart = new();

        /// <summary>
        /// Whether the menu should currently be shown.
        /// </summary>
        private bool shouldShowMenu;

        /// <summary>
        /// The height of the menu.
        /// </summary>
        private float MenuHeight => menu.sizeDelta.y;

        /// <summary>
        /// Duration of the animation that is used to show or hide the menu.
        /// </summary>
        private const float animationDuration = 0.5f;

        protected override void StartDesktop()
        {
            // Instantiate the menu.
            menu = (RectTransform)PrefabInstantiator.InstantiatePrefab(menuPrefabPath, Canvas.transform, false).transform;
            contentSizeFitter = menu.gameObject.MustGetComponent<ContentSizeFitter>();
            menuCanvasGroup = menu.gameObject.MustGetComponent<CanvasGroup>();
            entryList = (RectTransform)menu.Find("Action List");

            // The menu should be hidden when the user moves the mouse away from it.
            PointerHelper pointerHelper = menu.gameObject.MustGetComponent<PointerHelper>();
            pointerHelper.ExitEvent.AddListener(x =>
            {
                // If the mouse is not moving, this may indicate that the trigger has just been
                // menu entries being rebuilt instead of the mouse moving outside of the menu.
                if (x.IsPointerMoving())
                {
                    HideMenuAsync().Forget();
                }
            });

            // We add all entries that were added before the menu was started.
            while (entriesBeforeStart.Count > 0)
            {
                AddEntry(entriesBeforeStart.Dequeue());
            }

            // We hide the menu by default.
            menu.gameObject.SetActive(false);

            // TODO (#679): Make this scrollable once it gets too big.
        }

        /// <summary>
        /// Adds a new <paramref name="entry"/> to the menu.
        /// </summary>
        /// <param name="entry">The entry to be added.</param>
        public void AddEntry(PopupMenuEntry entry)
        {
            if (menu is null)
            {
                entriesBeforeStart.Enqueue(entry);
                return;
            }

            switch (entry)
            {
                case PopupMenuAction action:
                    AddAction(action);
                    break;
                case PopupMenuHeading heading:
                    AddHeading(heading);
                    break;
                default:
                    throw new System.ArgumentException($"Unknown entry type: {entry.GetType()}");
            }

            // TODO (#668): Respect priority
        }

        /// <summary>
        /// Adds a new <paramref name="action"/> to the menu.
        /// </summary>
        /// <param name="action">The action to be added.</param>
        private void AddAction(PopupMenuAction action)
        {
            GameObject actionItem = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/PopupMenuButton", entryList, false);
            ButtonManagerBasic button = actionItem.MustGetComponent<ButtonManagerBasic>();
            button.buttonText = action.Name;

            button.clickEvent.AddListener(OnClick);
            if (action.IconGlyph != default)
            {
                actionItem.transform.Find("Icon").gameObject.MustGetComponent<TextMeshProUGUI>().text = action.IconGlyph.ToString();
            }
            return;

            void OnClick()
            {
                action.Action();
                if (action.CloseAfterClick)
                {
                    HideMenuAsync().Forget();
                }
            }
        }

        /// <summary>
        /// Adds a new <paramref name="heading"/> to the menu.
        /// </summary>
        /// <param name="heading">The heading to be added.</param>
        private void AddHeading(PopupMenuHeading heading)
        {
            GameObject headingItem = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/PopupMenuHeading", entryList, false);
            TextMeshProUGUI text = headingItem.MustGetComponent<TextMeshProUGUI>();
            text.text = heading.Text;
        }

        /// <summary>
        /// Adds all given <paramref name="entries"/> to the menu.
        /// </summary>
        /// <param name="entries">The entries to be added.</param>
        public void AddEntries(IEnumerable<PopupMenuEntry> entries)
        {
            foreach (PopupMenuEntry entry in entries)
            {
                AddEntry(entry);
            }
        }

        /// <summary>
        /// Removes all entries from the menu.
        /// </summary>
        public void ClearEntries()
        {
            if (menu is null)
            {
                entriesBeforeStart.Clear();
                return;
            }

            foreach (Transform child in entryList)
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
            float scaleFactor = Canvas.MustGetComponent<Canvas>().scaleFactor;
            if (position.y < MenuHeight * scaleFactor)
            {
                // If the menu is too close to the bottom of the screen, expand it upwards instead.
                position.y += MenuHeight * scaleFactor;
                // The mouse should hover over the first menu item already rather than being just outside of it,
                // so we move the menu down and to the left a bit.
                position += new Vector2(-5, -5);
            }
            else
            {
                // The mouse should hover over the first menu item already rather than being just outside of it,
                // so we move the menu up and to the left a bit.
                position += new Vector2(-5, 5);
            }
            menu.position = position;
        }

        /// <summary>
        /// Activates the menu and fades it in.
        /// This asynchronous method will return once the menu is fully shown.
        /// </summary>
        public async UniTask ShowMenuAsync()
        {
            shouldShowMenu = true;
            menu.gameObject.SetActive(true);
            menu.localScale = Vector3.zero;
            // This may seem stupid, but unfortunately, due to a Unity bug,
            // this appears to be the only way to make the content size fitter update.
            // See https://forum.unity.com/threads/content-size-fitter-refresh-problem.498536/
            contentSizeFitter.enabled = false;
            await UniTask.WaitForEndOfFrame();
            contentSizeFitter.enabled = true;
            await UniTask.WhenAll(menu.DOScale(1, animationDuration).AsyncWaitForCompletion().AsUniTask(),
                                  menuCanvasGroup.DOFade(1, animationDuration / 2).AsyncWaitForCompletion().AsUniTask());
        }

        /// <summary>
        /// Hides the menu and fades it out.
        /// This asynchronous method will return once the menu is fully hidden and deactivated.
        /// </summary>
        public async UniTask HideMenuAsync()
        {
            shouldShowMenu = false;
            // We use a fade effect rather than DOScale because it looks better.
            await menuCanvasGroup.DOFade(0, animationDuration).AsyncWaitForCompletion();
            if (!shouldShowMenu)
            {
                menu.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Convenience method that shows the menu with the given <paramref name="entries"/>
        /// at the given <paramref name="position"/>.
        /// </summary>
        /// <param name="entries">The entries to be shown in the menu.
        /// If null, entries will not be modified.</param>
        /// <param name="position">The position at which the menu should be shown.
        /// If null, menu will not be moved.</param>
        public void ShowWith(IEnumerable<PopupMenuEntry> entries = null, Vector2? position = null)
        {
            if (entries != null)
            {
                ClearEntries();
                AddEntries(entries);
            }
            if (position.HasValue)
            {
                MoveTo(position.Value);
            }
            ShowMenuAsync().Forget();
        }
    }
}
