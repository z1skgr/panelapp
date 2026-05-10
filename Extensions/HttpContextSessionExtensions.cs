namespace panelapp.Extensions
{
    public static class HttpContextSessionExtensions
    {
        public static int? GetCurrentUserId(this HttpContext httpContext)
        {
            return httpContext.Session.GetInt32("UserID");
        }

        public static string GetCurrentRole(this HttpContext httpContext)
        {
            return httpContext.Session.GetString("RoleName") ?? string.Empty;
        }

        public static bool IsAdmin(this HttpContext httpContext)
        {
            return string.Equals(
                httpContext.Session.GetString("RoleName"),
                "Admin",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}