using System;
using SEE.Controls;

namespace SEE.Net
{

	public class EditAnnotationAction : AbstractAction
	{

        public uint id;
        public string annotation;

        public EditAnnotationAction(AnnotatableObject annotatableObject, String annotation) : base(false)
        {
            id = annotatableObject ? annotatableObject.id : uint.MaxValue;
            this.annotation = annotation;
        }

        protected override bool ExecuteOnServer()
        {
            //@todo
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            AnnotatableObject annotatableObject = (AnnotatableObject)InteractableObject.Get(id);
            annotatableObject.EditAnnotation(annotation);
            return true;
        }

        protected override bool UndoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool UndoOnClient()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override bool RedoOnClient()
        {
            throw new System.NotImplementedException();
        }
    }
}
