using System.Threading.Tasks;

namespace TAB.Web.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email with the specified parameters.
        /// </summary>
        /// <param name="to">Email address of the recipient</param>
        /// <param name="subject">Email subject</param>
        /// <param name="htmlMessage">Email message in HTML format</param>
        /// <param name="plainTextMessage">Email message in plain text format (optional)</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SendEmailAsync(string to, string subject, string htmlMessage, string? plainTextMessage = null);
        
        /// <summary>
        /// Send a welcome email to a newly created user with their initial password.
        /// </summary>
        /// <param name="to">Email address of the recipient</param>
        /// <param name="fullName">Full name of the user</param>
        /// <param name="initialPassword">The initial password assigned to the user</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SendWelcomeEmailAsync(string to, string fullName, string initialPassword);
    }
} 