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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using TextSelection = EnvDTE.TextSelection;

namespace VSSeeExtension.Utils.Helpers
{
    /// <summary>
    /// Provides functions for handling the document view.
    /// </summary>
    public sealed class DocumentHelper
    {
        /// <summary>
        /// Gets the document that is selected in solution explorer or tab view.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <returns>Async Task containing the current selected item. Returns null
        /// when nothing found.</returns>
        public static async Task<string> GetSelectedDocumentPathAsync(
            CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            SelectedItems items = (await DteHelper.GetDteAsync(cancellationToken)).SelectedItems;
            string result = null;

            foreach (SelectedItem item in items)
            {
                ProjectItem projectItem = item.ProjectItem;
                Project project = item.Project;
                if (projectItem != null)
                {
                    result = projectItem.Properties.Item("FullPath").Value.ToString();
                }
                else if (project != null)
                {
                    result = project.Properties.Item("FullPath").Value.ToString();
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a list of all elements of choice from a solution explorer selection or tab view.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <param name="elementType">The type that is wanted.</param>
        /// <returns>The selected elements. Order (name/line/column/length)</returns>
        public static async Task<ICollection<Tuple<string, int, int, int>>> GetAllElementsInActiveDocumentAsync(
            CancellationToken cancellationToken, vsCMElement elementType)
        {
            List<Tuple<string, int, int, int>> list = new List<Tuple<string, int, int, int>>();

            foreach (CodeElement elem in (await GetAllCodeElementsAsync(cancellationToken, elementType)))
            {
                string name = await GetElementNameAsync(cancellationToken, elem);
                var (line, column, length) = await GetElementPositionAsync(cancellationToken, elem);
                list.Add(new Tuple<string, int, int, int>(name, line, column, length));
            }

            return list;
        }

        /// <summary>
        /// Returns the selected element of choice.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <param name="elementType">The type that is wanted.</param>
        /// <returns>The selected element. Order (name/line/column/length). Null if nothing selected.</returns>
        public static async Task<Tuple<string, int, int, int>> GetSelectedElementInActiveDocumentAsync(
            CancellationToken cancellationToken, vsCMElement elementType)
        {
            CodeElement selectedElement = await GetCurrentSelectedCodeElementAsync(cancellationToken, elementType);
            if (selectedElement == null) return null;
            string name = await GetElementNameAsync(cancellationToken, selectedElement);
            var (line, column, length) = await GetElementPositionAsync(cancellationToken, selectedElement);

            return new Tuple<string, int, int, int>(name, line, column, length);
        }

        /// <summary>
        /// Gets the element name of choice.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <param name="element">The code element.</param>
        /// <returns>The name of the element</returns>
        private static async Task<string> GetElementNameAsync(CancellationToken cancellationToken,
            CodeElement element)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            string name = null;
            if (element != null)
            {
                name = element.Name;
            }

            return name;
        }

        /// <summary>
        /// Gets the position and length of an element.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <param name="element">The code element.</param>
        /// <returns>Tuple line/column/length.</returns>
        private static async Task<Tuple<int, int, int>> GetElementPositionAsync(
            CancellationToken cancellationToken, CodeElement element)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // text is the complete source text for element
            string text = element.StartPoint.CreateEditPoint().GetText(element.EndPoint);
            // split into lines
            string[] split = text.Split('\n');

            // The element's name followed by an arbitrary number of newlines and spaces
            // followed by one of the following symbols: ; { = (
            Match match = Regex.Match(text, element.Name + @"[\s\n]*[;{=(]");

            int x = 0;
            int y = 0;

            if (match.Success)
            {
                int counter = 0;
                for (; x < split.Length; x++)
                {
                    if (counter + split[x].Length < match.Index)
                    {
                        counter += split[x].Length + 1;
                    }
                    else
                    {
                        y = match.Index - counter;
                        break;
                    }
                }
            }

            // For now will only use StartPoint and EndPoint.
            // return new Tuple<int, int, int>(x + element.StartPoint.Line, (x == 0 ?
            // element.StartPoint.DisplayColumn + y : y + 1), element.EndPoint.Line - (x + element.StartPoint.Line - 1));
            return new Tuple<int, int, int>
                (element.StartPoint.Line,
                 (x == 0 ? element.StartPoint.DisplayColumn + y : y + 1),
                 element.EndPoint.Line - (element.StartPoint.Line - 1));
        }

        /// <summary>
        /// Gets the selected element in the code window if it matches <paramref name="element"/>.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <param name="element">Type of wanted element.</param>
        /// <returns>The name of the selected element. Null if not selected.</returns>
        private static async Task<CodeElement> GetCurrentSelectedCodeElementAsync(
            CancellationToken cancellationToken, vsCMElement element)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                TextSelection selection = (TextSelection)(await DteHelper.GetDteAsync(cancellationToken))
                    .ActiveDocument.Selection;
                TextPoint point = (TextPoint) selection.ActivePoint;

                CodeElement elem = point.CodeElement[element];
                if (elem != null)
                {
                    return elem;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Gets all code elements of specified type.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <param name="element">The element type.</param>
        /// <returns>A list of all wanted code elements</returns>
        private static async Task<ICollection<CodeElement>> GetAllCodeElementsAsync(
            CancellationToken cancellationToken, vsCMElement element)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            SelectedItems selectedItems = (await DteHelper.GetDteAsync(cancellationToken)).SelectedItems;
            Window window = selectedItems.Item(1).ProjectItem.Open();

            return AllCodeElements(window.ProjectItem.FileCodeModel.CodeElements);

            HashSet<CodeElement> AllCodeElements(CodeElements start)
            {
                HashSet<CodeElement> codeElements = new HashSet<CodeElement>();
                foreach (CodeElement codeElement in start)
                {
                    if (codeElement.Kind == element)
                    {
                        codeElements.Add(codeElement);
                    }
                    codeElements.UnionWith(AllCodeElements(codeElement.Children));
                }

                return codeElements;
            }
        }

        /// <summary>
        /// Will highlight the active selected line.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <returns>Async Task.</returns>
        public static async Task HighlightLineAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            DTE dte = await DteHelper.GetDteAsync(cancellationToken);

            TextSelection selection = (TextSelection)dte.ActiveDocument.Selection;
            selection.StartOfLine();
            EditPoint edit = selection.ActivePoint.CreateEditPoint();
            selection.EndOfLine();
            edit.TryToShow(vsPaneShowHow.vsPaneShowTop, selection.ActivePoint);
            selection.SelectLine();
        }

        /// <summary>
        /// Opens file of the current solution.
        /// </summary>
        /// <param name="cancellationToken">To avoid the main thread transition when no longer
        /// needed.</param>
        /// <param name="path">The absolute path of the file.</param>
        /// <param name="line">Go to this line number.</param>
        /// <returns>True when successfully opened.</returns>
        public static async Task<bool> OpenFileAsync(CancellationToken cancellationToken,
            string path, int? line = null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Window window = (await DteHelper.GetDteAsync(cancellationToken)).ItemOperations.OpenFile(path);
            if (line.HasValue)
            {
                (await DteHelper.GetDteAsync(cancellationToken)).ExecuteCommand(
                    "Edit.Goto", line.ToString());
            }

            return window.Document.FullName == path;
        }
    }
}
