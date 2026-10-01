using System.Runtime.CompilerServices;

// Internals must be visible to SEE and our tests.
[assembly: InternalsVisibleTo("SEE")]
[assembly: InternalsVisibleTo("SEETests")]
[assembly: InternalsVisibleTo("SEEPlayModeTests")]
[assembly: InternalsVisibleTo("SEE_Editor")]
/// <summary>
/// This namespace provides data structures and algorithms for dependency graphs.
/// </summary>
namespace SEE.Graphs
{
}