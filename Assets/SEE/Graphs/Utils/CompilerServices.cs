using System.ComponentModel;

// This is a workaround for the partial support for C# records in Unity.
// By declaring the class below, we can use records without restrictions.
// It needn't be imported anywhere, as it is only used by the compiler – having it in the project is enough.

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}
