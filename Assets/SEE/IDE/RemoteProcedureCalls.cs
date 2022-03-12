// Copyright © 2022 Jan-Philipp Schramm
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Game;
using SEE.GO;
using SEE.Utils.RPC;
using UnityEngine;

namespace SEE.IDE
{
    /// <summary>
    /// This script establishes the connection to an IDE of choice. There is the
    /// option to choose between all possible IDE implementations. Currently,
    /// only Visual Studio is supported, but could be easily extended in the
    /// future.
    /// Note: Only one instance of this class can be created.
    /// </summary>
    public partial class IDEIntegration : MonoBehaviour
    {
        /// <summary>
        /// This class contains all functions, that can be called by the client (IDE). It will be
        /// given to each <see cref="JsonRpcConnection"/> individually.
        /// </summary>
        private class RemoteProcedureCalls
        {
            /// <summary>
            /// instance of parent class.
            /// </summary>
            private readonly IDEIntegration ideIntegration;

            /// <summary>
            /// The current solution path of the connected IDE.
            /// </summary>
            private string solutionPath;

            /// <summary>
            /// Nested class in <see cref="IDEIntegration"/>. Contains all methods that can be accessed
            /// by the client. Should only be initiated by the <see cref="IDEIntegration"/>.
            /// </summary>
            /// <param name="ideIntegration">instance of IDEIntegration</param>
            /// <param name="solutionPath">The solution path of the connected IDE.</param>
            public RemoteProcedureCalls(IDEIntegration ideIntegration, string solutionPath)
            {
                this.ideIntegration = ideIntegration;
                this.solutionPath = solutionPath;
            }

            /// <summary>
            /// Adds all nodes from <see cref="cachedObjects"/> with the key created by
            /// <paramref name="path"/> and <paramref name="name"/>. If <paramref name="name"/> is
            /// null, it won't be appended to the key.
            /// </summary>
            /// <param name="path">The absolute path to the source file.</param>
            /// <param name="name">Name of the element in a file.</param>
            /// <param name="line">Line of the element.</param>
            /// <param name="column">Column of the element.</param>
            /// <param name="length">The length of the code range.</param>
            public void HighlightNode(string path, string name, int line, int column, int length)
            {
                if (TryLookUp(path, name, line, column, length, out ICollection<GameObject> collection))
                {
                    SetInteractableObjects(collection);
                }
            }

            /// <summary>
            /// Retrieves the <paramref name="collection"/> contained in <see cref="ideIntegration.cachedObjects"/>
            /// for the given attributes. Returns true if a collection is found. If none is found, false is
            /// return and <paramref name="collection"/> will be null.
            /// </summary>
            /// <param name="path">the path of the search entity</param>
            /// <param name="name">the name of the search entity</param>
            /// <param name="line">the source line of the search entity</param>
            /// <param name="column">the source column of the search entity</param>
            /// <param name="length">the length of the declaring region of the search entity</param>
            /// <param name="collection">the found collection or null</param>
            /// <returns>true if a collection was found</returns>
            private bool TryLookUp(string path, string name, int line, int column, int length, out ICollection<GameObject> collection)
            {
                collection = null;
                if (!ideIntegration.cachedObjects.TryGetValue(path, out IDictionary<string, ICollection<GameObject>> dictionary))
                {
                    Debug.LogError($"Unknown file {path}.\n");
                    return false;
                }
                if (!dictionary.TryGetValue(ideIntegration.GenerateKey(name, line, column, length), out collection))
                {
                    Debug.LogError($"Unknown entity [name={name} line={line} column={column} length={length}] in file {path}.\n");
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Adds all edges from <see cref="cachedObjects"/> with the key created by
            /// <paramref name="path"/> and <paramref name="name"/>. If <paramref name="name"/> is
            /// null, it won't be appended to the key.
            /// </summary>
            /// <param name="path">The absolute path to the source file.</param>
            /// <param name="name">Name of the element in a file.</param>
            /// <param name="line">Line of the element.</param>
            /// <param name="column">Column of the element.</param>
            /// <param name="length">The length of the code range.</param>
            public void HighlightNodeReferences(string path, string name, int line, int column, int length)
            {
                HashSet<GameObject> objects = new HashSet<GameObject>();
                if (TryLookUp(path, name, line, column, length, out ICollection<GameObject> collection))
                {
                    UniTask.Run(async () =>
                    {
                        await UniTask.SwitchToMainThread();
                        HashSet<string> incomingEdges
                           = new HashSet<string>(collection.SelectMany(x => x.GetNode().Incomings).Select(x => x.ID));
                        SetInteractableObjects(SceneQueries.Find(incomingEdges));
                    });
                }
                SetInteractableObjects(objects);
            }

            /// <summary>
            /// This method will highlight all given elements of a specific file in SEE.
            /// </summary>
            /// <param name="path">The absolute path to the source file.</param>
            /// <param name="nodes">A list of tuples representing the nodes. Order: (name/line/column)</param>
            public void HighlightNodes(string path, ICollection<Tuple<string, int, int, int>> nodes)
            {
                HashSet<GameObject> objects = new HashSet<GameObject>();
                foreach (var (name, line, column, length) in nodes)
                {
                    if (TryLookUp(path, name, line, column, length, out ICollection<GameObject> collection))
                    {
                        objects.UnionWith(collection);
                    }
                }
                SetInteractableObjects(objects);
            }

            /// <summary>
            /// Solution path changed.
            /// </summary>
            /// <param name="path">path of the solution</param>
            public void SolutionChanged(string path)
            {
                if (ideIntegration.cachedConnections.ContainsKey(solutionPath))
                {
                    JsonRpcConnection connection = ideIntegration.cachedConnections[solutionPath];

                    if (ideIntegration.cachedSolutionPaths.Contains(path) || ideIntegration.ConnectToAny)
                    {
                        if (ideIntegration.cachedConnections.TryRemove(solutionPath, out _))
                        {
                            ideIntegration.cachedConnections.GetOrAdd(path, connection);
                            solutionPath = path;
                        }
                    }
                    else
                    {
                        ideIntegration.ideCalls.Decline(connection).Forget();
                    }
                }
            }

            /// <summary>
            /// Will transform the given collection to a set of <see cref="InteractableObject"/> and
            /// add them to <see cref="pendingSelections"/>.
            /// </summary>
            /// <param name="objects">The collection of GameObjects representing nodes.</param>
            private void SetInteractableObjects(IEnumerable<GameObject> objects)
            {
                UniTask.Run(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    HashSet<InteractableObject> selections = new HashSet<InteractableObject>();

                    foreach (GameObject node in objects)
                    {
                        if (node.TryGetComponent(out InteractableObject obj))
                        {
                            selections.Add(obj);
                        }
                    }

                    await UniTask.SwitchToThreadPool();

                    ideIntegration.pendingSelections = selections;
                });
            }
        }
    }
}