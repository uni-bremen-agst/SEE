using System;
using SEE.Net.Dashboard.Model;

namespace SEE.Net.Dashboard
{
    /// <summary>
    /// Represents an error which occurred when accessing the dashboard API.
    /// Additional information about the error can be retrieved from the <see cref="Error"/> object.
    /// </summary>
    public class DashboardException: Exception
    {
        /// <summary>
        /// Contains additional information about the error which occurred.
        /// </summary>
        public DashboardError Error { get; }

        /// <summary>
        /// Instantiates a new <see cref="DashboardException"/> with the given <paramref name="error"/>.
        /// </summary>
        /// <param name="error">The error which occurred in the dashboard.</param>
        public DashboardException(DashboardError error) : this($"{error.type}: {error.localizedMessage}")
        {
            Error = error;
        }

        /// <summary>
        /// Instantiates a new <see cref="DashboardException"/> with the given <see cref="inner"/> exception.
        /// </summary>
        /// <param name="inner">Exception which occurred when accessing the dashboard API.</param>
        public DashboardException(Exception inner) : this("An error occurred while retrieving dashboard data.", inner)
        {
            // Intentionally empty, the other constructor that's being called is already doing all the work.
        }
        
        #region Usual Constructors
        
        private DashboardException(string message): base(message)
        {
            // Intentionally empty. Base call defines behavior.
        }

        private DashboardException(string message, Exception inner) : base(message, inner)
        {
            // Intentionally empty. Base call defines behavior.
        }
        
        #endregion
    }
}