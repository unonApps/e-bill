using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public class ClassOfServiceVersioningService : IClassOfServiceVersioningService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClassOfServiceVersioningService> _logger;

        public ClassOfServiceVersioningService(
            ApplicationDbContext context,
            ILogger<ClassOfServiceVersioningService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ClassOfService?> GetEffectiveVersionAsync(int classOfServiceId, DateTime effectiveDate)
        {
            // Get the root ID first
            var rootId = await GetRootClassOfServiceIdAsync(classOfServiceId);

            // Find all versions with this root ID
            var versions = await _context.ClassOfServices
                .Where(c => c.Id == rootId || c.ParentClassOfServiceId == rootId)
                .ToListAsync();

            // Find the version that was effective on the given date
            return versions
                .Where(v => v.EffectiveFrom.Date <= effectiveDate.Date &&
                           (!v.EffectiveTo.HasValue || v.EffectiveTo.Value.Date >= effectiveDate.Date))
                .OrderByDescending(v => v.Version)
                .FirstOrDefault();
        }

        public async Task<ClassOfService?> GetCurrentVersionAsync(int classOfServiceId)
        {
            // Get the root ID first
            var rootId = await GetRootClassOfServiceIdAsync(classOfServiceId);

            // Find all versions with this root ID
            var versions = await _context.ClassOfServices
                .Where(c => c.Id == rootId || c.ParentClassOfServiceId == rootId)
                .ToListAsync();

            // Find the current version (no end date or end date in the future)
            return versions
                .Where(v => !v.EffectiveTo.HasValue || v.EffectiveTo.Value.Date >= DateTime.UtcNow.Date)
                .OrderByDescending(v => v.Version)
                .FirstOrDefault();
        }

        public async Task<List<ClassOfService>> GetAllVersionsAsync(int classOfServiceId)
        {
            // Get the root ID first
            var rootId = await GetRootClassOfServiceIdAsync(classOfServiceId);

            // Find all versions with this root ID
            return await _context.ClassOfServices
                .Where(c => c.Id == rootId || c.ParentClassOfServiceId == rootId)
                .OrderBy(v => v.Version)
                .ToListAsync();
        }

        public async Task<ClassOfService> CreateNewVersionAsync(
            int currentVersionId,
            DateTime effectiveFrom,
            Action<ClassOfService> updatedValues)
        {
            var currentVersion = await _context.ClassOfServices.FindAsync(currentVersionId);
            if (currentVersion == null)
            {
                throw new ArgumentException($"ClassOfService with ID {currentVersionId} not found.");
            }

            // Get the root ID
            var rootId = await GetRootClassOfServiceIdAsync(currentVersionId);

            // Get all existing versions to determine the next version number
            var allVersions = await GetAllVersionsAsync(currentVersionId);
            var maxVersion = allVersions.Max(v => v.Version);

            // End-date the current version (set EffectiveTo to the day before the new version starts)
            currentVersion.EffectiveTo = effectiveFrom.Date.AddDays(-1);

            // Create new version by copying the current one
            var newVersion = new ClassOfService
            {
                Class = currentVersion.Class,
                Service = currentVersion.Service,
                EligibleStaff = currentVersion.EligibleStaff,
                AirtimeAllowance = currentVersion.AirtimeAllowance,
                DataAllowance = currentVersion.DataAllowance,
                HandsetAllowance = currentVersion.HandsetAllowance,
                HandsetAIRemarks = currentVersion.HandsetAIRemarks,
                AirtimeAllowanceAmount = currentVersion.AirtimeAllowanceAmount,
                DataAllowanceAmount = currentVersion.DataAllowanceAmount,
                HandsetAllowanceAmount = currentVersion.HandsetAllowanceAmount,
                BillingPeriod = currentVersion.BillingPeriod,
                ServiceStatus = currentVersion.ServiceStatus,
                EffectiveFrom = effectiveFrom.Date,
                EffectiveTo = null, // No end date for the new version
                Version = maxVersion + 1,
                ParentClassOfServiceId = rootId, // Link to the original
                CreatedDate = DateTime.UtcNow
            };

            // Apply the updates to the new version
            updatedValues(newVersion);

            // Add the new version to the database
            _context.ClassOfServices.Add(newVersion);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created new version {Version} for ClassOfService {RootId}, effective from {EffectiveFrom}",
                newVersion.Version, rootId, effectiveFrom.Date);

            return newVersion;
        }

        public async Task<List<ClassOfServiceVersionHistory>> GetVersionHistoryAsync(int classOfServiceId)
        {
            var versions = await GetAllVersionsAsync(classOfServiceId);

            return versions
                .OrderByDescending(v => v.Version)
                .Select(v => new ClassOfServiceVersionHistory
                {
                    Version = v.Version,
                    EffectiveFrom = v.EffectiveFrom,
                    EffectiveTo = v.EffectiveTo,
                    AirtimeAllowanceAmount = v.AirtimeAllowanceAmount,
                    DataAllowanceAmount = v.DataAllowanceAmount,
                    HandsetAllowanceAmount = v.HandsetAllowanceAmount,
                    CreatedDate = v.CreatedDate,
                    IsCurrent = v.IsCurrentVersion
                })
                .ToList();
        }

        public async Task<int> GetRootClassOfServiceIdAsync(int classOfServiceId)
        {
            var classOfService = await _context.ClassOfServices
                .FirstOrDefaultAsync(c => c.Id == classOfServiceId);

            if (classOfService == null)
            {
                throw new ArgumentException($"ClassOfService with ID {classOfServiceId} not found.");
            }

            // If this has a parent, the parent is the root
            // If it doesn't have a parent, it IS the root
            return classOfService.ParentClassOfServiceId ?? classOfService.Id;
        }
    }
}
