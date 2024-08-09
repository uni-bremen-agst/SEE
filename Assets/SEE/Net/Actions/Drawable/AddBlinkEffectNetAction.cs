using SEE.Game.Drawable;
using SEE.GO;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for add the blink effect on an object on a drawable on all clients.
    /// </summary>
    public class AddBlinkEffectNetAction : DrawableNetAction
    {
        /// <summary>
        /// The id of the object that should be get the blink effect
        /// </summary>
        public string ObjectName;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="objectName">The id of the object that should get the blink effect.</param>
        public AddBlinkEffectNetAction(string drawableID, string parentDrawableID, string objectName)
            : base(drawableID, parentDrawableID)
        {
            ObjectName = objectName;
        }

        /// <summary>
        /// Adds a blink effect to the object on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/>
        /// or <see cref="ObjectName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameFinder.FindChild(Surface, ObjectName)?.AddOrGetComponent<BlinkEffect>();
        }
    }
}
