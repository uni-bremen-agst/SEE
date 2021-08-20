using SEE.Game.UI.PropertyDialog;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.Architecture
{
    /// <summary>
    /// A dialog to enter the name of the architecture.
    /// </summary>
    public class SaveArchitectureDialog
    {
        
        /// <summary>
        /// Event triggered when the user presses the OK button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public readonly UnityEvent<string> OnConfirm = new UnityEvent<string>();
        /// <summary>
        /// Event triggered when the user presses the Cancel button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public readonly UnityEvent OnCancel = new UnityEvent();
        
        /// <summary>
        /// The dialog to save the architecture to disk.
        /// </summary>
        private GameObject dialog;
    
        /// <summary>
        /// The dialog property for the architecture name.
        /// </summary>
        private StringProperty architectureName;

        
        /// <summary>
        /// Creates and opens the dialog.
        /// </summary>
        public void Open()
        {
            
            
            dialog = new GameObject("Save Architecture");
            //Architecture Name property
            architectureName = dialog.AddComponent<StringProperty>();
            architectureName.Name = "Architecture name";
            architectureName.Value = "<Name>";
            architectureName.Description = "Name of the architecture";
            
            //Property group
            PropertyGroup propertyGroup = dialog.AddComponent<PropertyGroup>();
            propertyGroup.name = "File name";
            propertyGroup.AddProperty(architectureName);
            
            // Dialog
            PropertyDialog.PropertyDialog propertyDialog= dialog.AddComponent<PropertyDialog.PropertyDialog>();
            propertyDialog.Title = "Save Architecture";
            propertyDialog.Description = "Enter the name of the architecture";
            propertyDialog.AddGroup(propertyGroup);
            
            //Listeners
            propertyDialog.OnCancel.AddListener(CancelButtonPressed);
            propertyDialog.OnConfirm.AddListener(OKButtonPressed);

            propertyDialog.DialogShouldBeShown = true;
        }

        
        /// <summary>
        /// Notifies all listeners on <see cref="OnCancel"/> and  closes the dialog.
        /// </summary>
        private void CancelButtonPressed()
        {
            OnCancel.Invoke();
            Close();
        }

        
        /// <summary>
        /// Sets the attributes of <see cref="node"/> to the trimmed values entered in the dialog, notifies all listeners on <see cref="OnConfirm"/>, and closes the dialog.
        /// </summary>
        private void OKButtonPressed()
        {
            string value = architectureName.Value.Trim();
            OnConfirm.Invoke(value);
            Close();
        }
        
        /// <summary>
        /// Destroys <see cref="dialog"/>. <see cref="dialog"/> will be null afterwards.
        /// </summary>
        private void Close()
        {
            Object.Destroy(dialog);
            dialog = null;
        }
    }
}