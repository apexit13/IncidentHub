namespace IncidentHub.Api.Constants
{
    public static class AuthPolicies
    {
        // The "Values" assigned in Auth0
        public static class Permissions
        {
            public const string ReadIncidents = "read:incidents";
            public const string CreateIncidents = "create:incidents";
            public const string ManageIncidents = "manage:incidents";
            public const string AssignIncidents = "assign:incidents";
            public const string ReadUsers = "read:users";
        }

        // The "Names" used in .RequireAuthorization() and <AuthorizeView>
        public static class Policies
        {
            public const string CanReadIncidents = "CanReadIncidents";
            public const string CanCreateIncidents = "CanCreateIncidents";
            public const string CanManageIncidents = "CanManageIncidents";
            public const string CanAssignIncidents = "CanAssignIncidents";
            public const string CanReadUsers = "CanReadUsers";
        }
    }
}
