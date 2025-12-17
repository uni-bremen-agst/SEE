using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing all values (<see cref="EditAction"/>) of a text on all clients.
    /// </summary>
    public class EditTextNetAction : DrawableNetAction
    {
        /// <summary>
        /// The Text that should be changed. The Text object contains all relevant values to change.
        /// </summary>
        public TextConf Text;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="text">The text that contains the values to change the associated game object.</param>
        public EditTextNetAction(string drawableID, string parentDrawableID, TextConf text)
            : base(drawableID, parentDrawableID)
        {
            Text = text;
        }

        /// <summary>
        /// Changes the values of the given text on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="Text"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameEdit.ChangeText(FindChild(Text.ID), Text);
        }
    }
}