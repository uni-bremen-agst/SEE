using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the line cap style (<see cref="EditAction"/>)
    /// of a line on all clients.
    /// </summary>
    public class EditLineCapStyleNetAction : DrawableNetAction
    {
        /// <summary>
        /// The ID of the line that should be changed.
        /// </summary>
        public string LineName;

        /// <summary>
        /// True if the start cap should be modified; otherwise the end cap is modified.
        /// </summary>
        public bool IsStartCap;

        /// <summary>
        /// The visual configuration to apply to the selected line cap.
        /// </summary>
        public LineCapConf CapConf;

        /// <summary>
        /// The constructor of this action. All it does is to assign its parameters to the fields.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the line is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="lineName">The ID of the line that should be changed.</param>
        /// <param name="isStartCap">Whether the start cap should be modified.</param>
        /// <param name="capConf">The configuration of the line cap.</param>
        public EditLineCapStyleNetAction(string drawableID, string parentDrawableID, string lineName,
            bool isStartCap, LineCapConf capConf)
            : base(drawableID, parentDrawableID)
        {
            LineName = lineName;
            IsStartCap = isStartCap;
            CapConf = capConf;
        }

        /// <summary>
        /// Changes the style of the line cap of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">
        /// Will be thrown, if the <see cref="LineName"/> does not exist.
        /// </exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameEdit.ChangeLineCapStyle(FindChild(LineName), IsStartCap, CapConf);
        }
    }
}