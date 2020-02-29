//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.DataModel;

namespace SEE.Animation.Internal
{
    /// <summary>
    /// Extension that simplifies access to information of a node.
    /// </summary>
    public static class NodeExtension
    {
        /// <summary>
        /// Returns whether the attribute CodeHistory.WasAdded is set in a node.
        /// </summary>
        /// <param name="node">The node to check in.</param>
        /// <returns>True if CodeHistory.WasAdded is set in the given node.</returns>
        public static bool WasAdded(this Node node)
        {
            return node.TryGetInt("CodeHistory.WasAdded", out _);
        }

        /// <summary>
        /// Returns whether the attribute CodeHistory.WasAdded is set in a node.
        /// </summary>
        /// <param name="node">The node to check in.</param>
        /// <returns>True if CodeHistory.WasModified is set in the given node.</returns>
        public static bool WasModified(this Node node)
        {
            return node.TryGetInt("CodeHistory.WasModified", out _);
        }

        /// <summary>
        /// Returns whether the attribute CodeHistory.WasRelocated is set in a node
        /// and returns the set value.
        /// </summary>
        /// <param name="node">The node to check in.</param>
        /// <param name="oldLinkageName">The value set for CodeHistory.WasRelocated in node or null if none is set.</param>
        /// <returns>True if CodeHistory.WasRelocated is set in the given node.</returns>
        public static bool WasRelocated(this Node node, out string oldLinkageName)
        {
            oldLinkageName = null;
            return node.TryGetString("CodeHistory.WasRelocated", out oldLinkageName);
        }
    }
}