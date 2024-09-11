using UnityEngine;
using SEE.Utils;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using SEE.GO;
using Michsky.UI.ModernUIPack;
using System.Linq;
using System;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using UnityEngine.EventSystems;

namespace SEE.UI.Window.VariablesWindow
{
    /// <summary>
    /// Represents an item in the <see cref="VariablesWindow"/>.
    ///
    /// <para>
    /// Can be used to group items and/or represent an actual variable.
    /// </para>
    /// </summary>
    public class VariablesWindowItem : PlatformDependentComponent
    {
        /// <summary>
        /// Path to the prefab for this item.
        /// </summary>
        private const string variablesWindowItemPrefab = "Prefabs/UI/VariablesWindow/VariablesWindowItem";

        /// <summary>
        /// Color for variables.
        /// </summary>
        private static readonly Color variableColor = Color.blue.Darker();

        /// <summary>
        /// The shift per indentation level.
        /// </summary>
        private const int indentShift = 22;

        /// <summary>
        /// The name of the variable.
        /// </summary>
        public string Name;

        /// <summary>
        /// The string representation of the variable value.
        /// </summary>
        public string Text;

        /// <summary>
        /// The background color.
        /// </summary>
        public Color BackgroundColor;

        /// <summary>
        /// The variable reference.
        /// </summary>
        public int VariableReference;

        /// <summary>
        /// Function to retrieve nested variables.
        /// </summary>
        public Func<int, List<Variable>> RetrieveNestedVariables;

        /// <summary>
        /// Function to retrieve string representing the variable value.
        /// <para>
        /// Is executed on the main thread.
        /// </para>
        /// </summary>
        public Func<Variable, string> RetrieveVariableValue;

        /// <summary>
        /// The item.
        /// </summary>
        private GameObject item;

        /// <summary>
        /// Whether to display the children.
        /// </summary>
        private bool isExpanded = false;

        /// <summary>
        /// Whether the item is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (value == isExpanded)
                {
                    return;
                }
                isExpanded = value;
                UpdateExpand();
            }
        }

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
        private readonly List<VariablesWindowItem> children = new();

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
                if (value == isVisible)
                {
                    return;
                }
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
                if (value == indent)
                {
                    return;
                }
                indent = value;
                if (HasStarted)
                {
                    UpdateIndent();
                }
            }
        }

        /// <summary>
        /// The pointer helper.
        /// </summary>
        private PointerHelper pointerHelper;

        /// <summary>
        /// Whether to retrieve the children on the next update.
        /// </summary>
        private bool queueRetrieveChildren;

        /// <summary>
        /// Adds an item.
        /// </summary>
        /// <param name="child">The child item.</param>
        public void AddChild(VariablesWindowItem child)
        {
            children.Add(child);
            if (HasStarted)
            {
                UpdateChildVisibility(child);
                UpdateChildIndent(child);
                child.OnComponentInitialized += UpdateChildrenIndices;
            }
        }

        /// <summary>
        /// Adds a variable.
        /// </summary>
        /// <param name="variable">The child variable.</param>
        public void AddVariable(Variable variable)
        {
            VariablesWindowItem variableItem = gameObject.AddComponent<VariablesWindowItem>();
            variableItem.Name = variable.Name;
            variableItem.Text = variable.Name + ": " + RetrieveVariableValue(variable);
            variableItem.VariableReference = variable.VariablesReference;
            variableItem.RetrieveNestedVariables = RetrieveNestedVariables;
            variableItem.RetrieveVariableValue = RetrieveVariableValue;
            variableItem.BackgroundColor = variableColor;
            AddChild(variableItem);
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

            if (item.TryGetComponent<PointerHelper>(out pointerHelper))
            {
                if (children.Count > 0)
                {
                    pointerHelper.ClickEvent.AddListener(ToggleChildren);
                }
                else if (VariableReference > 0)
                {
                    IsExpanded = false;
                    pointerHelper.ClickEvent.AddListener(e =>
                    {
                        if (e.button == PointerEventData.InputButton.Left)
                        {
                            queueRetrieveChildren = true;
                        }
                    });
                }
                else
                {
                    expandIcon.gameObject.SetActive(false);
                }
            }

            UpdateVisibility();
            UpdateIndent();
            UpdateExpand();
        }

        /// <summary>
        /// Updates the item.
        ///
        /// <para>
        /// Retrieves child items if necessary.
        /// </para>
        /// </summary>
        protected override void Update()
        {
            base.Update();
            if (queueRetrieveChildren)
            {
                queueRetrieveChildren = false;
                RetrieveChildren();
            }
        }

        /// <summary>
        /// Destroys this item and its children.
        /// </summary>
        private void OnDestroy()
        {
            Destroyer.Destroy(item);
            foreach (VariablesWindowItem child in children)
            {
                Destroyer.Destroy(child);
            }
        }

        /// <summary>
        /// Updates the visibility.
        /// </summary>
        private void UpdateVisibility()
        {
            item.SetActive(IsVisible);
            children.ForEach(UpdateChildVisibility);
        }


        /// <summary>
        /// Updates the indent.
        /// </summary>
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

        /// <summary>
        /// Updates the indentation level of all children.
        /// </summary>
        /// <param name="child">The child that needs to be updated.</param>
        private void UpdateChildIndent(VariablesWindowItem child)
        {
            child.Indent = Indent + 1;
        }

        /// <summary>
        /// Updates the visibility of all children.
        /// </summary>
        /// <param name="child">The child that needs to be updated.</param>
        private void UpdateChildVisibility(VariablesWindowItem child)
        {
            child.IsVisible = IsVisible && isExpanded;
        }

        /// <summary>
        /// Updates (sorts) all children.
        /// </summary>
        private void UpdateChildrenIndices()
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].HasStarted)
                {
                    children[i].SetSiblingIndex(item.transform.GetSiblingIndex() + i + 1);
                }
            }
        }

        /// <summary>
        /// Updates the sibling index.
        /// </summary>
        /// <param name="index">The index.</param>
        private void SetSiblingIndex(int index)
        {
            item.transform.SetSiblingIndex(index);
        }

        /// <summary>
        /// Retrieves all children.
        /// </summary>
        private void RetrieveChildren()
        {
            pointerHelper.ClickEvent.RemoveAllListeners();
            List<Variable> childVariables = RetrieveNestedVariables(VariableReference);
            childVariables.ForEach(AddVariable);
            if (children.Count > 0)
            {
                IsExpanded = true;
                pointerHelper.ClickEvent.AddListener(ToggleChildren);
            }
            else
            {
                expandIcon.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Toggles all children.
        /// </summary>
        /// <param name="e">The click event.</param>
        private void ToggleChildren(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left)
            {
                IsExpanded = !IsExpanded;
            }
        }
    }
}
