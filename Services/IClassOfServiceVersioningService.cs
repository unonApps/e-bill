using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface IClassOfServiceVersioningService
    {
        /// <summary>
        /// Gets the Class of Service version that was effective on a specific date
        /// </summary>
        /// <param name="classOfServiceId">The ClassOfService ID (any version)</param>
        /// <param name="effectiveDate">The date to check (e.g., billing period date)</param>
        /// <returns>The version effective on that date, or null if not found</returns>
        Task<ClassOfService?> GetEffectiveVersionAsync(int classOfServiceId, DateTime effectiveDate);

        /// <summary>
        /// Gets the current (latest) version of a Class of Service
        /// </summary>
        /// <param name="classOfServiceId">The ClassOfService ID (any version)</param>
        /// <returns>The current version, or null if not found</returns>
        Task<ClassOfService?> GetCurrentVersionAsync(int classOfServiceId);

        /// <summary>
        /// Gets all versions of a Class of Service ordered by version number
        /// </summary>
        /// <param name="classOfServiceId">The ClassOfService ID (any version)</param>
        /// <returns>List of all versions</returns>
        Task<List<ClassOfService>> GetAllVersionsAsync(int classOfServiceId);

        /// <summary>
        /// Creates a new version of a Class of Service with a new effective date
        /// This will end-date the current version and create a new one
        /// </summary>
        /// <param name="currentVersionId">The current version ID to copy from</param>
        /// <param name="effectiveFrom">When the new version becomes effective</param>
        /// <param name="updatedValues">Action to update the new version's values</param>
        /// <returns>The newly created version</returns>
        Task<ClassOfService> CreateNewVersionAsync(
            int currentVersionId,
            DateTime effectiveFrom,
            Action<ClassOfService> updatedValues);

        /// <summary>
        /// Gets the version history for a Class of Service showing what changed and when
        /// </summary>
        /// <param name="classOfServiceId">The ClassOfService ID (any version)</param>
        /// <returns>List of version history items</returns>
        Task<List<ClassOfServiceVersionHistory>> GetVersionHistoryAsync(int classOfServiceId);

        /// <summary>
        /// Gets the root/original ClassOfService ID for any version
        /// </summary>
        /// <param name="classOfServiceId">The ClassOfService ID (any version)</param>
        /// <returns>The root ClassOfService ID</returns>
        Task<int> GetRootClassOfServiceIdAsync(int classOfServiceId);
    }

    public class ClassOfServiceVersionHistory
    {
        public int Version { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public decimal? AirtimeAllowanceAmount { get; set; }
        public decimal? DataAllowanceAmount { get; set; }
        public decimal? HandsetAllowanceAmount { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsCurrent { get; set; }
    }
}
