namespace IncidentHub.Api.Constants
{
    public class AuthClaimTypes
    {
        // Standard key in Auth0 Access Tokens (used by API)
        public const string Permissions = "permissions";

        // Custom keys in ID Token (used by React)
        public const string PermissionsNamespace = "https://incidenthub.example.com/permissions";
        public const string RolesNamespace = "https://incidenthub.example.com/roles";
    }
}
