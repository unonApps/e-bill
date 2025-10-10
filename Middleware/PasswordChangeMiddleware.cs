using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TAB.Web.Models;
using Microsoft.Extensions.Logging;

namespace TAB.Web.Middleware
{
    public class PasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        public PasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Check if the user is authenticated and on a page other than ChangePassword
                if (context.User.Identity?.IsAuthenticated == true && 
                    !context.Request.Path.StartsWithSegments("/Account/ChangePassword") &&
                    !context.Request.Path.StartsWithSegments("/Account/Logout") &&
                    !(context.Request.Path.Value?.EndsWith(".css") == true) &&
                    !(context.Request.Path.Value?.EndsWith(".js") == true) &&
                    !(context.Request.Path.Value?.EndsWith(".ico") == true) &&
                    !(context.Request.Path.Value?.EndsWith(".png") == true) &&
                    !context.Request.Path.StartsWithSegments("/lib"))
                {
                    // Get the user manager from the request services
                    var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    
                    try
                    {
                        // Get the current user
                        var user = await userManager.GetUserAsync(context.User);
                        
                        // If the user requires a password change, redirect to the ChangePassword page
                        if (user != null && user.RequirePasswordChange)
                        {
                            context.Response.Redirect("/Account/ChangePassword");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue processing the request
                        var logger = context.RequestServices.GetRequiredService<ILogger<PasswordChangeMiddleware>>();
                        logger.LogError(ex, "Error checking if user requires password change");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing the request
                var logger = context.RequestServices.GetRequiredService<ILogger<PasswordChangeMiddleware>>();
                logger.LogError(ex, "Error in password change middleware");
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
} 