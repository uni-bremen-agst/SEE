using System.Collections.Generic;
using JetBrains.Annotations;

namespace SEE.Game.UI.LiveDocumentation.Buffer
{
    /// <summary>
    ///     <see cref="LiveDocumentationBuffer" /> variant for class members
    ///     The signature of the class member is still stored in <see cref="LiveDocumentationBuffer.BufferItems" />.
    ///     But the documentation of the member is stored in <see cref="Documentation" />
    /// </summary>
    public class LiveDocumentationClassMemberBuffer : LiveDocumentationBuffer
    {
        /// <summary>
        ///     Contains the actual documentation of the method
        /// </summary>
        [NotNull]
        public LiveDocumentationBuffer Documentation { get; set; } = new();

        /// <summary>
        ///     Contains the signature and the documentation of the parameters of the method
        /// </summary>
        [NotNull]
        public List<LiveDocumentationBuffer> Parameters { get; set; } = new();

        /// <summary>
        ///     The source code line number of the method.
        /// </summary>
        public int LineNumber { get; set; }
    }
}
