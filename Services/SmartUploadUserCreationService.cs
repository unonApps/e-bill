using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    /// <summary>
    /// Data transfer object for extracted user info from PSTN/PW files
    /// </summary>
    public class ExtractedUserInfo
    {
        public string Extension { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? IndexNumber { get; set; } // Extracted from "Staff ID X: {IndexNumber}" in header
    }

    /// <summary>
    /// Information about a user that was auto-created
    /// </summary>
    public class CreatedUserInfo
    {
        public string Extension { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IndexNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of the auto-creation process
    /// </summary>
    public class UserCreationResult
    {
        public int UsersCreated { get; set; }
        public int PhonesCreated { get; set; }
        public int Skipped { get; set; }
        public List<CreatedUserInfo> CreatedUsers { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Configuration for SmartUpload auto-creation feature
    /// </summary>
    public class SmartUploadSettings
    {
        public bool AutoCreateUsers { get; set; } = true;
        public string EmailDomain { get; set; } = "un.org";
        public string IndexPrefix { get; set; } = "PSTN-";
    }

    public interface ISmartUploadUserCreationService
    {
        /// <summary>
        /// Auto-creates EbillUsers and UserPhones for any extensions found in the file
        /// that don't already exist in the system.
        /// </summary>
        /// <param name="extractedUsers">Dictionary of extension -> ExtractedUserInfo</param>
        /// <param name="importJobId">The import job ID for logging</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Result containing created users and any errors</returns>
        Task<UserCreationResult> AutoCreateMissingUsersAsync(
            Dictionary<string, ExtractedUserInfo> extractedUsers,
            Guid importJobId,
            CancellationToken ct);

        /// <summary>
        /// Gets the SmartUpload settings from configuration
        /// </summary>
        SmartUploadSettings GetSettings();
    }

    public class SmartUploadUserCreationService : ISmartUploadUserCreationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SmartUploadUserCreationService> _logger;
        private readonly SmartUploadSettings _settings;

        public SmartUploadUserCreationService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<SmartUploadUserCreationService> logger)
        {
            _context = context;
            _logger = logger;

            // Load settings from configuration
            _settings = new SmartUploadSettings();
            configuration.GetSection("SmartUpload").Bind(_settings);
        }

        public SmartUploadSettings GetSettings() => _settings;

        public async Task<UserCreationResult> AutoCreateMissingUsersAsync(
            Dictionary<string, ExtractedUserInfo> extractedUsers,
            Guid importJobId,
            CancellationToken ct)
        {
            var result = new UserCreationResult();

            if (!_settings.AutoCreateUsers)
            {
                _logger.LogInformation("Auto-create users is disabled in settings. Skipping user creation.");
                return result;
            }

            if (extractedUsers == null || extractedUsers.Count == 0)
            {
                _logger.LogInformation("No extracted users provided. Skipping user creation.");
                return result;
            }

            _logger.LogInformation(
                "Starting auto-creation of missing users for import job {JobId}. Found {Count} unique extensions.",
                importJobId, extractedUsers.Count);

            // Get all existing phone numbers to check against
            var existingPhoneNumbers = await _context.UserPhones
                .Where(up => up.IsActive)
                .Select(up => up.PhoneNumber)
                .ToListAsync(ct);

            var existingPhoneSet = new HashSet<string>(existingPhoneNumbers, StringComparer.OrdinalIgnoreCase);

            // Get all existing index numbers to avoid duplicates
            var existingIndexNumbers = await _context.EbillUsers
                .Select(u => u.IndexNumber)
                .ToListAsync(ct);

            var existingIndexSet = new HashSet<string>(existingIndexNumbers, StringComparer.OrdinalIgnoreCase);

            // Get all existing emails to avoid duplicates
            var existingEmails = await _context.EbillUsers
                .Select(u => u.Email)
                .ToListAsync(ct);

            var existingEmailSet = new HashSet<string>(existingEmails, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in extractedUsers)
            {
                var extension = kvp.Key;
                var userInfo = kvp.Value;

                try
                {
                    // Skip if extension already exists in UserPhones
                    if (existingPhoneSet.Contains(extension))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // Use extracted index number if available, otherwise generate one
                    var indexNumber = !string.IsNullOrWhiteSpace(userInfo.IndexNumber)
                        ? userInfo.IndexNumber
                        : $"{_settings.IndexPrefix}{extension}";
                    var email = $"{indexNumber}@{_settings.EmailDomain}";

                    // Skip if index number already exists
                    if (existingIndexSet.Contains(indexNumber))
                    {
                        _logger.LogWarning("Index number {IndexNumber} already exists. Skipping user for extension {Extension}.",
                            indexNumber, extension);
                        result.Skipped++;
                        continue;
                    }

                    // Skip if email already exists
                    if (existingEmailSet.Contains(email))
                    {
                        _logger.LogWarning("Email {Email} already exists. Skipping user for extension {Extension}.",
                            email, extension);
                        result.Skipped++;
                        continue;
                    }

                    // Parse name into first/last
                    var (firstName, lastName) = ParseName(userInfo.EmployeeName);

                    // Create new EbillUser
                    var newUser = new EbillUser
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        IndexNumber = indexNumber,
                        Email = email,
                        OfficialMobileNumber = extension,
                        IsActive = true, // Auto-created users are active by default
                        CreatedDate = DateTime.UtcNow,
                        Location = "Auto-created from PSTN/PW import",
                        IsAutoCreated = true,
                        AutoCreatedFromImportJobId = importJobId
                    };

                    _context.EbillUsers.Add(newUser);
                    await _context.SaveChangesAsync(ct);

                    _logger.LogInformation(
                        "Created EbillUser: {Name} ({IndexNumber}, {Email}) for extension {Extension}",
                        newUser.FullName, indexNumber, email, extension);

                    result.UsersCreated++;

                    // Create UserPhone linking extension to user
                    var userPhone = new UserPhone
                    {
                        IndexNumber = indexNumber,
                        PhoneNumber = extension,
                        PhoneType = PhoneTypes.Extension, // Fixed line for PSTN
                        IsPrimary = true,
                        IsActive = true,
                        Status = PhoneStatus.Active,
                        LineType = LineType.Primary,
                        OwnershipType = PhoneOwnershipType.Personal,
                        AssignedDate = DateTime.UtcNow,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "SmartUpload",
                        Notes = "Auto-created from PSTN/PW import"
                    };

                    _context.UserPhones.Add(userPhone);
                    await _context.SaveChangesAsync(ct);

                    _logger.LogInformation(
                        "Created UserPhone linking extension {Extension} to user {IndexNumber}",
                        extension, indexNumber);

                    result.PhonesCreated++;

                    // Add to existing sets to prevent duplicates within this batch
                    existingPhoneSet.Add(extension);
                    existingIndexSet.Add(indexNumber);
                    existingEmailSet.Add(email);

                    // Track created user for notification
                    result.CreatedUsers.Add(new CreatedUserInfo
                    {
                        Extension = extension,
                        Name = newUser.FullName,
                        IndexNumber = indexNumber,
                        Email = email
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error creating user for extension {Extension}: {Error}",
                        extension, ex.Message);

                    result.Errors.Add($"Extension {extension}: {ex.Message}");
                }
            }

            _logger.LogInformation(
                "Auto-creation completed for import job {JobId}. Created: {UsersCreated} users, {PhonesCreated} phones. Skipped: {Skipped}. Errors: {ErrorCount}",
                importJobId, result.UsersCreated, result.PhonesCreated, result.Skipped, result.Errors.Count);

            return result;
        }

        /// <summary>
        /// Parses Staff name into first and last name.
        /// Single word -> LastName only (FirstName = "Unknown")
        /// Multiple words -> First word is FirstName, rest is LastName
        /// </summary>
        private (string FirstName, string LastName) ParseName(string employeeName)
        {
            if (string.IsNullOrWhiteSpace(employeeName))
            {
                return ("Unknown", "Unknown");
            }

            var nameParts = employeeName.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 0)
            {
                return ("Unknown", "Unknown");
            }

            if (nameParts.Length == 1)
            {
                // Single name - treat as last name
                return ("Unknown", nameParts[0]);
            }

            // Multiple parts - first part is first name, rest is last name
            var firstName = nameParts[0];
            var lastName = string.Join(" ", nameParts.Skip(1));

            return (firstName, lastName);
        }
    }
}
