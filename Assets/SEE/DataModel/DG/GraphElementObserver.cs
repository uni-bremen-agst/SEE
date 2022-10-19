using System;
using System.Collections.Generic;
using VivoxUnity.Properties;

namespace SEE.DataModel.DG
{
    public partial class Graph
    {
        /// <summary>
        /// Observer for graph elements. This way, changes in each element (e.g., attribute changes) are also
        /// propagated through the graph's own observable implementation.
        /// </summary>
        private readonly GraphElementObserver ElementObserver;

        /// <summary>
        /// A wrapper class for the observer functionality, as we need to observe graph elements.
        /// We don't implement <see cref="IObserver{T}"/> on <see cref="Graph"/> because its methods should not be
        /// accessible from the outside.
        /// </summary>
        private class GraphElementObserver : IObserver<GraphEvent>
        {
            /// <summary>
            /// Set of all disposables associated with the observables this observer observes.
            /// </summary>
            private readonly ISet<IDisposable> Disposables = new HashSet<IDisposable>();
            
            /// <summary>
            /// The graph this observer belongs to.
            /// </summary>
            private readonly Graph Graph;

            public GraphElementObserver(Graph graph)
            {
                Graph = graph;
            }

            public void OnCompleted()
            {
                // Should never be called.
                throw new NotImplementedException();
            }

            public void OnError(Exception error) => throw error;

            public void OnNext(GraphEvent value) => Graph.Notify(value);

            /// <summary>
            /// Disposes all <see cref="Disposables"/>, thereby unsubscribing from all observables.
            /// </summary>
            public void Reset()
            {
                foreach (IDisposable disposable in Disposables)
                {
                    disposable.Dispose();
                }
                Disposables.Clear();
            }

            /// <summary>
            /// Adds a new <paramref name="disposable"/>. Should be called whenever we subscribe to a new observable.
            /// </summary>
            /// <param name="disposable">the new observable's disposable</param>
            public void AddDisposable(IDisposable disposable)
            {
                if (!Disposables.Contains(disposable))
                {
                    Disposables.Add(disposable);
                }
            }
        }
    }
}