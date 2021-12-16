using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls
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
                if (ideIntegration.cachedObjects.TryGetValue(path, out IDictionary<string, ICollection<GameObject>> dictionary) && dictionary
                    .TryGetValue(ideIntegration.GenerateKey(name, line, column, length), out ICollection<GameObject> collection))
                {
                    SetInteractableObjects(collection);
                }
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
                if (ideIntegration.cachedObjects.TryGetValue(path, out IDictionary<string, ICollection<GameObject>> dictionary) && dictionary
                    .TryGetValue(ideIntegration.GenerateKey(name, line, column, length), out ICollection<GameObject> collection))
                {
                    SetInteractableObjects(SceneQueries.Find(new HashSet<string>(collection.
                        Select(x => x.ID()))));
                }
            }

            /// <summary>
            /// This method will highlight all given elements of a specific file in SEE.
            /// </summary>
            /// <param name="path">The absolute path to the source file.</param>
            /// <param name="nodes">A list of tuples representing the nodes. Order: (name/line/column)</param>
            /// <returns></returns>
            public void HighlightNodes(string path, ICollection<Tuple<string, int, int, int>> nodes)
            {
                HashSet<GameObject> objects = new HashSet<GameObject>();
                foreach (var (name, line, column, length) in nodes)
                {
                    if (ideIntegration.cachedObjects.TryGetValue(path, out IDictionary<string, ICollection<GameObject>> dictionary) && dictionary
                        .TryGetValue(ideIntegration.GenerateKey(name, line, column, length), out ICollection<GameObject> collection))
                    {
                        objects.UnionWith(collection);
                    }
                }
                SetInteractableObjects(objects);
            }

            /// <summary>
            /// Solution path changed.
            /// </summary>
            /// <returns>Async Task.</returns>
            public void SolutionChanged(string path)
            {
                ideIntegration.semaphore.Wait();
                if (ideIntegration.cachedConnections.ContainsKey(solutionPath))
                {
                    JsonRpcConnection connection = ideIntegration.cachedConnections[solutionPath];

                    if (ideIntegration.cachedSolutionPaths.Contains(path) || ideIntegration.ConnectToAny)
                    {
                        if (ideIntegration.cachedConnections.Remove(solutionPath))
                        {
                            ideIntegration.cachedConnections.Add(path, connection);
                            solutionPath = path;
                        }
                    }
                    else
                    {
                        ideIntegration.ideCalls.Decline(connection).Forget();
                    }
                }

                ideIntegration.semaphore.Release();
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
                    HashSet<InteractableObject> tmp = new HashSet<InteractableObject>();

                    foreach (GameObject node in objects)
                    {
                        if (node.TryGetComponent(out InteractableObject obj))
                        {
                            tmp.Add(obj);
                        }
                    }

                    await UniTask.SwitchToThreadPool();

                    ideIntegration.pendingSelections = tmp;
                });
            }
        }
    }
}