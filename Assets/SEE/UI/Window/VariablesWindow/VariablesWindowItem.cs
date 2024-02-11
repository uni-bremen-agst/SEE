using UnityEngine;
using SEE.Utils;
using DG.Tweening;
using System.Collections.Generic;
using static RootMotion.FinalIK.RagdollUtility;
using TMPro;
using SEE.GO;
using Crosstales;
using Michsky.UI.ModernUIPack;
using System.Linq;

namespace SEE.UI.Window.VariablesWindow
{
    public class VariablesWindowItem : PlatformDependentComponent
    {
        /// <summary>
        /// Path to the prefab for this item.
        /// </summary>
        private const string variablesWindowItemPrefab = "Prefabs/UI/VariablesWindow/VariablesWindowItem";

        /// <summary>
        /// The shift per indentation level.
        /// </summary>
        private const int indentShift = 22;

        /// <summary>
        /// The name.
        /// </summary>
        public string Name;

        /// <summary>
        /// The text.
        /// </summary>
        public string Text;

        /// <summary>
        /// The background color.
        /// </summary>
        public Color BackgroundColor;

        /// <summary>
        /// The item.
        /// </summary>
        private GameObject item;

        /// <summary>
        /// Whether to display the children.
        /// </summary>
        private bool isExpanded = true;

        /// <summary>
        /// The background.
        /// </summary>
        private Transform background;

        /// <summary>
        /// The foreground.
        /// </summary>
        private Transform foreground;

        /// <summary>
        /// The expand icon.
        /// </summary>
        private Transform expandIcon;

        /// <summary>
        /// List of all items.
        /// </summary>
        private List<VariablesWindowItem> children = new();

        /// <summary>
        /// Whether this item is visible.
        /// </summary>
        private bool isVisible = true;

        /// <summary>
        /// Whether this item is visible.
        /// </summary>
        public bool IsVisible
        {
            get => isVisible;
            set
            {
                if (value == isVisible) return;
                isVisible = value;
                if (HasStarted)
                {
                    UpdateVisibility();
                }
            }
        }

        /// <summary>
        /// The indentation.
        /// Children have +1 indentation.
        /// </summary>
        private int indent;

        /// <summary>
        /// The indentation.
        /// Children have +1 indentation.
        /// </summary>
        public int Indent
        {
            get => indent;
            set
            {
                if (value == indent) return;
                indent = value;
                if (HasStarted)
                {
                    UpdateIndent();
                }
            }
        }

        /// <summary>
        /// Adds an item.
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(VariablesWindowItem child)
        {
            children.Add(child);
            if (HasStarted)
            {
                UpdateChildVisibility(child);
                UpdateChildIndent(child);
                expandIcon.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Setup on the desktop platform.
        /// </summary>
        protected override void StartDesktop()
        {
            item = PrefabInstantiator.InstantiatePrefab(variablesWindowItemPrefab, transform, false);
            item.name = Name;

            TextMeshProUGUI textMesh = item.transform.Find("Foreground/Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.text = Text;
            textMesh.color = BackgroundColor.IdealTextColor();

            background = item.transform.Find("Background");
            foreground = item.transform.Find("Foreground");

            background.GetComponent<UIGradient>().EffectGradient.SetKeys(
                new Color[] { BackgroundColor, BackgroundColor.Darker(0.3f) }.ToGradientColorKeys().ToArray(),
                new GradientAlphaKey[] { new(1, 0), new(1, 1) });

            expandIcon = item.transform.Find("Foreground/Expand Icon");
            expandIcon.gameObject.SetActive(children.Count > 0);


            if (item.TryGetComponent(out PointerHelper pointerHelper))
            {
                pointerHelper.ClickEvent.AddListener(e => {
                    if (e.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left)
                    {
                        isExpanded = !isExpanded;
                        UpdateExpand();
                    }
                });
            }
            UpdateVisibility();
            UpdateIndent();
            UpdateExpand();
        }

        private void UpdateVisibility()
        {
            item.SetActive(IsVisible);
            children.ForEach(UpdateChildVisibility);
        }

        private void UpdateIndent()
        {
            background.localPosition = background.localPosition.WithXYZ(x: Indent * indentShift);
            foreground.localPosition = foreground.localPosition.WithXYZ(x: Indent * indentShift);
            children.ForEach(UpdateChildIndent);
        }

        /// <summary>
        /// Updates the children.
        /// </summary>
        private void UpdateExpand()
        {
            expandIcon.DORotate(new Vector3(0, 0, isExpanded ? -180 : -90), duration: 0.5f);
            children.ForEach(UpdateChildVisibility);
        }

        private void UpdateChildIndent(VariablesWindowItem child)
        {
            child.Indent = Indent + 1;
        }

        private void UpdateChildVisibility(VariablesWindowItem child)
        {
            child.IsVisible = IsVisible && isExpanded;
        }
    }
}