using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface IEmailTemplateService
    {
        /// <summary>
        /// Renders an email template with provided data
        /// </summary>
        /// <param name="templateCode">Unique template code</param>
        /// <param name="data">Dictionary of placeholder values</param>
        /// <returns>Rendered subject and body</returns>
        Task<(string subject, string htmlBody, string plainTextBody)> RenderTemplateAsync(
            string templateCode,
            Dictionary<string, string> data);

        /// <summary>
        /// Gets a template by its code
        /// </summary>
        Task<EmailTemplate?> GetTemplateByCodeAsync(string templateCode);

        /// <summary>
        /// Gets all active templates
        /// </summary>
        Task<List<EmailTemplate>> GetActiveTemplatesAsync();

        /// <summary>
        /// Gets templates by category
        /// </summary>
        Task<List<EmailTemplate>> GetTemplatesByCategoryAsync(string category);

        /// <summary>
        /// Creates a new template
        /// </summary>
        Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template);

        /// <summary>
        /// Updates an existing template
        /// </summary>
        Task<EmailTemplate> UpdateTemplateAsync(EmailTemplate template);

        /// <summary>
        /// Deletes a template (only non-system templates)
        /// </summary>
        Task<bool> DeleteTemplateAsync(int templateId);

        /// <summary>
        /// Preview template with sample data
        /// </summary>
        Task<(string subject, string htmlBody, string plainTextBody)> PreviewTemplateAsync(
            string templateCode,
            Dictionary<string, string> sampleData);
    }
}
