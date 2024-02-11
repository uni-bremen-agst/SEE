using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.UI.Window;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window.VariablesWindow
{
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
        /// Color for variables.
        /// </summary>
        private static readonly Color variableColor = Color.blue.Darker();

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
        /// Container for the items of the window.
        /// </summary>
        private GameObject items;

        /// <summary>
        /// Setup for the desktop platform.
        /// </summary>
        protected override void StartDesktop()
        {
            Debug.Log("StartDesktop "+ this);
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

        /// <summary>
        /// Rebuilds the items.
        /// </summary>
        private void Rebuild()
        {
            Debug.Log("Rebuild " + this);
            foreach (VariablesWindowItem child in items.GetComponents<VariablesWindowItem>())
            {
                Destroyer.Destroy(child);
            }

            foreach ((Thread thread, Dictionary<StackFrame, Dictionary<Scope, List<Variable>>> threadVariables) in Variables)
            {
                VariablesWindowItem threadItem = items.AddComponent<VariablesWindowItem>();
                threadItem.Name = thread.Name;
                threadItem.Text = thread.Name;
                threadItem.BackgroundColor = threadColor;

                foreach ((StackFrame stackFrame, Dictionary<Scope, List<Variable>> stackFrameVariables) in threadVariables)
                {
                    VariablesWindowItem stackFrameItem = items.AddComponent<VariablesWindowItem>();
                    threadItem.AddChild(stackFrameItem);
                    stackFrameItem.Name = stackFrame.Name;
                    stackFrameItem.Text = stackFrame.Name;
                    stackFrameItem.BackgroundColor = stackFrameColor;

                    foreach ((Scope scope, List<Variable> scopeVariables) in stackFrameVariables)
                    {
                        VariablesWindowItem scopeItem = items.AddComponent<VariablesWindowItem>();
                        stackFrameItem.AddChild(scopeItem);
                        scopeItem.Name = scope.Name;
                        scopeItem.Text = scope.Name;
                        scopeItem.BackgroundColor = scopeColor;

                        foreach (Variable variable in scopeVariables)
                        {
                            VariablesWindowItem variableItem = items.AddComponent<VariablesWindowItem>();
                            scopeItem.AddChild(variableItem);
                            variableItem.Name = variable.Name;
                            variableItem.Text = variable.Name + ": " + variable.Value;
                            variableItem.BackgroundColor = variableColor;
                        }
                    }
                }
            }
        }


        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            // TODO: Should tree windows be sent over the network?
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