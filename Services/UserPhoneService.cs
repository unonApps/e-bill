using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface IUserPhoneService
    {
        Task<List<UserPhone>> GetUserPhonesAsync(string indexNumber);
        Task<UserPhone?> GetPhoneAsync(int id);
        Task<bool> AssignPhoneAsync(string indexNumber, string phoneNumber, string phoneType, bool isPrimary, string? location = null, string? notes = null, int? classOfServiceId = null, bool forceReassign = false, PhoneStatus status = PhoneStatus.Active);
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

        public UserPhoneService(ApplicationDbContext context, ILogger<UserPhoneService> logger)
        {
            _context = context;
            _logger = logger;
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

        public async Task<bool> AssignPhoneAsync(string indexNumber, string phoneNumber, string phoneType, bool isPrimary, string? location = null, string? notes = null, int? classOfServiceId = null, bool forceReassign = false, PhoneStatus status = PhoneStatus.Active)
        {
            try
            {
                // Check if user exists
                var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                if (user == null)
                {
                    _logger.LogWarning($"User with IndexNumber {indexNumber} not found");
                    return false;
                }

                // Check if phone is already assigned to another user
                var existingAssignment = await _context.UserPhones
                    .FirstOrDefaultAsync(up => up.PhoneNumber == phoneNumber &&
                                              up.IsActive &&
                                              up.IndexNumber != indexNumber);

                if (existingAssignment != null)
                {
                    if (!forceReassign)
                    {
                        _logger.LogWarning($"Phone {phoneNumber} is already assigned to user {existingAssignment.IndexNumber}");
                        return false;
                    }

                    // Unassign from previous user
                    _logger.LogInformation($"Reassigning phone {phoneNumber} from user {existingAssignment.IndexNumber} to user {indexNumber}");
                    existingAssignment.IsActive = false;
                    existingAssignment.UnassignedDate = DateTime.UtcNow;

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

                // If setting as primary, remove primary from others
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
                    }

                    // Update user's primary phone
                    user.OfficialMobileNumber = phoneNumber;
                }

                await _context.SaveChangesAsync();
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
                var phone = await _context.UserPhones.FindAsync(phoneId);
                if (phone == null)
                {
                    return false;
                }

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

                        var user = await _context.EbillUsers
                            .FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);
                        if (user != null)
                        {
                            user.OfficialMobileNumber = nextPhone.PhoneNumber;
                        }
                    }
                }

                await _context.SaveChangesAsync();
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

                // Remove primary from other phones
                var otherPhones = await _context.UserPhones
                    .Where(up => up.IndexNumber == phone.IndexNumber &&
                                up.Id != phone.Id &&
                                up.IsPrimary)
                    .ToListAsync();

                foreach (var otherPhone in otherPhones)
                {
                    otherPhone.IsPrimary = false;
                }

                // Set this as primary
                phone.IsPrimary = true;

                // Update user's primary phone
                var user = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);
                if (user != null)
                {
                    user.OfficialMobileNumber = phone.PhoneNumber;
                }

                await _context.SaveChangesAsync();
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
    }
}