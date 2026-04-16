namespace IncidentHub.Api.Constants
{
    public static class AuthPolicies
    {
        // The "Values" assigned in Auth0
        public static class Permissions
        {
            public const string ReadIncidents = "read:incidents";
            public const string ManageIncidents = "manage:incidents";
        }

        // The "Names" used in .RequireAuthorization() and <AuthorizeView>
        public static class Policies
        {
            public const string CanReadIncidents = "CanReadIncidents";
            public const string CanManageIncidents = "CanManageIncidents";
        }
    }
}
