using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for writing <see cref="WriteTextAction"/> a text on the given drawable on all clients.
    /// </summary>
    public class WriteTextNetAction : DrawableNetAction
    {
        /// <summary>
        /// The text that should be written as <see cref="Text"/> object.
        /// </summary>
        public TextConf Text;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the text should be written.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="text">The text that should be written.</param>
        public WriteTextNetAction(string drawableID, string parentDrawableID, TextConf text)
            : base (drawableID, parentDrawableID)
        {
            Text = text;
        }

        /// <summary>
        /// Writes the text on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="Text"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (Text != null && Text.ID != "")
            {
                GameTexter.ReWriteText(Surface, Text);
            }
            else
            {
                throw new System.Exception($"There is no text to write.");
            }
        }
    }
}