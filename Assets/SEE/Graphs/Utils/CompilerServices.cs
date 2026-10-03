using System.ComponentModel;

// This is a workaround for the partial support for C# records in Unity.
// By declaring the class below, we can use records without restrictions.
// It needn't be imported anywhere, as it is only used by the compiler – having it in the project is enough.
// It is public so that every assembly referencing SEE.Graphs uses this declaration. A second
// declaration in one of them would be ambiguous for the tests, which see the internals of both.

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class IsExternalInit { }
}
