using SEE.Game.SceneManipulation;
using SEE.Net.Actions;

namespace SEE.Net
{
    public class MarkNetAction : AbstractNetAction
    {
        /// <summary>
        /// The ID of the parent gameObject which gets the marker.
        /// </summary>
        public string ParentID;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentID">unique ID of the parent which gets the marker</param>
        public MarkNetAction
            (string parentID)
            : base()
        {
            ParentID = parentID;
        }
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameNodeMarker.AddMarker(Find(ParentID));
            }
        }
    }
}
