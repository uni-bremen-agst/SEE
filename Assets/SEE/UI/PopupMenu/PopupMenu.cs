using Cysharp.Threading.Tasks;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private RectTransform actionList;

        /// <summary>
        /// The scroll view containing the popup items.
        /// </summary>
        private RectTransform scrollView;

        /// <summary>
        /// The scale factor of the canvas.
        /// Should only be accessed through the <see cref="ScaleFactor"/> property.
        /// </summary>
        private float? scaleFactor;

        /// <summary>
        /// The scale factor of the canvas.
        /// </summary>
        private float ScaleFactor => scaleFactor ??= Canvas.MustGetComponent<Canvas>().scaleFactor;

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
        private float MenuHeight => GetHeight();

        /// <summary>
        /// The width of the menu.
        /// </summary>
        private float MenuWidth => menu.sizeDelta.x;

        /// <summary>
        /// Duration of the animation that is used to show or hide the menu.
        /// </summary>
        private const float animationDuration = 0.5f;

        /// <summary>
        /// The popup entries.
        /// </summary>
        private readonly List<GameObject> entries = new();

        /// <summary>
        /// Gets the height of the menu.
        /// </summary>
        /// <returns>The current height.</returns>
        private float GetHeight()
        {
            return entries.Sum(x => ((RectTransform) x.transform).rect.height);
        }

        protected override void StartDesktop()
        {
            // Instantiate the menu.
            if (SceneSettings.InputType == PlayerInputType.VRPlayer)
            {
                menu = (RectTransform)GameObject.Find("XRRig(Clone)/Camera Offset/Right Controller/Popup/XRCanvas/PopupMenu").transform;
            }
            else
            {
                menu = (RectTransform)PrefabInstantiator.InstantiatePrefab(menuPrefabPath, Canvas.transform, false).transform;
            }
            contentSizeFitter = menu.gameObject.MustGetComponent<ContentSizeFitter>();
            menuCanvasGroup = menu.gameObject.MustGetComponent<CanvasGroup>();
            scrollView = (RectTransform)menu.Find("Scroll View");
            actionList = (RectTransform)scrollView.Find("Viewport/Action List");
            if (SceneSettings.InputType == PlayerInputType.VRPlayer)
            {
                RectTransform background = (RectTransform)menu.Find("Background");
                RectTransform shadow = (RectTransform)menu.Find("Shadow");
                shadow.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                background.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                scrollView.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }
            // The menu should be hidden when the user moves the mouse away from it.
            PointerHelper pointerHelper = menu.gameObject.MustGetComponent<PointerHelper>();
            if (SceneSettings.InputType == PlayerInputType.DesktopPlayer)
            {
                pointerHelper.ExitEvent.AddListener(x =>
                {
                    // If the mouse is not moving, this may indicate that the trigger has just been
                    // menu entries being rebuilt instead of the mouse moving outside of the menu.
                    if (x.IsPointerMoving())
                    {
                        HideMenuAsync().Forget();
                    }
                });
            }

            // We add all entries that were added before the menu was started.
            while (entriesBeforeStart.Count > 0)
            {
                AddEntry(entriesBeforeStart.Dequeue());
            }

            // We hide the menu by default.
            menu.gameObject.SetActive(false);
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        protected override void UpdateVR()
        {
            if (XRSEEActions.TooltipToggle && XRSEEActions.OnSelectToggle)
            {
                XRSEEActions.TooltipToggle = false;
                XRSEEActions.OnSelectToggle = false;
                HideMenuAsync().Forget();
            }
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
                case PopupMenuActionDoubleIcon doubleIconAction:
                    AddDoubleIconAction(doubleIconAction);
                    break;
                case PopupMenuAction action:
                    AddAction(action);
                    break;
                case PopupMenuHeading heading:
                    AddHeading(heading);
                    break;
                default:
                    throw new System.ArgumentException($"Unknown entry type: {entry.GetType()}");
            }
        }

        /// <summary>
        /// Adds a new <paramref name="action"/> to the menu.
        /// </summary>
        /// <param name="action">The action to be added.</param>
        /// <param name="actionItem">The item to be added to the PopupMenu.
        /// If the default item is to be added, null should be used.
        /// Another item can be, for example, the SubMenuButton.
        /// </param>
        private void AddAction(PopupMenuAction action, GameObject actionItem = null)
        {
            actionItem ??= PrefabInstantiator.InstantiatePrefab("Prefabs/UI/PopupMenuButton", actionList, false);
            ButtonManagerBasic button = actionItem.MustGetComponent<ButtonManagerBasic>();
            button.buttonText = action.Name;

            PriorityHolder priorityHolder = actionItem.AddComponent<PriorityHolder>();
            priorityHolder.Priority = action.Priority;

            button.clickEvent.AddListener(OnClick);
            if (action.IconGlyph != default)
            {
                actionItem.transform.Find("Icon").gameObject.MustGetComponent<TextMeshProUGUI>().text = action.IconGlyph.ToString();
            }
            entries.Add(actionItem);
            return;

            void OnClick()
            {
                action.Action();
                if (action.CloseAfterClick)
                {
                    XRSEEActions.OnSelectToggle = false;
                    HideMenuAsync().Forget();
                }
            }
        }

        /// <summary>
        /// Adds a new <paramref name="doubleIconAction"/> to the menu.
        /// It can be used for popup sub-menus.
        /// </summary>
        /// <param name="doubleIconAction">The sub-menu to be added.</param>
        private void AddDoubleIconAction(PopupMenuActionDoubleIcon doubleIconAction)
        {
            GameObject actionItem = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/PopupMenuSubMenuButton", actionList, false);
            if (doubleIconAction.RightIconGlyph != default)
            {
                actionItem.transform.Find("RightIcon").gameObject.MustGetComponent<TextMeshProUGUI>()
                    .text = doubleIconAction.RightIconGlyph.ToString();
            }
            AddAction(doubleIconAction, actionItem);
        }

        /// <summary>
        /// Adds a new <paramref name="heading"/> to the menu.
        /// </summary>
        /// <param name="heading">The heading to be added.</param>
        private void AddHeading(PopupMenuHeading heading)
        {
            GameObject headingItem = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/PopupMenuHeading", actionList, false);
            TextMeshProUGUI text = headingItem.MustGetComponent<TextMeshProUGUI>();
            text.text = heading.Text;
            PriorityHolder priorityHolder = headingItem.AddComponent<PriorityHolder>();
            priorityHolder.Priority = heading.Priority;
            entries.Add(headingItem);
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
            foreach (GameObject entry in entries)
            {
                Destroyer.Destroy(entry);
            }
            entries.Clear();
        }

        /// <summary>
        /// Moves this menu to the given <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position to which the menu should be moved.</param>
        public void MoveTo(Vector2 position)
        {
            AdjustMenuHeight();
            bool moved = false;
            if (position.y < MenuHeight * ScaleFactor)
            {
                // If the menu is too close to the bottom of the screen, expand it upwards instead.
                position.y = MenuHeight * ScaleFactor;
                moved = true;
            }
            if (Screen.width < position.x + MenuWidth * ScaleFactor)
            {
                position.x = Screen.width - MenuWidth * ScaleFactor;
                moved = true;
            }
            if (!moved)
            {
                // The mouse should hover over the first menu item already rather than being just outside of it,
                // so we move the menu up and to the left a bit.
                position += new Vector2(-5, 5);
            }
            menu.position = position;
        }

        /// <summary>
        /// Adjusts the height of the menu (specifically, the scroll view) to fit the content
        /// while ensuring that the menu does not take up more than 40% of the screen.
        /// </summary>
        private void AdjustMenuHeight()
        {
            // Menu should not take up more than 40% of the screen.
            float height = Mathf.Clamp(MenuHeight, 100f, Screen.height / (2.5f * ScaleFactor));
            scrollView.sizeDelta = new Vector2(scrollView.sizeDelta.x, height);
        }

        /// <summary>
        /// Sorts the entries by their priority.
        /// Keep in mind: A higher priority is displayed first.
        /// </summary>
        private void SortEntries()
        {
            entries.Sort((a, b) =>
            {
                PriorityHolder aHolder = a.GetComponent<PriorityHolder>();
                PriorityHolder bHolder = b.GetComponent<PriorityHolder>();
                if (aHolder == null || bHolder == null)
                {
                    return 0;
                }
                return bHolder.Priority.CompareTo(aHolder.Priority);
            });
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].transform.SetSiblingIndex(i);
            }
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
            AdjustMenuHeight();
            SortEntries();
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
                if (SceneSettings.InputType == PlayerInputType.DesktopPlayer)
                {
                    MoveTo(position.Value);
                }
            }
            ShowMenuAsync().Forget();
        }
    }
}
