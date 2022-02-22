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
using VSSeeExtension.Utils;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension.Commands
{
    /// <summary>
    /// The command in Visual Studio to highlight a class in SEE.
    /// </summary>
    public sealed class HighlightClassCommand : HighlightElementCommand
    {
        /// <summary>
        /// Singleton constructor.
        /// </summary>
        private HighlightClassCommand() : base(vsCMElement.vsCMElementClass)
        {
        }

        /// <summary>
        /// Singleton instance of this class.
        /// </summary>
        public static HighlightClassCommand Instance { get; private set; }

        /// <summary>
        /// Initialize the <see cref="HighlightClassCommand"/> class.
        /// </summary>
        /// <param name="package">The Package which is calling initializing the button.</param>
        /// <returns></returns>
        public static async Task InitializeAsync(VSSeeExtensionPackage package)
        {
            Instance = new HighlightClassCommand();
            await Instance.AddCommandAsync(package, PackageInfo.VSSeePackageCommandSet,
                PackageInfo.HighlightClassCommandId);
        }
    }
}
