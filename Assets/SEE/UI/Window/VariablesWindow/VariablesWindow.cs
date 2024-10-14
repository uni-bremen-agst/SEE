using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window.VariablesWindow
{
    /// <summary>
    /// Represents a movable, scrollable window containing the variables of a debug session.
    /// </summary>
    public class VariablesWindow : BaseWindow
    {
        /// <summary>
        /// The prefab for this window.
        /// </summary>
        private const string variablesWindowPrefab = "Prefabs/UI/VariablesWindow/VariablesWindow";

        /// <summary>
        /// Color for threads.
        /// </summary>
        private static readonly Color threadColor = Color.gray.Darker();

        /// <summary>
        /// Color for stack frames.
        /// </summary>
        private static readonly Color stackFrameColor = Color.magenta.Darker();

        /// <summary>
        /// Color for scopes.
        /// </summary>
        private static readonly Color scopeColor = Color.yellow.Darker();

        /// <summary>
        /// The variables.
        /// </summary>
        private Dictionary<Thread, Dictionary<StackFrame, Dictionary<Scope, List<Variable>>>> variables;

        /// <summary>
        /// The variables.
        /// </summary>
        public Dictionary<Thread, Dictionary<StackFrame, Dictionary<Scope, List<Variable>>>> Variables
        {
            get => variables;
            set
            {
                variables = value;
                if (HasStarted)
                {
                    Rebuild();
                }
            }
        }

        /// <summary>
        /// Function to retrieve nested variables.
        /// </summary>
        public Func<int, List<Variable>> RetrieveNestedVariables;

        /// <summary>
        /// Function to retrieve the string representing the variable value.
        /// </summary>
        public Func<Variable, string> RetrieveVariableValue;

        /// <summary>
        /// Container for the items of the window.
        /// </summary>
        private GameObject items;

        protected override void StartDesktop()
        {
            Title ??= "Variables";
            base.StartDesktop();

            Transform root = PrefabInstantiator.InstantiatePrefab(variablesWindowPrefab, Window.transform.Find("Content"), false).transform;

            items = root.Find("Content/Items").gameObject;
            foreach (Transform child in items.transform)
            {
                Destroyer.Destroy(child.gameObject);
            }

            Rebuild();
        }

        private void Rebuild()
        {
            foreach (VariablesWindowItem child in items.GetComponents<VariablesWindowItem>())
            {
                Destroyer.Destroy(child);
            }

            bool topThread = true;
            foreach ((Thread thread, Dictionary<StackFrame, Dictionary<Scope, List<Variable>>> threadVariables) in Variables)
            {
                VariablesWindowItem threadItem = items.AddComponent<VariablesWindowItem>();
                threadItem.Name = thread.Name;
                threadItem.Text = thread.Name;
                threadItem.BackgroundColor = threadColor;
                threadItem.IsExpanded = topThread;

                bool topStack = true;
                foreach ((StackFrame stackFrame, Dictionary<Scope, List<Variable>> stackFrameVariables) in threadVariables)
                {
                    VariablesWindowItem stackFrameItem = items.AddComponent<VariablesWindowItem>();
                    threadItem.AddChild(stackFrameItem);
                    stackFrameItem.Name = stackFrame.Name;
                    stackFrameItem.Text = stackFrame.Name;
                    stackFrameItem.IsExpanded = topThread && topStack;
                    stackFrameItem.BackgroundColor = stackFrameColor;

                    foreach ((Scope scope, List<Variable> scopeVariables) in stackFrameVariables)
                    {
                        VariablesWindowItem scopeItem = items.AddComponent<VariablesWindowItem>();
                        stackFrameItem.AddChild(scopeItem);
                        scopeItem.Name = scope.Name;
                        scopeItem.Text = scope.Name;
                        scopeItem.BackgroundColor = scopeColor;
                        scopeItem.IsExpanded = topThread && topStack;
                        // passed down to variables
                        scopeItem.RetrieveNestedVariables = RetrieveNestedVariables;
                        scopeItem.RetrieveVariableValue = RetrieveVariableValue;

                        scopeVariables.ForEach(scopeItem.AddVariable);
                    }

                    topStack = false;
                }
                topThread = false;
            }
        }

        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            throw new NotImplementedException();
        }
    }
}
