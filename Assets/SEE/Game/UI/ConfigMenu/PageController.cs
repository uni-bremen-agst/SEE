// Copyright 2021 Ruben Smidt
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

using TMPro;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// Controls the active page (mostly its headline).
    /// </summary>
    public class PageController : DynamicUIBehaviour
    {
        /// <summary>
        /// The headline text of this page.
        /// </summary>
        public string HeadlineText;

        public void Start()
        {
            /// FIXME: This is potentially a future problem because the
            /// <see cref="HeadlineText"/> will be set by methods that are also
            /// called by a Start method. Thus, the execution of those Start
            /// methods play a role.
            MustGetComponentInChild("Heading", out TextMeshProUGUI headline);
            headline.text = HeadlineText;
        }
    }
}

