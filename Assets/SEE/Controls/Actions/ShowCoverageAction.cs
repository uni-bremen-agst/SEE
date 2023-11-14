using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace Assets.SEE.Controls.Actions
{
    internal class ShowCoverageAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ShowCoverage"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.ShowCoverage;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
            };
        }

        /// <summary>
        /// Returns a new instance of <see cref="ShowCoverageAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new ShowCoverageAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="ShowCoverageAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /* public static CoverageWindow ShowCoverage(GraphElementRef graphElementRef)
        /{
            GraphElement graphElement = graphElementRef.Elem;
            // File name of source code file to read from it
            (string filename, string absolutePlatformPath) = GetPath(graphElement);
        }
        */

        /// <summary>
        /// Returns the filename and the absolute platform-specific path of
        /// given graphElement.
        /// </summary>
        /// <param name="graphElement">The graph element to get the filename and path for</param>
        /// <returns>filename and absolute path</returns>
        /// <exception cref="InvalidOperationException">
        /// If the given graphElement has no filename or the path does not exist
        /// </exception>
        /// Used from the ShowCode Class
        private static (string filename, string absolutePlatformPath) GetPath(GraphElement graphElement)
        {
            string filename = graphElement.Filename();
            if (filename == null)
            {
                string message = $"Selected {GetName(graphElement)} has no filename.";
                ShowNotification.Error("No filename", message, log: false);
                throw new InvalidOperationException(message);
            }
            string absolutePlatformPath = graphElement.AbsolutePlatformPath();
            if (!File.Exists(absolutePlatformPath))
            {
                string message = $"Path {absolutePlatformPath} of selected {GetName(graphElement)} does not exist.";
                ShowNotification.Error("Path does not exist", message, log: false);
                throw new InvalidOperationException(message);
            }
            if ((File.GetAttributes(absolutePlatformPath) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                string message = $"Path {absolutePlatformPath} of selected {GetName(graphElement)} is a directory.";
                ShowNotification.Error("Path is a directory", message, log: false);
                throw new InvalidOperationException(message);
            }
            Debug.Log(filename);
            Debug.Log(absolutePlatformPath);
            Debug.Log(graphElement.ID);

            return (filename, absolutePlatformPath);
        }

        /// <summary>
        /// Returns a human-readable representation of given graphElement.
        /// </summary>
        /// <param name="graphElement">The graph element to get the name for</param>
        /// <returns>human-readable name</returns>
        private static string GetName(GraphElement graphElement)
        {
            return graphElement.ToShortString();
        }

        public override bool Update()
        {
            //throw new System.NotImplementedException();

            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit _, out GraphElementRef graphElementRef) != HitGraphElement.None)
            {
                GraphElement graphElement = graphElementRef.Elem;
                // File name of source code file to read from it
                (string filename, string absolutePlatformPath) = GetPath(graphElement);

            }
            return true;
        }
    }
}