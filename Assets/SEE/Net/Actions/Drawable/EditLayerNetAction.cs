using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the order in layer (<see cref="EditAction"/>)
    /// of a <see cref="DrawableType"/> object on all clients.
    /// </summary>
    public class EditLayerNetAction : DrawableNetAction
    {
        /// <summary>
        /// The id of the drawable type that should be changed
        /// </summary>
        public string TypeName;
        /// <summary>
        /// The new order in layer for the drawable type.
        /// </summary>
        public int OrderInLayer;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="typeName">The id of the drawable type that should be changed.</param>
        /// <param name="orderInLayer">The new order in layer for the drawable type.</param>
        public EditLayerNetAction(string drawableID, string parentDrawableID, string typeName, int orderInLayer)
            : base(drawableID, parentDrawableID)
        {
            TypeName = typeName;
            OrderInLayer = orderInLayer;
        }

        /// <summary>
        /// Changes the order in layer of the given object on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/>
        /// or <see cref="TypeName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (TryFindChild(TypeName, out GameObject typeName))
            {
                GameEdit.ChangeLayer(typeName, OrderInLayer);
            }
            else
            {
                GameStickyNoteManager.ChangeLayer(Surface.GetRootParent(), OrderInLayer);
            }
        }
    }
}