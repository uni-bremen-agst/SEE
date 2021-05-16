using System;

namespace SEE.Net.Dashboard.Model
{
    [Serializable]
    public class DashboardError
    {
        public readonly string dashboardVersionNumber;
        public readonly string type;
        public readonly string message;
        public readonly string localizedMessage;
        public readonly DashboardErrorData data;

        public DashboardError(string dashboardVersionNumber, string type, string message, string localizedMessage, DashboardErrorData data)
        {
            this.dashboardVersionNumber = dashboardVersionNumber;
            this.type = type;
            this.message = message;
            this.localizedMessage = localizedMessage;
            this.data = data;
        }

        [Serializable]
        public class DashboardErrorData
        {
            public readonly string column;
            public readonly string help;
            public readonly bool passwordMayBeUsedAsApiToken;

            public DashboardErrorData(string column, string help, bool passwordMayBeUsedAsApiToken)
            {
                this.column = column;
                this.help = help;
                this.passwordMayBeUsedAsApiToken = passwordMayBeUsedAsApiToken;
            }

            public override string ToString()
            {
                return $"{nameof(column)}: {column}, {nameof(help)}: {help}, "
                       + $"{nameof(passwordMayBeUsedAsApiToken)}: {passwordMayBeUsedAsApiToken}";
            }
        }

        public override string ToString()
        {
            return $"{nameof(dashboardVersionNumber)}: {dashboardVersionNumber}, "
                   + $"{nameof(type)}: {type}, {nameof(message)}: {message}, "
                   + $"{nameof(localizedMessage)}: {localizedMessage}, {nameof(data)}: {data}";
        }
    }
}