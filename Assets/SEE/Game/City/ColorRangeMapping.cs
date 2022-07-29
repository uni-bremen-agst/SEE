using System;
using System.Collections.Generic;

namespace SEE.Game.City
{
    /// <summary>
    /// A mapping of property names (e.g., name of node types) onto <see cref="ColorRange"/>.
    /// This concrete instantiation is needed because Unity cannot serialize generic types.
    /// </summary>
    [Serializable]
    public class ColorRangeMapping : Dictionary<string, ColorRange> { }
}
