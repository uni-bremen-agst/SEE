using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SEE.Game.UI.LiveDocumantation
{
    /// <summary>
    /// LiveDocumentationBuffer contains the documentation of a class including links as chunks of <see cref="ILiveDocumentationBufferItem"/>.
    ///
    /// Those hold either a simple text or a link
    /// </summary>
    /// <example>
    ///     Just add Text
    ///     <code>
    ///         LiveDocumentationBuffer buffer = new LiveDocumentationBuffer();
    ///         buffer.Add(new LiveDocumentationBufferText('TEXT TO ADD'));
    ///     </code>
    ///     <p/>
    ///     Adding Text and Links
    ///     <code>
    ///         LiveDocumentationBuffer buffer = new LiveDocumentationBuffer();
    ///         buffer.Add(new LiveDocumentationBufferText('TEXT TO ADD'));
    ///         buffer.Add(new LiveDocumentationLink("./path/to/class", "Link name"));
    ///     </code>
    /// </example>
    public class LiveDocumentationBuffer
    {
        private IList<ILiveDocumentationBufferItem> BufferItems;


        public LiveDocumentationBuffer()
        {
            this.BufferItems = new List<ILiveDocumentationBufferItem>();
        }


        /// <summary>
        /// Adds a new Link to the documentation buffer.
        /// According to the Unity documentation https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.2/manual/RichTextLink.html the ID tag of the link-id should be unique.
        ///
        /// Hence, this function will check if multiple links of the same class exists.
        /// If so, then a incremental number is appended
        ///
        /// The Link that should be added shall not have a counter suffix yet.
        /// This function will calculate the right counter and append it
        /// TODO: Maybe this is not necessary: try out later without this 
        /// </summary>
        /// <param name="link">The Link which should be added</param>
        public void Add(LiveDocumentationLink link)
        {
//            int linkCounter = 1;

            // Find and count all Links with the same target
            //int amountOfLinks = BufferItems
             //   .Where(x => x is LiveDocumentationLink)
              //  .Cast<LiveDocumentationLink>()
               // .Count(x => x.TargetPath.StartsWith(link.TargetPath + "::"));

            // link.TargetPath += "::" + (amountOfLinks + 1);

            BufferItems.Add(link);
        }

        /// <summary>
        /// Adds a simple text chunk to the buffer
        /// </summary>
        /// <param name="text">The Text that should be added</param>
        public void Add(LiveDocumentationBufferText text) => BufferItems.Add(text);

        
        public string PrintBuffer()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var i in BufferItems)
            {
                builder.Append(i.GetPrintableText());
            }

            return builder.ToString();
        }
    }
}