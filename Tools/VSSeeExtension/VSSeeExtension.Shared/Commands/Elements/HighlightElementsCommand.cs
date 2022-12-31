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

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using VSSeeExtension.Utils.Helpers;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension.Commands
{
    /// <summary>
    /// Base class for all commands that will highlight nodes in SEE.
    /// </summary>
    public abstract class HighlightElementsCommand : Command
    {
        /// <summary>
        /// The element in the document.
        /// </summary>
        private readonly vsCMElement elementType;

        /// <summary>
        /// Initializes <see cref="HighlightElementsCommand"/>.
        /// </summary>
        /// <param name="elementType">The element type that is wanted.</param>
        protected HighlightElementsCommand(vsCMElement elementType)
        {
            this.elementType = elementType;
        }
        /// <summary>
        /// Functionality of this command.
        /// </summary>
        protected override async Task ExecuteAsync()
        {
            await base.ExecuteAsync();
            string path = await DocumentHelper.GetSelectedDocumentPathAsync(
                Package.DisposalToken);
            ICollection<Tuple<string, int, int, int>> nodes = await DocumentHelper
                .GetAllElementsInActiveDocumentAsync(Package.DisposalToken, elementType);

            await Package.Integration.See.HighlightNodesAsync(path, nodes);
        }

        /// <summary>
        /// The visibility of this command.
        /// </summary>
        /// <returns>Visibility.</returns>
        protected override bool IsVisible()
        {
            ICollection<Tuple<string, int, int, int>> selected = ThreadHelper.JoinableTaskFactory.Run(async () =>
                await DocumentHelper.GetAllElementsInActiveDocumentAsync(Package.DisposalToken, elementType));

            return Package.Integration.IsConnected() && selected.Count > 0;
        }
    }
}
