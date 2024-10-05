using SEE.Utils.Paths;
using SimpleFileBrowser;
using UnityEngine.Events;

namespace SEE.UI.FilePicker
{
    /// <summary>
    /// Allows a user to pick a file or folder for a <see cref="DataPath"/>.
    /// </summary>
    public partial class DataPathPicker : PlatformDependentComponent
    {
        /// <summary>
        /// The label.
        /// </summary>
        private string label;

        /// <summary>
        /// The label.
        /// </summary>
        public string Label
        {
            get => label;
            set
            {
                label = value;
                OnLabelChanged?.Invoke();
            }
        }

        /// <summary>
        /// The data path.
        /// </summary>
        private DataPath dataPathInstance;

        /// <summary>
        /// The data path.
        /// </summary>
        public DataPath DataPathInstance
        {
            get => dataPathInstance;
            set
            {
                dataPathInstance = value;
                OnDataPathChanged?.Invoke();
            }
        }

        /// <summary>
        /// The pick mode.
        /// </summary>
        private FileBrowser.PickMode pickingMode = FileBrowser.PickMode.Files;

        /// <summary>
        /// The pick mode.
        /// </summary>
        public FileBrowser.PickMode PickingMode
        {
            get => pickingMode;
            set
            {
                pickingMode = value;
                OnPickModeChanged?.Invoke();
            }
        }

        /// <summary>
        /// Triggers when <see cref="Label"/> was changed.
        /// </summary>
        public event UnityAction OnLabelChanged;

        /// <summary>
        /// Triggers when <see cref="DataPathInstance"/> was changed.
        /// </summary>
        public event UnityAction OnDataPathChanged;

        /// <summary>
        /// Triggers when <see cref="PickingMode"/> was changed.
        /// </summary>
        public event UnityAction OnPickModeChanged;
    }
}
