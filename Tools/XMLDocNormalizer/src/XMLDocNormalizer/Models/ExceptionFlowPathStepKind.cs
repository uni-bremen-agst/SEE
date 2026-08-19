namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Identifies the role of one step in an exception-flow path.
    /// </summary>
    internal enum ExceptionFlowPathStepKind
    {
        /// <summary>
        /// The documented member from which exception-flow analysis starts.
        /// </summary>
        RootMember,

        /// <summary>
        /// A method invocation followed during transitive analysis.
        /// </summary>
        MethodCall,

        /// <summary>
        /// A virtual class-method invocation expanded to one known runtime
        /// target.
        /// </summary>
        VirtualMethodCall,

        /// <summary>
        /// An interface-method invocation expanded to one known runtime
        /// implementation.
        /// </summary>
        InterfaceMethodCall,

        /// <summary>
        /// A directly invoked local function.
        /// </summary>
        LocalFunctionCall,

        /// <summary>
        /// An invocation through a delegate whose concrete target was
        /// resolved.
        /// </summary>
        DelegateInvocation,

        /// <summary>
        /// A constructor invocation followed during transitive analysis.
        /// </summary>
        ConstructorCall,

        /// <summary>
        /// A property getter followed during transitive analysis.
        /// </summary>
        PropertyGetter,

        /// <summary>
        /// A property setter followed during transitive analysis.
        /// </summary>
        PropertySetter,

        /// <summary>
        /// A property init accessor followed during transitive analysis.
        /// </summary>
        PropertyInit,

        /// <summary>
        /// An indexer getter followed during transitive analysis.
        /// </summary>
        IndexerGetter,

        /// <summary>
        /// An indexer setter followed during transitive analysis.
        /// </summary>
        IndexerSetter,

        /// <summary>
        /// An indexer init accessor followed during transitive analysis.
        /// </summary>
        IndexerInit,

        /// <summary>
        /// A custom event add accessor followed during transitive analysis.
        /// </summary>
        EventAdd,

        /// <summary>
        /// A custom event remove accessor followed during transitive analysis.
        /// </summary>
        EventRemove,

        /// <summary>
        /// A user-defined unary operator followed during transitive analysis.
        /// </summary>
        UnaryOperatorCall,

        /// <summary>
        /// A user-defined binary operator followed during transitive analysis.
        /// </summary>
        BinaryOperatorCall,

        /// <summary>
        /// A user-defined conversion operator followed during transitive
        /// analysis.
        /// </summary>
        ConversionOperatorCall,

        /// <summary>
        /// An implicit synchronous enumerator acquisition performed by a
        /// <c>foreach</c> statement.
        /// </summary>
        ForEachGetEnumeratorCall,

        /// <summary>
        /// An implicit synchronous enumerator advance performed by a
        /// <c>foreach</c> statement.
        /// </summary>
        ForEachMoveNextCall,

        /// <summary>
        /// An implicit synchronous enumerator current-value access performed
        /// by a <c>foreach</c> statement.
        /// </summary>
        ForEachCurrentGetter,

        /// <summary>
        /// An implicit asynchronous enumerator acquisition performed by an
        /// <c>await foreach</c> statement.
        /// </summary>
        AsyncForEachGetEnumeratorCall,

        /// <summary>
        /// An implicit asynchronous enumerator advance performed by an
        /// <c>await foreach</c> statement.
        /// </summary>
        AsyncForEachMoveNextCall,

        /// <summary>
        /// An implicit asynchronous enumerator current-value access performed
        /// by an <c>await foreach</c> statement.
        /// </summary>
        AsyncForEachCurrentGetter,

        /// <summary>
        /// An implicit call that obtains an awaiter for an awaited value.
        /// </summary>
        AwaitGetAwaiterCall,

        /// <summary>
        /// An implicit access to an awaiter's completion-state getter.
        /// </summary>
        AwaitIsCompletedGetter,

        /// <summary>
        /// An implicit call that retrieves the result of an awaited value.
        /// </summary>
        AwaitGetResultCall,

        /// <summary>
        /// A runtime-provided await helper selected by the compiler.
        /// </summary>
        RuntimeAwaitCall,

        /// <summary>
        /// An implicit synchronous disposal call produced by a using
        /// construct or a synchronous enumeration.
        /// </summary>
        DisposeCall,

        /// <summary>
        /// An implicit asynchronous disposal call produced by an await-using
        /// construct or an asynchronous enumeration.
        /// </summary>
        DisposeAsyncCall,

        /// <summary>
        /// An explicit throw statement or throw expression.
        /// </summary>
        ExplicitThrow,

        /// <summary>
        /// A modeled framework helper that throws an exception.
        /// </summary>
        FrameworkThrowHelper,

        /// <summary>
        /// An exception object supplied through a delegate factory and thrown
        /// by the callee.
        /// </summary>
        DelegateExceptionFactory,

        /// <summary>
        /// An implicit <c>Deconstruct</c> call selected for a deconstruction
        /// assignment or deconstructing foreach variable.
        /// </summary>
        DeconstructCall,

        /// <summary>
        /// An implicit <c>Add</c> call selected for a classic collection
        /// initializer element.
        /// </summary>
        CollectionAddCall,

        /// <summary>
        /// Exception evidence obtained from the XML documentation of an external
        /// callable whose executable source body is unavailable.
        /// </summary>
        ExternalDocumentationEvidence
    }
}
