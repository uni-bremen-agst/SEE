namespace SEE.Utils
{
    /// <summary>
    /// Creates a new instance of <see cref="ReversibleAction"/>.
    /// </summary>
    /// <returns>new instance of <see cref="ReversibleAction"/>/returns>
    public delegate ReversibleAction CreateReversibleAction();

    /// <summary>
    /// Defines the expected operations and their protocol for actions that
    /// can be reversed (have Undo() and Redo(). Their protocol resembles 
    /// Unity's protocol for MonoBehaviours regarding <see cref="Awake"/>,
    /// <see cref="Start"/>, and <see cref="Update"/>.
    /// </summary>
    public interface ReversibleAction
    {
        // The protocol of the following operations over the lifecycle of a
        // reversible object is as follows (where "X . Y" means X is called
        // before Y and * denotes the Kleene star, i.e., refers to zero or
        // more repetitions.        
        //    Awake . (Start . Update* . Stop)*
        // Undo can be called only after Awake has been called before.
        // Let calls(X) denote the number of calls for method X, then
        //    calls(Redo) = calls(Undo) or calls(Redo) = calls(Undo) - 1
        // holds as an invariant.

        /// <summary>
        /// Will be called exactly once before <see cref="Start"/> and any other method
        /// in this interface. Here code can be executed for intialization.
        /// </summary>
        void Awake();
        /// <summary>
        /// Will be called whenever the action is to start its execution.
        /// There two different situations in which this is the case:
        /// (1) upon its intial start and (2) when it is resumed.
        /// Situation (1): <see cref="Start"/> will be called once after 
        /// <see cref="Awake"/> when it is executed the first time. 
        /// Situation (2): Then it will be called again each time it gets 
        /// re-activated because a previously executed action having superseding 
        /// it gets undone.
        /// Invariant: The first call to <see cref="Start"/> will take place
        /// before <see cref="Update"/> and after <see cref="Awake"/>.
        /// </summary>
        void Start();
        /// <summary>
        /// Will be called after <see cref="Start"/>. Can be called multiple times
        /// (e.g., once per frame) for continuously executing the action.
        /// </summary>
        void Update();
        /// <summary>
        /// Will be called when the action is to stop executing. This is 
        /// generally the case when another action is superseding it.
        /// Invariant: A <see cref="Start"/> message has been received before.
        /// </summary>
        void Stop();

        /// <summary>
        /// Called when the effect of the action is to be reversed.
        /// </summary>
        void Undo();
        /// <summary>
        /// Called when the action was previously reversed by <see cref="Undo"/>
        /// to re-establish the effect that was undone.
        /// Precondition: <see cref="Undo"/> was called before.
        /// </summary>
        void Redo();
    }
}
