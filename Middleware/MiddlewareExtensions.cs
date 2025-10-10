using Microsoft.AspNetCore.Builder;

namespace TAB.Web.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UsePasswordChangeMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PasswordChangeMiddleware>();
        }
    }
} 