using Assets.SEE.Game.Drawable;
using Assets.SEE.Net.Actions.Drawable;
using Dissonance.Audio;
using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        /// The button that opens the grouping menu.
        /// </summary>
        private ButtonManagerBasic groupButton;

        /// <summary>
        /// The button that opens the sorting menu.
        /// </summary>
        private ButtonManagerBasic sortButton;

        /// <summary>
        /// A set of all items (drawable IDs) that have been expanded.
        /// </summary>
        private readonly ISet<string> expandedItems = new HashSet<string>();

        protected override void StartDesktop()
        {
            Title = "Drawable Surface Manager";
            base.StartDesktop();
            Transform root = PrefabInstantiator.InstantiatePrefab(dmWindowPrefab, Window.transform.Find("Content"), false).transform;
            items = (RectTransform)root.Find("Content/Items");
            scrollRect = root.gameObject.MustGetComponent<ScrollRect>();

            /// TODO: SEARCH ETC
            
            Rebuild();
        }

        /// <summary>
        /// Rebuilds the drawable manager view.
        /// </summary>
        private void Rebuild()
        {
            ClearManager();
            AddDrawableSurfaces();
        }

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

            ButtonManagerBasic colorBtn = foreground.Find("ColorBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            TextMeshProUGUI colorMesh = foreground.Find("ColorBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            colorMesh.color = config.Color;

            ButtonManagerBasic lightningBtn = foreground.Find("LightningBtn").gameObject.MustGetComponent<ButtonManagerBasic>();
            TextMeshProUGUI lightMesh = foreground.Find("LightningBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
            lightMesh.color = GetLightColor(config.Lightning);
            lightningBtn.clickEvent.AddListener(() =>
            {
                bool light = !GameFinder.GetDrawableSurfaceParent(surface).GetComponentInChildren<Light>().enabled;
                GameDrawableManager.ChangeLightning(surface, light);
                new DrawableChangeLightningNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                lightMesh.color = GetLightColor(light);
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
            });

            //ColorItem();
            return;

            string GetVisibilityText(bool state)
            {
                return state? Icons.Show.ToString() : Icons.Hide.ToString();
            }

            Color GetVisibilityColor(bool state)
            {
                return state? Color.white : Color.red;
            }

            Color GetLightColor(bool state)
            {
                return state ? Color.yellow : Color.white;
            }
        }
    }
}