namespace Umbraco.Web.Security
{
    public enum PublicAccessStatus
    {
        NotLoggedIn,
        AccessDenied,
        NotApproved,
        LockedOut,
        AccessAccepted
    }
}
