using Assets.SEE.Game.Drawable;
using Assets.SEE.Net.Actions.Drawable;
using HighlightPlus;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.UI.Menu.Drawable;
using SEE.UI.PopupMenu;
using SEE.UI.PropertyDialog.Drawable;
using SEE.UI.Window.TreeWindow;
using SEE.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Assets.SEE.UI.Window.DrawableManagerWindow
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
            PopupMenu popupMenu = gameObject.AddComponent<PopupMenu>();
            UnityEvent<List<GameObject>> rebuild = new ();
            rebuild.AddListener(list => { Rebuild(list);});
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
            Destroyer.Destroy(item);
        }

        /// <summary>
        /// Adds a given drawable surface to the drawable manager view.
        /// </summary>
        /// <param name="surface">The drawable surface to be added.</param>
        private void AddItem(GameObject surface)
        {
            DrawableConfig config = DrawableConfigManager.GetDrawableConfig(surface);
            GameObject item = PrefabInstantiator.InstantiatePrefab(dmItemPrefab, items, false);
            Transform background = item.transform.Find("Background");
            Transform foreground = item.transform.Find("Foreground");
            GameObject expandIcon = foreground.Find("Expand Icon").gameObject;
            expandIcon.SetActive(false);

            TextMeshProUGUI textMesh = foreground.Find("Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.text = !string.IsNullOrWhiteSpace(config.ParentID) ? config.ParentID : config.ID;

            TextMeshProUGUI iconMesh = foreground.Find("Type Icon").gameObject.MustGetComponent<TextMeshProUGUI>();
            iconMesh.text = GameFinder.IsWhiteboard(surface) ? Icons.Whiteboard.ToString() : Icons.StickyNote.ToString();

            ButtonManagerBasic descriptionBtn = foreground.Find("DescriptionBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
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
                    descriptionMesh.text = textOut;

                    if ((!string.IsNullOrEmpty(textOut) && !string.IsNullOrWhiteSpace(textOut) 
                        && !contextMenu.filter.IncludeHaveDescription)
                    || ((string.IsNullOrEmpty(textOut) || string.IsNullOrWhiteSpace(textOut)) 
                        && !contextMenu.filter.IncludeHaveNoDescription))
                    {
                        RemoveItem(item);
                    }
                };

                writeTextDialog.Open(stringAction);
            });

            ButtonManagerBasic colorBtn = foreground.Find("ColorBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            TextMeshProUGUI colorMesh = foreground.Find("ColorBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            colorMesh.color = config.Color;
            colorBtn.clickEvent.AddListener(() =>
            {
                if (!SurfaceColorMenu.IsOpen())
                {
                    UnityAction<Color> colorAction = colorOut =>
                    {
                        colorMesh.color = colorOut;
                    };
                    SurfaceColorMenu.Enable(surface, colorAction);
                }
            });

            ButtonManagerBasic lightingBtn = foreground.Find("LightingBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            TextMeshProUGUI lightMesh = foreground.Find("LightingBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            lightMesh.color = GetLightColor(config.Lighting);
            lightingBtn.clickEvent.AddListener(() =>
            {
                bool light = !GameFinder.GetDrawableSurfaceParent(surface).GetComponentInChildren<Light>().enabled;
                GameDrawableManager.ChangeLighting(surface, light);
                new DrawableChangeLightingNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                lightMesh.color = GetLightColor(light);

                if (light && !contextMenu.filter.IncludeHaveLighting
                || !light && !contextMenu.filter.IncludeHaveNoLighting)
                {
                    RemoveItem(item);
                }
            });

            ButtonManagerBasic visibilityBtn = foreground.Find("VisibilityBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            TextMeshProUGUI visibilityMesh = foreground.Find("VisibilityBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            visibilityBtn.buttonText = GetVisibilityText(config.Visibility);
            visibilityMesh.color = GetVisibilityColor(config.Visibility);
            visibilityBtn.clickEvent.AddListener(() =>
            {
                bool visibility = !GameFinder.GetHighestParent(surface).activeInHierarchy;
                GameDrawableManager.ChangeVisibility(surface, visibility);
                new DrawableChangeVisibilityNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                visibilityMesh.text = GetVisibilityText(visibility);
                visibilityMesh.color = GetVisibilityColor(visibility);
                
                if (visibility && !contextMenu.filter.IncludeIsVisible
                || !visibility && !contextMenu.filter.IncludeIsInvisibile)
                {
                    RemoveItem(item);
                }
            });

            ColorItem();
            RegisterClickHandler();
            return;

            string GetVisibilityText(bool state)
            {
                return state ? Icons.Show.ToString() : Icons.Hide.ToString();
            }

            Color GetVisibilityColor(bool state)
            {
                return state ? Color.white : Color.red;
            }

            Color GetLightColor(bool state)
            {
                return state ? Color.yellow : Color.white;
            }

            void ColorItem()
            {

            }

            /// Registers a click handler for the item.
            /// Highlights the surface for 3 seconds.
            void RegisterClickHandler()
            {
                if (item.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    pointerHelper.ClickEvent.AddListener(e =>
                    {
                        GameHighlighter.EnableGlowOverlay(GameFinder.GetHighestParent(surface));
                        Destroy(GameFinder.GetHighestParent(surface).GetComponent<HighlightEffect>(), 3.0f);
                    });
                }
            }
        }
    }
}