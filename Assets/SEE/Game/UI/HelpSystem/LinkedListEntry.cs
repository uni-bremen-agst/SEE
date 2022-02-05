// Copyright 2022 Thore Frenzel.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace SEE.Game.UI.HelpSystem
{
    /// <summary>
    /// An entry for the linkedList of an HelpSystemEntry.
    /// </summary>
    public class LinkedListEntry
    {
        /// <summary>
        /// The text of this entry which will be spoken by SEE and displayed in the entry.
        /// </summary>
        private readonly string text;

        /// <summary>
        /// The cumulatedTome of this entry and the previous entries.
        /// </summary>
        private readonly int cumulatedTime;

        /// <summary>
        /// The position of the linkedListEntry in the linkedList.
        /// </summary>
        private readonly int index;

        /// <summary>
        /// Creates a new LinkedListEntry.
        /// </summary>
        /// <param name="index">the index of the entry.</param>
        /// <param name="text">the text of the entry.</param>
        /// <param name="cumulatedTime">the cumulated time of this entry and his previous.</param>
        public LinkedListEntry(int index, string text, int cumulatedTime)
        {
            this.text = text;
            this.cumulatedTime = cumulatedTime;
            this.index = index;
        }

        public int CumulatedTime { get => cumulatedTime; }
        public string Text { get => text; }
        public int Index { get => index; }
    }
}
