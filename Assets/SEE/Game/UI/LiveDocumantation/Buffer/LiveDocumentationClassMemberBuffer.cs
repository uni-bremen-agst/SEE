using System.Collections.Generic;
using JetBrains.Annotations;

namespace SEE.Game.UI.LiveDocumantation
{
    /// <summary>
    /// <see cref="LiveDocumentationBuffer"/> variant for class members
    ///
    /// The signature of the class member is still stored in BufferItems.
    /// But the documentation of the member is stored in <see cref="Documentation"/>
    /// </summary>
    public class LiveDocumentationClassMemberBuffer : LiveDocumentationBuffer
    {
        [NotNull] public LiveDocumentationBuffer Documentation { get; set; } = new();

        [NotNull] public List<LiveDocumentationBuffer> Parameters { get; set; } = new();

        public int LineNumber { get; set; }
    }
}