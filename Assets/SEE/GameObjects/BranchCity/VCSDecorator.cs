using SEE.GO;
using Sirenix.OdinInspector;

namespace SEE.GameObjects.BranchCity
{
    /// <summary>
    /// Abstract super class of game node and edge decorators in the
    /// context of a <see cref="Game.City.BranchCity"/>. Provides
    /// a cached access to the containing city.
    /// </summary>
    public abstract class VCSDecorator : SerializedMonoBehaviour
    {
        /// <summary>
        /// Backing field for <see cref="City"/>.
        /// </summary>
        private Game.City.BranchCity city;

        /// <summary>
        /// Returns the <see cref="Game.City.BranchCity"/> the associated
        /// game node or edge is contained in.
        /// </summary>
        /// <remarks>May be null if it is not contained in a <see cref="Game.City.BranchCity"/>.</remarks>
        protected Game.City.BranchCity City
        {
            get
            {
                if (city == null)
                {
                    city = gameObject.ContainingCity() as Game.City.BranchCity;
                }
                return city;
            }
        }
    }
}
