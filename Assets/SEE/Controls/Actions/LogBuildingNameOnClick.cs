using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Logs the building (class) name to the console when clicked.
    /// </summary>
    public class LogBuildingNameOnClick : InteractableObjectAction
    {
        /// <summary>
        /// Operator component for this object.
        /// </summary>
        private NodeOperator nodeOperator;

        /// <summary>
        /// Registers the click event handler.
        /// </summary>
        protected void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn += OnClick;
            }
            else
            {
                Debug.LogError($"{nameof(LogBuildingNameOnClick)}.OnEnable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters the click event handler.
        /// </summary>
        protected void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn -= OnClick;
            }
            else
            {
                Debug.LogError($"{nameof(LogBuildingNameOnClick)}.OnDisable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Called when the building is clicked. Logs the building name and methods to console.
        /// </summary>
        /// <param name="interactableObject">the object being clicked</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void OnClick(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                nodeOperator ??= gameObject.NodeOperator();

                if (nodeOperator.Node != null)
                {
                    string buildingName = nodeOperator.Node.SourceName ?? nodeOperator.Node.ID;
                    string buildingType = nodeOperator.Node.Type;

                    Debug.Log($"Building clicked: {buildingName} (Type: {buildingType})");

                    // Try to get methods from the source file
                    try
                    {
                        string filePath = nodeOperator.Node.AbsolutePlatformPath();

                        if (File.Exists(filePath))
                        {
                            string sourceCode = File.ReadAllText(filePath);
                            List<string> methods = ExtractMethods(sourceCode);

                            if (methods.Count > 0)
                            {
                                Debug.Log($"Methods found in {buildingName}:");
                                foreach (string method in methods)
                                {
                                    Debug.Log($"  - {method}");
                                }
                            }
                            else
                            {
                                Debug.Log($"No methods found in {buildingName}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Source file not found: {filePath}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error reading source file: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Extracts method names from C# source code using regex.
        /// </summary>
        /// <param name="sourceCode">The source code to parse</param>
        /// <returns>List of method names</returns>
        private List<string> ExtractMethods(string sourceCode)
        {
            List<string> methods = new List<string>();

            // Regex pattern to match C# method declarations
            // Matches: [access modifiers] [return type] [method name]([parameters])
            string pattern = @"(?:public|private|protected|internal|static|\s)+\s+\w+\s+(\w+)\s*\([^)]*\)";

            MatchCollection matches = Regex.Matches(sourceCode, pattern);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    string methodName = match.Groups[1].Value;
                    // Filter out common non-method keywords
                    if (methodName != "if" && methodName != "while" && methodName != "for"
                        && methodName != "foreach" && methodName != "switch" && methodName != "catch")
                    {
                        methods.Add(methodName);
                    }
                }
            }

            return methods;
        }
    }
}
