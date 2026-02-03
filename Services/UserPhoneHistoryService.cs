using TAB.Web.Data;
using TAB.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace TAB.Web.Services
{
    public interface IUserPhoneHistoryService
    {
        Task AddHistoryAsync(int userPhoneId, string action, string? description = null, string? changedBy = null, string? fieldChanged = null, string? oldValue = null, string? newValue = null);
        Task<List<UserPhoneHistory>> GetHistoryForPhoneAsync(int userPhoneId);
        Task<List<UserPhoneHistory>> GetHistoryByPhoneNumberAsync(string phoneNumber);
        Task<List<UserPhoneHistory>> GetRecentHistoryForUserAsync(string indexNumber, int count = 10);
        Task CopyHistoryToNewPhoneAsync(int oldPhoneId, int newPhoneId, string? changedBy = null);
    }

    public class UserPhoneHistoryService : IUserPhoneHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserPhoneHistoryService> _logger;

        public UserPhoneHistoryService(ApplicationDbContext context, ILogger<UserPhoneHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddHistoryAsync(int userPhoneId, string action, string? description = null, string? changedBy = null, string? fieldChanged = null, string? oldValue = null, string? newValue = null)
        {
            try
            {
                var history = new UserPhoneHistory
                {
                    UserPhoneId = userPhoneId,
                    Action = action,
                    Description = description,
                    ChangedBy = changedBy ?? "System",
                    FieldChanged = fieldChanged,
                    OldValue = oldValue,
                    NewValue = newValue,
                    ChangedDate = DateTime.UtcNow
                };

                _context.UserPhoneHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Added history entry for UserPhone {userPhoneId}: {action}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding history for UserPhone {userPhoneId}");
            }
        }

        public async Task<List<UserPhoneHistory>> GetHistoryForPhoneAsync(int userPhoneId)
        {
            return await _context.UserPhoneHistories
                .Where(h => h.UserPhoneId == userPhoneId)
                .OrderByDescending(h => h.ChangedDate)
                .ToListAsync();
        }

        public async Task<List<UserPhoneHistory>> GetRecentHistoryForUserAsync(string indexNumber, int count = 10)
        {
            return await _context.UserPhoneHistories
                .Include(h => h.UserPhone)
                .Where(h => h.UserPhone!.IndexNumber == indexNumber)
                .OrderByDescending(h => h.ChangedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<UserPhoneHistory>> GetHistoryByPhoneNumberAsync(string phoneNumber)
        {
            // Get all history entries for a phone number across all UserPhone records
            // This is useful for viewing complete history of a phone number regardless of reassignments
            return await _context.UserPhoneHistories
                .Include(h => h.UserPhone)
                .Where(h => h.UserPhone!.PhoneNumber == phoneNumber)
                .OrderByDescending(h => h.ChangedDate)
                .ToListAsync();
        }

        public async Task CopyHistoryToNewPhoneAsync(int oldPhoneId, int newPhoneId, string? changedBy = null)
        {
            try
            {
                // Get all history from the old phone
                var oldHistory = await _context.UserPhoneHistories
                    .Where(h => h.UserPhoneId == oldPhoneId)
                    .OrderBy(h => h.ChangedDate)
                    .ToListAsync();

                if (!oldHistory.Any())
                {
                    _logger.LogInformation($"No history to copy from UserPhone {oldPhoneId} to {newPhoneId}");
                    return;
                }

                // Copy each history entry to the new phone
                foreach (var history in oldHistory)
                {
                    var newHistory = new UserPhoneHistory
                    {
                        UserPhoneId = newPhoneId,
                        Action = history.Action,
                        Description = history.Description,
                        ChangedBy = history.ChangedBy,
                        FieldChanged = history.FieldChanged,
                        OldValue = history.OldValue,
                        NewValue = history.NewValue,
                        ChangedDate = history.ChangedDate // Preserve original date
                    };
                    _context.UserPhoneHistories.Add(newHistory);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Copied {oldHistory.Count} history entries from UserPhone {oldPhoneId} to {newPhoneId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error copying history from UserPhone {oldPhoneId} to {newPhoneId}");
            }
        }
    }
}
