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
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace VSSeeExtension.Options
{
    /// <summary>
    /// Here are all settings from Visual Studios options Page.
    /// </summary>
    public sealed class VSSeeExtensionOptions : DialogPage
    {
        /// <summary>
        /// Name of the TCP category.
        /// </summary>
        private const string TcpCategory = "IPC - TCP";

        /// <summary>
        /// Name of the common category.
        /// </summary>
        private const string CommonCategory = "Common";

        /// <summary>
        /// EventHandler when Settings changes.
        /// </summary>
        public event EventHandler SettingsChanged;

        /// <summary>
        /// TCP Port for the See.
        /// </summary>
        [Category(TcpCategory)]
        [DisplayName("TCP Port")]
        [Description("Determines the port of Socket TCP connection.")]
        [DefaultValue(26100)]
        public int TcpPort { get; set; } = 26100;

        /// <summary>
        /// Auto connect option.
        /// </summary>
        [Category(CommonCategory)]
        [DisplayName("Auto Connect")]
        [Description("Determines if the extension tries to auto connect.")]
        [DefaultValue(true)]
        public bool AutoConnect { get; set; } = true;

        /// <summary>
        /// Publishes the event to all listeners.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            EventHandler raiseEvent = SettingsChanged;
            raiseEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
