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
using System.Threading.Tasks;

namespace VSSeeExtension.SEE
{
    public partial class SeeIntegration
    {
        /// <summary>
        /// A class for all possible remote procedure calls to the server (SEE).
        /// </summary>
        public class SeeCalls
        {
            /// <summary>
            /// Parent class.
            /// </summary>
            private readonly SeeIntegration integration;

            /// <summary>
            /// Constructor of SeeCalls. This class is only constructed by <see cref="SeeIntegration"/>.
            /// </summary>
            /// <param name="integration">The parent class.</param>
            public SeeCalls(SeeIntegration integration)
            {
                this.integration = integration;
            }

            /// <summary>
            /// This method will call SEE to highlight the node of a code city.
            /// </summary>
            /// <param name="path">Absolute file path of the document.</param>
            /// <param name="name">The name of the element.</param>
            /// <param name="line">Line of the element.</param>
            /// <param name="column">Column of the element.</param>
            /// <param name="length">The length of the code range.</param>
            /// <returns>Async Task.</returns>
            public async Task HighlightNodeAsync(string path, string name, int line, int column, int length)
            {
                await integration.rpc.CallRemoteProcessAsync("HighlightNode", path, name, line, column, length);
            }

            /// <summary>
            /// This method will call SEE to highlight all references of a node.
            /// </summary>
            /// <param name="path">Absolute file path of the document.</param>
            /// <param name="name">The name of the element.</param>
            /// <param name="line">Line of the element.</param>
            /// <param name="column">Column of the element.</param>
            /// <param name="length">The length of the code range.</param>
            /// <returns>Async Task.</returns>
            public async Task HighlightNodeReferencesAsync(string path, string name, int line, int column, int length)
            {
                await integration.rpc.CallRemoteProcessAsync("HighlightNodeReferences", path, name, line, column, length);
            }

            /// <summary>
            /// This method will highlight all given elements of a specific file in SEE.
            /// </summary>
            /// <param name="path">The absolute path to the source file.</param>
            /// <param name="nodes">A list of tuples representing the nodes. Order: (name/line/column/length)</param>
            /// <returns>Async Task.</returns>
            public async Task HighlightNodesAsync(string path, ICollection<Tuple<string, int, int, int>> nodes)
            {
                await integration.rpc.CallRemoteProcessAsync("HighlightNodes", path, nodes);
            }

            /// <summary>
            /// Notifies SEE that the loaded project has changed.
            /// </summary>
            /// <param name="path">New solution path.</param>
            /// <returns>Async Task.</returns>
            public async Task SolutionChangedAsync(string path)
            {
                await integration.rpc.CallRemoteProcessAsync("SolutionChanged", path);
            }
        }
    }
}
