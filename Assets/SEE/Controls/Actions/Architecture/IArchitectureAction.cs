namespace SEE.Controls.Actions.Architecture
{
    /// <summary>
    /// Interface for actions that can be enabled within the <see cref="ArchitectureAction"/>
    /// </summary>
    public interface IArchitectureAction
    {
        public void Update();
        public void Awake();
        public void Start();
        public void Stop();
        public ArchitectureActionType GetActionType();
    }
}