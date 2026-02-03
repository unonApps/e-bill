using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface IUserPhoneService
    {
        Task<List<UserPhone>> GetUserPhonesAsync(string indexNumber);
        Task<UserPhone?> GetPhoneAsync(int id);
        Task<bool> AssignPhoneAsync(string indexNumber, string phoneNumber, string phoneType, bool isPrimary, string? location = null, string? notes = null, int? classOfServiceId = null, bool forceReassign = false, PhoneStatus status = PhoneStatus.Active, LineType lineType = LineType.Secondary, PhoneOwnershipType ownershipType = PhoneOwnershipType.Personal, string? purpose = null, string? reassignedFromIndex = null, string? reassignedFromName = null, int? oldPhoneIdForHistory = null);
        Task<bool> UnassignPhoneAsync(int phoneId);
        Task<bool> SetPrimaryPhoneAsync(int phoneId);
        Task<string?> GetUserByPhoneAsync(string phoneNumber, DateTime? billDate = null);
        Task<Dictionary<string, List<UserPhone>>> GetAllUserPhonesAsync();
        Task<bool> IsPhoneAvailableAsync(string phoneNumber, string? excludeIndexNumber = null);
        Task<(bool success, string? existingUserIndex, string? existingUserName)> CheckPhoneAssignmentAsync(string phoneNumber, string currentUserIndex);
    }

    public class UserPhoneService : IUserPhoneService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserPhoneService> _logger;
        private readonly IUserPhoneHistoryService _historyService;
        private readonly IEnhancedEmailService _enhancedEmailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserPhoneService(
            ApplicationDbContext context,
            ILogger<UserPhoneService> logger,
            IUserPhoneHistoryService historyService,
            IEnhancedEmailService enhancedEmailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _historyService = historyService;
            _enhancedEmailService = enhancedEmailService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<UserPhone>> GetUserPhonesAsync(string indexNumber)
        {
            return await _context.UserPhones
                .Include(up => up.ClassOfService)
                .Where(up => up.IndexNumber == indexNumber && up.IsActive)
                .OrderByDescending(up => up.IsPrimary)
                .ThenBy(up => up.PhoneType)
                .ToListAsync();
        }

        public async Task<UserPhone?> GetPhoneAsync(int id)
        {
            return await _context.UserPhones
                .Include(up => up.EbillUser)
                .FirstOrDefaultAsync(up => up.Id == id);
        }

        public async Task<(bool success, string? existingUserIndex, string? existingUserName)> CheckPhoneAssignmentAsync(string phoneNumber, string currentUserIndex)
        {
            var existingAssignment = await _context.UserPhones
                .Include(up => up.EbillUser)
                .FirstOrDefaultAsync(up => up.PhoneNumber == phoneNumber &&
                                          up.IsActive &&
                                          up.IndexNumber != currentUserIndex);

            if (existingAssignment != null)
            {
                var fullName = existingAssignment.EbillUser != null
                    ? $"{existingAssignment.EbillUser.FirstName} {existingAssignment.EbillUser.LastName}"
                    : "Unknown User";
                return (false, existingAssignment.IndexNumber, fullName);
            }

            return (true, null, null);
        }

        public async Task<bool> AssignPhoneAsync(string indexNumber, string phoneNumber, string phoneType, bool isPrimary, string? location = null, string? notes = null, int? classOfServiceId = null, bool forceReassign = false, PhoneStatus status = PhoneStatus.Active, LineType lineType = LineType.Secondary, PhoneOwnershipType ownershipType = PhoneOwnershipType.Personal, string? purpose = null, string? reassignedFromIndex = null, string? reassignedFromName = null, int? oldPhoneIdForHistory = null)
        {
            try
            {
                // Sync LineType with IsPrimary - they must match
                if (lineType == LineType.Primary)
                {
                    isPrimary = true;
                }
                else
                {
                    // If LineType is not Primary, IsPrimary must be false
                    isPrimary = false;
                }

                // Check if user exists
                var user = await _context.EbillUsers
                    .Include(u => u.ApplicationUser)
                    .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                if (user == null)
                {
                    _logger.LogWarning($"User with IndexNumber {indexNumber} not found");
                    return false;
                }

                // Check if phone is already assigned to another user
                var existingAssignment = await _context.UserPhones
                    .Include(up => up.EbillUser)
                        .ThenInclude(u => u.ApplicationUser)
                    .FirstOrDefaultAsync(up => up.PhoneNumber == phoneNumber &&
                                              up.IsActive &&
                                              up.IndexNumber != indexNumber);

                // Track if this is a reassignment for history purposes
                // Can be set from existingAssignment OR passed from caller (when phone was pre-unassigned)
                var isReassignment = !string.IsNullOrEmpty(reassignedFromIndex);
                string? previousUserIndex = reassignedFromIndex;
                string? previousUserName = reassignedFromName;
                int? oldPhoneId = oldPhoneIdForHistory; // Track old phone ID for history copy

                if (existingAssignment != null)
                {
                    if (!forceReassign)
                    {
                        _logger.LogWarning($"Phone {phoneNumber} is already assigned to user {existingAssignment.IndexNumber}");
                        return false;
                    }

                    // Mark this as a reassignment for history tracking
                    isReassignment = true;
                    previousUserIndex = existingAssignment.IndexNumber;
                    previousUserName = existingAssignment.EbillUser != null
                        ? $"{existingAssignment.EbillUser.FirstName} {existingAssignment.EbillUser.LastName}"
                        : existingAssignment.IndexNumber;
                    oldPhoneId = existingAssignment.Id; // Store old phone ID for history copy

                    // Unassign from previous user
                    _logger.LogInformation($"Reassigning phone {phoneNumber} from user {existingAssignment.IndexNumber} to user {indexNumber}");
                    existingAssignment.IsActive = false;
                    existingAssignment.UnassignedDate = DateTime.UtcNow;

                    // Store previous user info for email notification
                    var previousUserForEmail = existingAssignment.EbillUser;

                    // If it was primary for the previous user, update their primary phone
                    if (existingAssignment.IsPrimary)
                    {
                        var previousUser = await _context.EbillUsers
                            .FirstOrDefaultAsync(u => u.IndexNumber == existingAssignment.IndexNumber);
                        if (previousUser != null)
                        {
                            previousUser.OfficialMobileNumber = null;
                            _logger.LogInformation($"Removed primary phone from user {existingAssignment.IndexNumber}");
                        }
                    }

                    // Send unassignment email to previous user
                    if (previousUserForEmail != null)
                    {
                        var newUserInfo = await _context.EbillUsers
                            .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                        var newUserName = newUserInfo != null
                            ? $"{newUserInfo.FirstName} {newUserInfo.LastName}"
                            : indexNumber;

                        await SendPhoneUnassignedEmailAsync(
                            previousUserForEmail,
                            existingAssignment,
                            $"This phone number has been reassigned to {newUserName} (Index: {indexNumber})"
                        );
                    }
                }

                // Check if this user already has this phone (reactivate if needed)
                var existingPhone = await _context.UserPhones
                    .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.PhoneNumber == phoneNumber);

                if (existingPhone != null)
                {
                    if (existingPhone.IsActive)
                    {
                        // Phone is already active for this user - this shouldn't happen due to earlier check
                        _logger.LogWarning($"Attempted to assign phone {phoneNumber} to user {indexNumber} but it's already active");
                        return false;
                    }

                    // Reactivate existing assignment
                    existingPhone.IsActive = true;
                    existingPhone.UnassignedDate = null;
                    existingPhone.AssignedDate = DateTime.UtcNow;
                    existingPhone.PhoneType = phoneType;
                    existingPhone.LineType = lineType;
                    existingPhone.OwnershipType = ownershipType;
                    existingPhone.Purpose = purpose;
                    existingPhone.Location = location;
                    existingPhone.Notes = notes;
                    existingPhone.IsPrimary = isPrimary;
                    existingPhone.Status = status;
                    _logger.LogInformation($"Reactivated phone {phoneNumber} for user {indexNumber}");
                }
                else
                {
                    // Create new assignment
                    var userPhone = new UserPhone
                    {
                        IndexNumber = indexNumber,
                        PhoneNumber = phoneNumber,
                        PhoneType = phoneType,
                        LineType = lineType,
                        OwnershipType = ownershipType,
                        Purpose = purpose,
                        IsPrimary = isPrimary,
                        IsActive = true,
                        Location = location,
                        Notes = notes,
                        ClassOfServiceId = classOfServiceId,
                        AssignedDate = DateTime.UtcNow,
                        Status = status
                    };
                    _context.UserPhones.Add(userPhone);
                }

                // If setting as primary, remove primary from others and set their LineType to Secondary
                if (isPrimary)
                {
                    var otherPhones = await _context.UserPhones
                        .Where(up => up.IndexNumber == indexNumber &&
                                    up.PhoneNumber != phoneNumber &&
                                    up.IsPrimary)
                        .ToListAsync();

                    foreach (var phone in otherPhones)
                    {
                        phone.IsPrimary = false;
                        // Set LineType to Secondary when removing primary status
                        phone.LineType = LineType.Secondary;

                        // Add history for removed primary status
                        await _historyService.AddHistoryAsync(
                            phone.Id,
                            "SetPrimary",
                            $"Primary status removed (new primary: {phoneNumber})",
                            "System",
                            "IsPrimary, LineType",
                            "true, Primary",
                            "false, Secondary"
                        );
                    }

                    // Update user's primary phone
                    user.OfficialMobileNumber = phoneNumber;
                }

                await _context.SaveChangesAsync();

                // Add history for the newly assigned/reactivated phone
                var assignedPhone = existingPhone ?? await _context.UserPhones
                    .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.PhoneNumber == phoneNumber);

                if (assignedPhone != null)
                {
                    // Copy history from old phone if this is a reassignment and we have the old phone ID
                    if (isReassignment && oldPhoneId.HasValue && oldPhoneId.Value != assignedPhone.Id)
                    {
                        await _historyService.CopyHistoryToNewPhoneAsync(oldPhoneId.Value, assignedPhone.Id, "System");
                        _logger.LogInformation($"Copied history from old phone {oldPhoneId.Value} to new phone {assignedPhone.Id}");
                    }

                    string action;
                    string description;

                    if (isReassignment)
                    {
                        // This was a reassignment from another user
                        action = "Reassigned";
                        description = $"Phone reassigned from {previousUserName} ({previousUserIndex})";
                    }
                    else if (existingPhone != null)
                    {
                        // Reactivating an existing phone record for this user
                        action = "Assigned";
                        description = "Phone reactivated and assigned to user";
                    }
                    else
                    {
                        // Brand new phone assignment
                        action = "Created";
                        description = "Phone created and assigned to user";
                    }

                    if (isPrimary)
                    {
                        description += " as Primary line";
                    }

                    await _historyService.AddHistoryAsync(
                        assignedPhone.Id,
                        action,
                        description,
                        "System",
                        null,
                        null,
                        $"{phoneType}, {lineType}"
                    );

                    // Send email notification for phone assignment
                    await SendPhoneAssignedEmailAsync(user, assignedPhone);
                }

                _logger.LogInformation($"Phone {phoneNumber} assigned to user {indexNumber}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning phone {phoneNumber} to user {indexNumber}");
                return false;
            }
        }

        public async Task<bool> UnassignPhoneAsync(int phoneId)
        {
            try
            {
                var phone = await _context.UserPhones
                    .Include(p => p.EbillUser)
                        .ThenInclude(u => u.ApplicationUser)
                    .FirstOrDefaultAsync(p => p.Id == phoneId);

                if (phone == null)
                {
                    return false;
                }

                var wasPrimary = phone.IsPrimary;
                phone.IsActive = false;
                phone.UnassignedDate = DateTime.UtcNow;

                // If this was primary, need to assign another as primary
                if (phone.IsPrimary)
                {
                    phone.IsPrimary = false;

                    var nextPhone = await _context.UserPhones
                        .Where(up => up.IndexNumber == phone.IndexNumber &&
                                    up.Id != phone.Id &&
                                    up.IsActive)
                        .FirstOrDefaultAsync();

                    if (nextPhone != null)
                    {
                        nextPhone.IsPrimary = true;
                        nextPhone.LineType = LineType.Primary;

                        var user = await _context.EbillUsers
                            .FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);
                        if (user != null)
                        {
                            user.OfficialMobileNumber = nextPhone.PhoneNumber;
                        }

                        // Add history for the phone that became primary
                        await _historyService.AddHistoryAsync(
                            nextPhone.Id,
                            "SetPrimary",
                            $"Automatically set as primary after {phone.PhoneNumber} was unassigned",
                            "System",
                            "IsPrimary, LineType",
                            "false, Secondary",
                            "true, Primary"
                        );
                    }
                }

                await _context.SaveChangesAsync();

                // Add history for the unassigned phone
                await _historyService.AddHistoryAsync(
                    phone.Id,
                    "Unassigned",
                    wasPrimary ? "Primary phone unassigned" : "Phone unassigned",
                    "System"
                );

                // Send email notification for phone unassignment
                if (phone.EbillUser != null)
                {
                    await SendPhoneUnassignedEmailAsync(phone.EbillUser, phone);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unassigning phone {phoneId}");
                return false;
            }
        }

        public async Task<bool> SetPrimaryPhoneAsync(int phoneId)
        {
            try
            {
                var phone = await _context.UserPhones.FindAsync(phoneId);
                if (phone == null || !phone.IsActive)
                {
                    return false;
                }

                // Load user first to use for email notifications
                var user = await _context.EbillUsers
                    .Include(u => u.ApplicationUser)
                    .FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);

                // Remove primary from other phones and set their LineType to Secondary
                var otherPhones = await _context.UserPhones
                    .Where(up => up.IndexNumber == phone.IndexNumber &&
                                up.Id != phone.Id &&
                                up.IsPrimary)
                    .ToListAsync();

                foreach (var otherPhone in otherPhones)
                {
                    otherPhone.IsPrimary = false;
                    // Set LineType to Secondary when removing primary status
                    otherPhone.LineType = LineType.Secondary;

                    // Add history for removed primary status
                    await _historyService.AddHistoryAsync(
                        otherPhone.Id,
                        "SetPrimary",
                        $"Primary status removed (new primary: {phone.PhoneNumber})",
                        "System",
                        "IsPrimary, LineType",
                        "true, Primary",
                        "false, Secondary"
                    );

                    // Send email notification for phone being demoted from Primary to Secondary
                    if (user != null)
                    {
                        await SendPhoneTypeChangedEmailAsync(user, otherPhone.PhoneNumber, LineType.Primary, LineType.Secondary);
                        _logger.LogInformation($"Sent phone type changed email for {otherPhone.PhoneNumber} (demoted to Secondary)");
                    }
                }

                // Set this as primary and sync LineType
                phone.IsPrimary = true;
                phone.LineType = LineType.Primary;

                // Update user's primary phone
                if (user != null)
                {
                    user.OfficialMobileNumber = phone.PhoneNumber;
                }

                await _context.SaveChangesAsync();

                // Add history for the phone that became primary
                await _historyService.AddHistoryAsync(
                    phone.Id,
                    "SetPrimary",
                    "Set as primary phone",
                    "System",
                    "IsPrimary, LineType",
                    "false, Secondary",
                    "true, Primary"
                );

                // Send email notification for phone type change to Primary
                if (user != null)
                {
                    await SendPhoneTypeChangedEmailAsync(user, phone.PhoneNumber, LineType.Secondary, LineType.Primary);
                    _logger.LogInformation($"Sent phone type changed email for {phone.PhoneNumber} (promoted to Primary)");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting primary phone {phoneId}");
                return false;
            }
        }

        public async Task<string?> GetUserByPhoneAsync(string phoneNumber, DateTime? billDate = null)
        {
            var date = billDate ?? DateTime.UtcNow;

            var assignment = await _context.UserPhones
                .Where(up => up.PhoneNumber == phoneNumber &&
                           up.AssignedDate <= date &&
                           (up.UnassignedDate == null || up.UnassignedDate > date))
                .OrderByDescending(up => up.IsPrimary)
                .ThenByDescending(up => up.AssignedDate)
                .FirstOrDefaultAsync();

            return assignment?.IndexNumber;
        }

        public async Task<Dictionary<string, List<UserPhone>>> GetAllUserPhonesAsync()
        {
            var phones = await _context.UserPhones
                .Where(up => up.IsActive)
                .Include(up => up.EbillUser)
                .OrderBy(up => up.IndexNumber)
                .ThenByDescending(up => up.IsPrimary)
                .ToListAsync();

            return phones.GroupBy(p => p.IndexNumber)
                        .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<bool> IsPhoneAvailableAsync(string phoneNumber, string? excludeIndexNumber = null)
        {
            var query = _context.UserPhones
                .Where(up => up.PhoneNumber == phoneNumber && up.IsActive);

            if (!string.IsNullOrEmpty(excludeIndexNumber))
            {
                query = query.Where(up => up.IndexNumber != excludeIndexNumber);
            }

            return !await query.AnyAsync();
        }

        // Email notification helper methods
        private async Task SendPhoneAssignedEmailAsync(EbillUser user, UserPhone phone)
        {
            try
            {
                if (user.ApplicationUser == null || string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning("Cannot send phone assigned email: User {IndexNumber} has no email", user.IndexNumber);
                    return;
                }

                var userPhonesUrl = GetUserPhonesUrl(user.IndexNumber);
                var (badgeColor, textColor) = GetLineTypeBadgeColors(phone.LineType);

                var emailData = new Dictionary<string, string>
                {
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "PhoneNumber", phone.PhoneNumber },
                    { "PhoneType", phone.PhoneType },
                    { "LineType", phone.LineType.ToString() },
                    { "LineTypeBadgeColor", badgeColor },
                    { "LineTypeTextColor", textColor },
                    { "IndexNumber", user.IndexNumber },
                    { "AssignedDate", DateTime.Now.ToString("MMMM dd, yyyy 'at' hh:mm tt") },
                    { "UserPhonesUrl", userPhonesUrl }
                };

                await _enhancedEmailService.SendTemplatedEmailAsync(
                    to: user.Email,
                    templateCode: "PHONE_NUMBER_ASSIGNED",
                    data: emailData,
                    createdBy: "System",
                    relatedEntityType: "UserPhone",
                    relatedEntityId: phone.Id.ToString()
                );

                _logger.LogInformation("Sent phone assigned email to {Email} for phone {PhoneNumber}", user.Email, phone.PhoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send phone assigned email to {Email}", user.Email);
            }
        }

        private async Task SendPhoneTypeChangedEmailAsync(EbillUser user, string phoneNumber, LineType oldLineType, LineType newLineType)
        {
            try
            {
                if (user.ApplicationUser == null || string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning("Cannot send phone type changed email: User {IndexNumber} has no email", user.IndexNumber);
                    return;
                }

                var userPhonesUrl = GetUserPhonesUrl(user.IndexNumber);
                var (badgeColor, textColor) = GetLineTypeBadgeColors(newLineType);
                var statusDescription = GetLineTypeDescription(newLineType);

                var emailData = new Dictionary<string, string>
                {
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "PhoneNumber", phoneNumber },
                    { "OldLineType", oldLineType.ToString() },
                    { "NewLineType", newLineType.ToString() },
                    { "LineTypeBadgeColor", badgeColor },
                    { "LineTypeTextColor", textColor },
                    { "StatusDescription", statusDescription },
                    { "IndexNumber", user.IndexNumber },
                    { "ChangeDate", DateTime.Now.ToString("MMMM dd, yyyy 'at' hh:mm tt") },
                    { "UserPhonesUrl", userPhonesUrl }
                };

                await _enhancedEmailService.SendTemplatedEmailAsync(
                    to: user.Email,
                    templateCode: "PHONE_TYPE_CHANGED",
                    data: emailData,
                    createdBy: "System"
                );

                _logger.LogInformation("Sent phone type changed email to {Email} for phone {PhoneNumber}", user.Email, phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send phone type changed email to {Email}", user.Email);
            }
        }

        private async Task SendPhoneUnassignedEmailAsync(EbillUser user, UserPhone phone, string? reason = null)
        {
            try
            {
                if (user.ApplicationUser == null || string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning("Cannot send phone unassigned email: User {IndexNumber} has no email", user.IndexNumber);
                    return;
                }

                var userPhonesUrl = GetUserPhonesUrl(user.IndexNumber);

                var emailData = new Dictionary<string, string>
                {
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "PhoneNumber", phone.PhoneNumber },
                    { "PhoneType", phone.PhoneType },
                    { "LineType", phone.LineType.ToString() },
                    { "IndexNumber", user.IndexNumber },
                    { "UnassignedDate", DateTime.Now.ToString("MMMM dd, yyyy 'at' hh:mm tt") },
                    { "Reason", reason ?? "Not specified" },
                    { "UserPhonesUrl", userPhonesUrl }
                };

                await _enhancedEmailService.SendTemplatedEmailAsync(
                    to: user.Email,
                    templateCode: "PHONE_NUMBER_UNASSIGNED",
                    data: emailData,
                    createdBy: "System",
                    relatedEntityType: "UserPhone",
                    relatedEntityId: phone.Id.ToString()
                );

                _logger.LogInformation("Sent phone unassigned email to {Email} for phone {PhoneNumber}", user.Email, phone.PhoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send phone unassigned email to {Email}", user.Email);
            }
        }

        private string GetUserPhonesUrl(string indexNumber)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                return $"{request.Scheme}://{request.Host}/Admin/UserPhones?indexNumber={indexNumber}";
            }

            return $"http://localhost:5041/Admin/UserPhones?indexNumber={indexNumber}";
        }

        private (string badgeColor, string textColor) GetLineTypeBadgeColors(LineType lineType)
        {
            return lineType switch
            {
                LineType.Primary => ("#10b981", "#ffffff"), // Green background, white text
                LineType.Secondary => ("#dbeafe", "#1e40af"), // Light blue background, dark blue text
                LineType.Reserved => ("#fef3c7", "#92400e"), // Light yellow background, dark yellow text
                _ => ("#e5e7eb", "#1f2937") // Gray background, dark text
            };
        }

        private string GetLineTypeDescription(LineType lineType)
        {
            return lineType switch
            {
                LineType.Primary => @"
                    <li>This is now your official primary phone number</li>
                    <li>It will be used as your main contact number in the system</li>
                    <li>All official communications will reference this number</li>
                    <li>You are responsible for all calls made on this number</li>
                    <li>This number will appear on your official records and reports</li>",

                LineType.Secondary => @"
                    <li>This is a secondary phone number assigned to your account</li>
                    <li>It serves as an additional contact line</li>
                    <li>You remain responsible for calls made on this number</li>
                    <li>This number is for official UNON business use</li>
                    <li>Secondary numbers appear in your phone list but are not your primary contact</li>",

                LineType.Reserved => @"
                    <li>This phone number has been reserved for your account</li>
                    <li>Reserved numbers are held for future assignment or special purposes</li>
                    <li>You may have limited or no active usage on this line</li>
                    <li>Contact ICTS if you need this number activated</li>
                    <li>This status is typically temporary pending activation or assignment</li>",

                _ => "<li>Line type status updated</li>"
            };
        }
    }
}