using DG.Tweening;
using HighlightPlus;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.PropertyDialog.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace SEE.UI.Window.DrawableManagerWindow
{
    /// <summary>
    /// Parts of the drawable manager window that are specific to the desktop UI.
    /// </summary>
    public partial class DrawableManagerWindow
    {
        /// <summary>
        /// Component that allows scrolling through the items of the tree window.
        /// </summary>
        private ScrollRect scrollRect;

        /// <summary>
        /// A set of all items that have been expanded.
        /// Note that this may contain items that are not currently visible due to collapsed parents.
        /// Such items will be expanded when they become visible again.
        /// </summary>
        private readonly ISet<string> expandedItems = new HashSet<string>();

        /// <summary>
        /// This dictionary holds the item for a surface name.
        /// </summary>
        private readonly Dictionary<string, GameObject> surfaceItems = new();

        /// <summary>
        /// The amount by which the text of an item is indented per level.
        /// </summary>
        private const int indentShift = 22;

        /// <summary>
        /// The alpha keys for the gradient of a menu item (fully opaque).
        /// </summary>
        private static readonly GradientAlphaKey[] alphaKeys = { new(1, 0), new(1, 1) };

        /// <summary>
        /// The gradient for a whiteboard.
        /// </summary>
        public readonly Color[] WhiteboardColor = new Color[] { Color.black, Color.blue };

        /// <summary>
        /// The gradient for a sticky note.
        /// </summary>
        public readonly Color[] StickyNoteColor = new Color[] { Color.black, Color.red };

        /// <summary>
        /// The input field in which the user can enter a search term.
        /// </summary>
        private TMP_InputField searchField;

        /// <summary>
        /// The button that opens the filter menu.
        /// </summary>
        private ButtonManagerBasic filterButton;

        /// <summary>
        /// The button that opens the sorting menu.
        /// </summary>
        private ButtonManagerBasic sortButton;

        /// <summary>
        /// The button that opens the grouping menu.
        /// </summary>
        private ButtonManagerBasic groupButton;

        protected override void StartDesktop()
        {
            Title = "Drawable Surface Manager";
            base.StartDesktop();
            Transform root = PrefabInstantiator.InstantiatePrefab(dmWindowPrefab, Window.transform.Find("Content"), false).transform;
            items = (RectTransform)root.Find("Content/Items");
            scrollRect = root.gameObject.MustGetComponent<ScrollRect>();

            searchField = root.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();
            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            searchField.onValueChanged.AddListener(SearchFor);

            filterButton = root.Find("Search/Filter").gameObject.MustGetComponent<ButtonManagerBasic>();
            sortButton = root.Find("Search/Sort").gameObject.MustGetComponent<ButtonManagerBasic>();
            groupButton = root.Find("Search/Group").gameObject.MustGetComponent<ButtonManagerBasic>();
            PopupMenu.PopupMenu popupMenu = gameObject.AddComponent<PopupMenu.PopupMenu>();
            UnityEvent<List<GameObject>> rebuild = new();
            rebuild.AddListener(list => { Rebuild(list); });
            contextMenu = new DrawableWindowContextMenu(popupMenu, rebuild,
                                                    filterButton, sortButton, groupButton);
            Rebuild();
        }

        /// <summary>
        /// Rebuilds the drawable manager view.
        /// </summary>
        private void Rebuild(List<GameObject> surfaces = null)
        {
            ClearManager();
            AddDrawableSurfaces(surfaces);
        }

        /// <summary>
        /// Searches for the given <paramref name="searchTerm"/> in the description of
        /// a surface or in the ID and displays the results in the view.
        /// </summary>
        /// <param name="searchTerm">The search term to be searched for.</param>
        private void SearchFor(string searchTerm)
        {
            ClearManager();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                AddDrawableSurfaces();
                return;
            }

            foreach (GameObject surface in ValueHolder.DrawableSurfaces)
            {
                DrawableConfig config = DrawableConfigManager.GetDrawableConfig(surface);

                if (config.Description.Contains(searchTerm) /// Search description
                    || config.ParentID.Contains(searchTerm) /// Search parent ID
                    || (config.ID.Contains(searchTerm) /// Search ID if the parent ID is not set.
                        && (string.IsNullOrWhiteSpace(config.ParentID)
                            || string.IsNullOrEmpty(config.ParentID))))
                {
                    AddItem(surface);
                }
            }
        }

        /// <summary>
        /// Removes an item.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        private void RemoveItem(GameObject item)
        {
            string key = surfaceItems.FirstOrDefault(x=>x.Value == item).Key;
            surfaceItems.Remove(key);
            Destroyer.Destroy(item);
        }

        /// <summary>
        /// Expands the given <paramref name="item"/>.
        /// This does not add the item's children to the tree window.
        /// </summary>
        /// <param name="item">The item to be expanded.</param>
        private void ExpandItem(GameObject item)
        {
            expandedItems.Add(item.name);
            if (item.transform.Find("Foreground/Expand Icon").gameObject.TryGetComponentOrLog(out RectTransform rectTransform))
            {
                rectTransform.DORotate(new Vector3(0, 0, -180), duration: 0.5f);
            }
        }

        /// <summary>
        /// Collapses the given <paramref name="item"/>.
        /// This does not remove the item's children from the tree window.
        /// </summary>
        /// <param name="item">The item to be collapsed.</param>
        private void CollapseItem(GameObject item)
        {
            expandedItems.Remove(item.name);
            if (item.transform.Find("Foreground/Expand Icon").gameObject.TryGetComponentOrLog(out RectTransform rectTransform))
            {
                rectTransform.DORotate(new Vector3(0, 0, -90), duration: 0.5f);
            }
        }

        /// <summary>
        /// Adds a group to the drawable manager window.
        /// </summary>
        /// <param name="name">The name for the group.</param>
        /// <param name="icon">The icon for the group.</param>
        /// <param name="children">The children that should be displayed.</param>
        private void AddGroup(string name, char icon, List<GameObject> children, Color[] gradient = null)
        {
            children.Reverse();
            GameObject item = PrefabInstantiator.InstantiatePrefab(groupItemPrefab, items, false);
            item.name = name;
            Transform background = item.transform.Find("Background");
            Transform foreground = item.transform.Find("Foreground");
            GameObject expandIcon = foreground.Find("Expand Icon").gameObject;
            TextMeshProUGUI textMesh = foreground.Find("Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            TextMeshProUGUI iconMesh = foreground.Find("Type Icon").gameObject.MustGetComponent<TextMeshProUGUI>();

            textMesh.text = name + " [" + children.Count + "]";
            iconMesh.text = icon.ToString();

            List<GameObject> childrenItems = new();
            gradient ??= new[] { Color.gray, Color.gray.Darker() };
            if (expandedItems.Contains(name))
            {
                ExpandItem(item);
                AddChildren(item, children, ref childrenItems, gradient);
            }

            ColorItem();
            RegisterClickHandler();

            return;

            ///Colors the item according to its group.
            void ColorItem()
            {
                background.GetComponent<UIGradient>().EffectGradient.SetKeys(gradient.ToGradientColorKeys().ToArray(), alphaKeys);

                /// We also need to set the text color to a color that is readable on the background color.
                Color foregroundColor = gradient.Aggregate((x, y) => (x + y) / 2).IdealTextColor();
                textMesh.color = foregroundColor;
                iconMesh.color = foregroundColor;
                expandIcon.GetComponent<Graphic>().color = foregroundColor;
            }

            /// Registers a click handler for the item.
            void RegisterClickHandler()
            {
                if (item.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    /// expands/collapses the item.
                    pointerHelper.ClickEvent.AddListener(e =>
                    {

                        if (expandedItems.Contains(name))
                        {
                            CollapseItem(item);
                            foreach (GameObject child in childrenItems)
                            {
                                if (child != null)
                                {
                                    RemoveItem(child);
                                }
                            }
                        }
                        else
                        {
                            ExpandItem(item);
                            AddChildren(item, children, ref childrenItems, gradient);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Adds the children items to the group and indent it.
        /// </summary>
        /// <param name="group">The group item.</param>
        /// <param name="children">The list of the surfaces for which to create an children item.</param>
        /// <param name="childrenItems">The created children items.</param>
        /// <param name="gradient">The gradient of the group.</param>
        private void AddChildren(GameObject group, List<GameObject> children, ref List<GameObject> childrenItems,
            Color[] gradient)
        {
            foreach (GameObject child in children)
            {
                GameObject childItem = AddItem(child, gradient);

                childItem.name += "#" + group.name;
                RectTransform rtBack = ((RectTransform)childItem.transform.Find("Background"));
                rtBack.offsetMin = new Vector2(indentShift, rtBack.offsetMin.y);

                RectTransform rtFore = ((RectTransform)childItem.transform.Find("Foreground"));
                rtFore.offsetMin = new Vector2(indentShift, rtFore.offsetMin.y);

                childItem.transform.SetSiblingIndex(group.transform.GetSiblingIndex() + 1);

                childrenItems.Add(childItem);
            }
        }

        /// <summary>
        /// Adds a given drawable surface to the drawable manager view.
        /// </summary>
        /// <param name="surface">The drawable surface to be added.</param>
        /// <param name="gradient">The gradient of the group.</param>
        private GameObject AddItem(GameObject surface, Color[] gradient = null)
        {
            DrawableConfig config = DrawableConfigManager.GetDrawableConfig(surface);
            GameObject item;
            if (string.IsNullOrEmpty(config.Description) || string.IsNullOrWhiteSpace(config.Description))
            {
                item = PrefabInstantiator.InstantiatePrefab(dmwdItemPrefab, items, false);
            }
            else
            {
                item = PrefabInstantiator.InstantiatePrefab(dmItemPrefab, items, false);
            }
            item.name = !string.IsNullOrWhiteSpace(config.ParentID) ? config.ParentID : config.ID;
            Transform background = item.transform.Find("Background");
            Transform foreground = item.transform.Find("Foreground");

            GameObject expandIcon = foreground.Find("Expand Icon").gameObject;
            expandIcon.SetActive(false);

            TextMeshProUGUI textMesh = foreground.Find("Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.text = !string.IsNullOrWhiteSpace(config.ParentID) ? config.ParentID : config.ID;

            TextMeshProUGUI iconMesh = foreground.Find("Type Icon").gameObject.MustGetComponent<TextMeshProUGUI>();
            iconMesh.text = GameFinder.IsWhiteboard(surface) ? Icons.Whiteboard.ToString() : Icons.StickyNote.ToString();

            ButtonManagerBasic descriptionBtn = foreground.Find("DescriptionBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            TextMeshProUGUI descriptionBtnMesh = foreground.Find("DescriptionBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            TextMeshProUGUI descriptionMesh = foreground.Find("Description").gameObject.MustGetComponent<TextMeshProUGUI>();
            descriptionMesh.text = config.Description;
            descriptionBtn.clickEvent.AddListener(() =>
            {
                WriteEditTextDialog writeTextDialog = new();
                writeTextDialog.SetStringInit(descriptionMesh.text);
                UnityAction<string> stringAction = textOut =>
                {
                    GameDrawableManager.ChangeDescription(surface, textOut);
                    new DrawableChangeDescriptionNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                    Rebuild(contextMenu.Filter.GetFilteredSurfaces());
                };

                writeTextDialog.Open(stringAction);
            });

            ButtonManagerBasic colorBtn = foreground.Find("ColorBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            Image colorImage = foreground.Find("ColorBtn").gameObject.GetComponent<Image>();
            TextMeshProUGUI colorMesh = foreground.Find("ColorBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            colorMesh.color = config.Color;
            colorBtn.clickEvent.AddListener(() =>
            {
                if (!SurfaceColorMenu.Instance.IsOpen())
                {
                    UnityAction<Color> colorAction = colorOut =>
                    {
                        colorMesh.color = colorOut;
                    };
                    SurfaceColorMenu.Instance.Enable(surface, colorAction);
                }
            });

            ButtonManagerBasic pageBtn = foreground.Find("PageBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            TextMeshProUGUI pageBtnMesh = foreground.Find("PageBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            pageBtn.buttonText = config.CurrentPage.ToString();
            pageBtnMesh.text = config.CurrentPage.ToString();
            GUIClickController pageClickController = foreground.Find("PageBtn").gameObject.MustGetComponent<GUIClickController>();
            pageClickController.OnLeft.AddListener(() => contextMenu.ShowSelectionAddPageMenu(surface, pageBtn.transform.position));
            pageClickController.OnRight.AddListener(() => contextMenu.ShowRemovePageMenu(surface, pageBtn.transform.position));

            ButtonManagerBasic lightingBtn = foreground.Find("LightingBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            Image lightingImage = foreground.Find("LightingBtn").gameObject.GetComponent<Image>();
            TextMeshProUGUI lightMesh = foreground.Find("LightingBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            lightMesh.color = GetLightColor(config.Lighting);
            lightingBtn.clickEvent.AddListener(() =>
            {
                bool light = !GameFinder.GetDrawableSurfaceParent(surface).GetComponentInChildren<Light>().enabled;
                GameDrawableManager.ChangeLighting(surface, light);
                new DrawableChangeLightingNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                lightMesh.color = GetLightColor(light);

                if (light && !contextMenu.Filter.IncludeHaveLighting
                || !light && !contextMenu.Filter.IncludeHaveNoLighting)
                {
                    RemoveItem(item);
                }
            });

            ButtonManagerBasic visibilityBtn = foreground.Find("VisibilityBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            Image visibilityImage = foreground.Find("VisibilityBtn").gameObject.GetComponent<Image>();
            TextMeshProUGUI visibilityMesh = foreground.Find("VisibilityBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            visibilityBtn.buttonText = GetVisibilityText(config.Visibility);
            visibilityMesh.color = GetVisibilityColor(config.Visibility);
            visibilityBtn.clickEvent.AddListener(() =>
            {
                bool visibility = !surface.GetRootParent().activeInHierarchy;
                GameDrawableManager.ChangeVisibility(surface, visibility);
                new DrawableChangeVisibilityNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                visibilityMesh.text = GetVisibilityText(visibility);
                visibilityMesh.color = GetVisibilityColor(visibility);

                if (visibility && !contextMenu.Filter.IncludeIsVisible
                || !visibility && !contextMenu.Filter.IncludeIsInvisibile)
                {
                    RemoveItem(item);
                }
            });

            ColorItem();
            RegisterClickHandler();
            AnimateIn();

            surfaceItems.Add(GameFinder.GetUniqueID(surface), item);
            return item;

            /// Changes the item color.
            void ColorItem()
            {
                if (gradient == null)
                {
                    gradient = GameFinder.IsWhiteboard(surface) ? WhiteboardColor : StickyNoteColor;
                }
                background.GetComponent<UIGradient>().EffectGradient.SetKeys(gradient.ToGradientColorKeys().ToArray(), alphaKeys);

                /// We also need to set the text color to a color that is readable on the background color.
                Color foregroundColor = gradient.Aggregate((x, y) => (x + y) / 2).IdealTextColor();
                textMesh.color = foregroundColor;
                iconMesh.color = foregroundColor;
                descriptionMesh.color = foregroundColor;
                descriptionBtnMesh.color = foregroundColor;
                colorImage.color = foregroundColor;
                lightingImage.color = foregroundColor;
                visibilityImage.color = foregroundColor;
            }

            /// Expands the item by animating its scale.
            void AnimateIn()
            {
                item.transform.localScale = new Vector3(1, 0, 1);
                item.transform.DOScaleY(1, duration: 0.5f);
            }

            /// Registers a click handler for the item.
            /// Highlights the surface for 3 seconds.
            void RegisterClickHandler()
            {
                if (item.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    pointerHelper.ClickEvent.AddListener(e =>
                    {
                        GameHighlighter.EnableGlowOverlay(surface.GetRootParent());
                        Destroy(surface.GetRootParent().GetComponent<HighlightEffect>(), 3.0f);
                    });
                }
            }
        }

        /// Get the icon for the visibility mesh.
        string GetVisibilityText(bool state)
        {
            return state ? Icons.Show.ToString() : Icons.Hide.ToString();
        }

        /// Get the color for the visibility icon.
        Color GetVisibilityColor(bool state)
        {
            return state ? Color.white : Color.red.Darker();
        }

        /// Get the color for the lighting icon.
        Color GetLightColor(bool state)
        {
            return state ? Color.yellow : Color.white;
        }
    }
}
