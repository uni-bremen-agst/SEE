using System;
using SEE.Net.Dashboard.Model;

namespace SEE.Net.Dashboard
{
    public class DashboardException: Exception
    {
        
        public DashboardError Error { get; }

        public DashboardException(DashboardError error) : this($"{error.type}: {error.localizedMessage}")
        {
            Error = error;
        }

        public DashboardException(Exception inner) : this("An error occurred while retrieving dashboard data.", inner)
        {
            // Intentionally empty, the other constructor that's being called is already doing all the work.
        }
        
        #region Usual Constructors
        
        private DashboardException()
        {
            // Intentionally empty, exception should not be created without information
        }

        public DashboardException(string message): base(message)
        {
            
        }

        public DashboardException(string message, Exception inner) : base(message, inner)
        {
            
        }
        
        #endregion
        
    }
}