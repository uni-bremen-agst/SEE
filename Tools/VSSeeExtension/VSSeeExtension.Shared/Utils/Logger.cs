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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VSSeeExtension.Utils
{
    /// <summary>
    /// Static logger class. To get the logs, you have to run Visual Studio with the argument "/Log".
    /// After Visual Studio is closed, you can find the log at
    /// <a href="%AppData%\Microsoft\VisualStudio\&lt;version&gt;\ActivityLog.xml">Activity Log</a>. 
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Logs message.
        /// </summary>
        /// <param name="sender">Class that calls the logger.</param>
        /// <param name="message">Message that contains the information</param>
        /// <returns>Async Task.</returns>
        public static async Task LogMessageAsync(object sender, string message)
        {
            await LogAsync(sender, message, __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION);
        }

        /// <summary>
        /// Logs warning.
        /// </summary>
        /// <param name="sender">Class that calls the logger.</param>
        /// <param name="message">Message that contains the information</param>
        /// <returns>Async Task.</returns>
        public static async Task LogWarningAsync(object sender, string message)
        {
            await LogAsync(sender, message, __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING);
        }

        /// <summary>
        /// Logs error.
        /// </summary>
        /// <param name="sender">Class that calls the logger.</param>
        /// <param name="message">Message that contains the information</param>
        /// <returns>Async Task.</returns>
        public static async Task LogErrorAsync(object sender, string message)
        {
            await LogAsync(sender, message, __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);
        }

        /// <summary>
        /// Logs message with a given type.
        /// </summary>
        /// <param name="sender">Class that calls the logger.</param>
        /// <param name="message">Message that contains the information</param>
        /// <param name="type">Defines the logging type.</param>
        /// <returns>Async Task.</returns>
        private static async Task LogAsync(object sender, string message,
            __ACTIVITYLOG_ENTRYTYPE type)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsActivityLog log = (IVsActivityLog)Package.GetGlobalService(typeof(SVsActivityLog)); 
            log.LogEntry((uint)type, sender.ToString(), message);
        }
    }
}
