using System.Collections.Generic;
using System.Text;

namespace SEE.Game.UI.LiveDocumentation.Buffer
{
    /// <summary>
    /// LiveDocumentationBuffer contains the documentation of a class including links as chunks of
    /// <see cref="ILiveDocumentationBufferItem" />. Those hold either a simple text or a link.
    /// </summary>
    /// <example>
    /// Just add Text
    ///     <code>
    ///         LiveDocumentationBuffer buffer = new LiveDocumentationBuffer();
    ///         buffer.Add(new LiveDocumentationBufferText('TEXT TO ADD'));
    ///     </code>
    ///     <p />
    /// Adding Text and Links
    ///     <code>
    ///         LiveDocumentationBuffer buffer = new LiveDocumentationBuffer();
    ///         buffer.Add(new LiveDocumentationBufferText('TEXT TO ADD'));
    ///         buffer.Add(new LiveDocumentationLink("./path/to/class", "Link name"));
    ///     </code>
    /// </example>
    public class LiveDocumentationBuffer
    {
        /// <summary>
        /// All elements in the buffer
        /// </summary>
        private IList<ILiveDocumentationBufferItem> BufferItems { get; }
            = new List<ILiveDocumentationBufferItem>();

        /// <summary>
        /// Adds a new Link to the documentation buffer.
        /// </summary>
        /// <param name="link">The Link which should be added</param>
        public void Add(LiveDocumentationLink link)
        {
            BufferItems.Add(link);
        }

        /// <summary>
        /// Adds a simple text chunk to the buffer
        /// </summary>
        /// <param name="text">The Text that should be added</param>
        public void Add(LiveDocumentationBufferText text)
        {
            BufferItems.Add(text);
        }

        /// <summary>
        /// Prints the entire buffer with all its elements concatenated together.
        /// </summary>
        /// <returns>The buffer as one string</returns>
        public string PrintBuffer()
        {
            StringBuilder builder = new();

            foreach (ILiveDocumentationBufferItem item in BufferItems)
            {
                builder.Append(item.GetPrintableText());
            }
            return builder.ToString();
        }
    }
}