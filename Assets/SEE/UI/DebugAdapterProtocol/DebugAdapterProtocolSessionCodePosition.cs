using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.UI.Window;
using SEE.UI.Window.CodeWindow;
using SEE.Utils;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using StackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;

namespace SEE.UI.DebugAdapterProtocol
{
    /// <summary>
    /// This part of the <see cref="DebugAdapterProtocolSession"/> class handles updating the code position in the UI.
    /// </summary>
    public partial class DebugAdapterProtocolSession
    {
        /// <summary>
        /// Updates the code position.
        /// Marks it in the code window and highlights it in the city.
        /// Must be executed on the main thread.
        /// </summary>
        private void UpdateCodePosition()
        {
            if (IsRunning)
            {
                return;
            }

            StackFrame stackFrameWithSource = stackFrames.FirstOrDefault(frame => frame.Source != null);
            if (stackFrameWithSource == null)
            {
                return;
            }

            lastCodePath = stackFrameWithSource.Source.Path;
            lastCodeLine = stackFrameWithSource.Line;

            if (sourceRangeIndex != null)
            {
                string sourceRangeIndexPath = lastCodePath.Replace("\\", "/");
                if (sourceRangeIndexPath.StartsWith(City.SourceCodeDirectory.AbsolutePath))
                {
                    sourceRangeIndexPath = sourceRangeIndexPath.Substring(City.SourceCodeDirectory.AbsolutePath.Length);
                    if (sourceRangeIndexPath.StartsWith("/"))
                    {
                        sourceRangeIndexPath = sourceRangeIndexPath.Substring(1);
                    }
                }
                Node node;
                if (sourceRangeIndex.TryGetValue(sourceRangeIndexPath, lastCodeLine, out node))
                {
                    if (lastHighlighted != null && lastHighlighted.ID != node.ID)
                    {
                        Edge edge = lastHighlighted.Outgoings.FirstOrDefault(e => e.Target.ID == node.ID);
                        if (edge)
                        {
                            edge.Operator().Highlight(highlightDurationInitial, false);
                        }
                    }
                    float duration = lastHighlighted == null || lastHighlighted.ID != node.ID ? highlightDurationInitial : highlightDurationRepeated;
                    lastHighlighted = node;
                    ShowCodePosition(true, true, highlightDurationInitial);
                }
                else
                {
                    ShowCodePosition(true, true, -1);
                }
            }
            else
            {
                ShowCodePosition(true, true, -1);
            }
        }

        /// <summary>
        /// Shows the code position.
        /// </summary>
        /// <param name="makeActive">Whether to make the code window active.</param>
        /// <param name="scroll">Whether to scroll the code window.</param>
        /// <param name="highlightDuration">The highlight duration (seconds) in the city.</param>
        private void ShowCodePosition(bool makeActive = false, bool scroll = false, float highlightDuration = highlightDurationInitial)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            CodeWindow codeWindow = manager.Windows.OfType<CodeWindow>().FirstOrDefault(window => Filenames.OnCurrentPlatform(window.FilePath) == Filenames.OnCurrentPlatform(lastCodePath));
            if (codeWindow == null)
            {
                codeWindow = Canvas.AddComponent<CodeWindow>();
                codeWindow.Title = Path.GetFileName(lastCodePath);
                codeWindow.EnterFromFileAsync(lastCodePath).Forget();
                manager.AddWindow(codeWindow);
                codeWindow.OnComponentInitialized += Mark;
                codeWindow.OnComponentInitialized += MakeActive;
            }
            else
            {
                Mark();
                MakeActive();
            }
            if (lastHighlighted && highlightDuration > 0)
            {
                lastHighlighted.Operator().Highlight(highlightDuration, false);
            }

            lastCodeWindow = codeWindow;


            void MakeActive()
            {
                if (makeActive)
                {
                    manager.ActiveWindow = codeWindow;
                }
            }
            void Mark()
            {
                if (scroll)
                {
                    codeWindow.ScrolledVisibleLine = lastCodeLine;
                }
                else
                {
                    codeWindow.MarkLine(lastCodeLine);
                }
            }
        }

        /// <summary>
        /// Clears the last code position.
        /// </summary>
        private void ClearLastCodePosition()
        {
            if (lastCodeWindow)
            {
                lastCodeWindow.MarkLine(0);
            }
        }
    }
}
