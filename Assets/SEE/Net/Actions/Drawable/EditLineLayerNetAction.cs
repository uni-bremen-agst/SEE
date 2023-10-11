using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using SEE.Controls.Actions.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the order in layer (<see cref="EditAction"/>) of a drawable type on all clients.
    /// </summary>
    public class EditLayerNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the object is located
        /// </summary>
        public string DrawableID;
        /// <summary>
        /// The id of the drawable parent
        /// </summary>
        public string ParentDrawableID;
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
        public EditLayerNetAction(string drawableID, string parentDrawableID, string typeName, int oderInLayer) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            TypeName = typeName;
            this.OrderInLayer = oderInLayer;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the order in layer of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (!IsRequester())
                {
                    GameObject drawable = GameDrawableFinder.Find(DrawableID, ParentDrawableID);
                    if (drawable != null && GameDrawableFinder.FindChild(drawable, TypeName) != null)
                    {
                        GameEdit.ChangeLayer(GameDrawableFinder.FindChild(drawable, TypeName), OrderInLayer);
                    }
                    else
                    {
                        throw new System.Exception($"There is no drawable with the ID {DrawableID} or drawable type with the ID {TypeName}.");
                    }
                }
            }
        }
    }
}