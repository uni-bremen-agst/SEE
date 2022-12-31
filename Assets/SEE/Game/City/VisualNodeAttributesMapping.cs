using System;
using System.Collections.Generic;

namespace SEE.Game.City
{
    /// <summary>
    /// A mapping of node-type names onto <see cref="VisualNodeAttributes"/>.
    /// This concrete instantiation is needed because Unity cannot serialize generic types.
    /// </summary>
    [Serializable]
    public class VisualNodeAttributesMapping : Dictionary<string, VisualNodeAttributes> { }
}
