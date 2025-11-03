using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(
            ApplicationDbContext context,
            ILogger<EmailTemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(string subject, string htmlBody, string plainTextBody)> RenderTemplateAsync(
            string templateCode,
            Dictionary<string, string> data)
        {
            var template = await GetTemplateByCodeAsync(templateCode);
            if (template == null)
            {
                throw new InvalidOperationException($"Template with code '{templateCode}' not found");
            }

            if (!template.IsActive)
            {
                throw new InvalidOperationException($"Template with code '{templateCode}' is not active");
            }

            // Render subject
            var subject = ReplacePlaceholders(template.Subject, data);

            // Render HTML body
            var htmlBody = ReplacePlaceholders(template.HtmlBody, data);

            // Render plain text body
            var plainTextBody = string.IsNullOrEmpty(template.PlainTextBody)
                ? StripHtml(htmlBody)
                : ReplacePlaceholders(template.PlainTextBody, data);

            return (subject, htmlBody, plainTextBody);
        }

        public async Task<EmailTemplate?> GetTemplateByCodeAsync(string templateCode)
        {
            return await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateCode == templateCode);
        }

        public async Task<List<EmailTemplate>> GetActiveTemplatesAsync()
        {
            return await _context.EmailTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<EmailTemplate>> GetTemplatesByCategoryAsync(string category)
        {
            return await _context.EmailTemplates
                .Where(t => t.Category == category && t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template)
        {
            // Check if template code already exists
            var exists = await _context.EmailTemplates
                .AnyAsync(t => t.TemplateCode == template.TemplateCode);

            if (exists)
            {
                throw new InvalidOperationException($"Template with code '{template.TemplateCode}' already exists");
            }

            template.CreatedDate = DateTime.UtcNow;
            _context.EmailTemplates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created email template: {TemplateCode}", template.TemplateCode);
            return template;
        }

        public async Task<EmailTemplate> UpdateTemplateAsync(EmailTemplate template)
        {
            var existing = await _context.EmailTemplates.FindAsync(template.Id);
            if (existing == null)
            {
                throw new InvalidOperationException($"Template with ID {template.Id} not found");
            }

            if (existing.IsSystemTemplate)
            {
                _logger.LogWarning("Attempted to update system template: {TemplateCode}", existing.TemplateCode);
                throw new InvalidOperationException("System templates cannot be modified");
            }

            existing.Name = template.Name;
            existing.Subject = template.Subject;
            existing.HtmlBody = template.HtmlBody;
            existing.PlainTextBody = template.PlainTextBody;
            existing.Description = template.Description;
            existing.AvailablePlaceholders = template.AvailablePlaceholders;
            existing.Category = template.Category;
            existing.IsActive = template.IsActive;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = template.ModifiedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated email template: {TemplateCode}", existing.TemplateCode);
            return existing;
        }

        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            var template = await _context.EmailTemplates.FindAsync(templateId);
            if (template == null)
            {
                return false;
            }

            if (template.IsSystemTemplate)
            {
                _logger.LogWarning("Attempted to delete system template: {TemplateCode}", template.TemplateCode);
                throw new InvalidOperationException("System templates cannot be deleted");
            }

            _context.EmailTemplates.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted email template: {TemplateCode}", template.TemplateCode);
            return true;
        }

        public async Task<(string subject, string htmlBody, string plainTextBody)> PreviewTemplateAsync(
            string templateCode,
            Dictionary<string, string> sampleData)
        {
            return await RenderTemplateAsync(templateCode, sampleData);
        }

        /// <summary>
        /// Replaces placeholders in the format {{PlaceholderName}} with actual values
        /// </summary>
        private string ReplacePlaceholders(string template, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(template))
            {
                return template;
            }

            var result = template;

            // Replace placeholders in the format {{PlaceholderName}}
            foreach (var kvp in data)
            {
                var placeholder = $"{{{{{kvp.Key}}}}}";
                result = result.Replace(placeholder, kvp.Value ?? string.Empty);
            }

            // Check for unreplaced placeholders and log warning
            var unreplacedMatches = Regex.Matches(result, @"\{\{([^}]+)\}\}");
            if (unreplacedMatches.Count > 0)
            {
                var unreplaced = string.Join(", ", unreplacedMatches.Cast<Match>().Select(m => m.Value));
                _logger.LogWarning("Template has unreplaced placeholders: {Placeholders}", unreplaced);
            }

            return result;
        }

        /// <summary>
        /// Strips HTML tags from a string to create plain text version
        /// </summary>
        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            var text = html;

            // Replace common block-level elements with line breaks
            text = Regex.Replace(text, @"</?(br|p|div|h[1-6]|tr|li)[^>]*>", "\n", RegexOptions.IgnoreCase);

            // Replace table cells with spacing
            text = Regex.Replace(text, @"</(td|th)[^>]*>", " | ", RegexOptions.IgnoreCase);

            // Remove remaining HTML tags
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);

            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);

            // Normalize multiple spaces to single space (but preserve line breaks)
            text = Regex.Replace(text, @"[ \t]+", " ");

            // Remove excessive blank lines (more than 2 consecutive newlines)
            text = Regex.Replace(text, @"\n\s*\n\s*\n+", "\n\n");

            // Trim each line
            var lines = text.Split('\n');
            text = string.Join("\n", lines.Select(line => line.Trim()));

            return text.Trim();
        }
    }
}
