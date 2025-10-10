using System;

namespace TAB.Web.Services
{
    /// <summary>
    /// Service for handling GUID-based entity operations
    /// </summary>
    public interface IGuidService
    {
        /// <summary>
        /// Find an EbillUser by PublicId
        /// </summary>
        Task<Models.EbillUser?> GetEbillUserByPublicIdAsync(Guid publicId);

        /// <summary>
        /// Find an Organization by PublicId
        /// </summary>
        Task<Models.Organization?> GetOrganizationByPublicIdAsync(Guid publicId);

        /// <summary>
        /// Find an Office by PublicId
        /// </summary>
        Task<Models.Office?> GetOfficeByPublicIdAsync(Guid publicId);

        /// <summary>
        /// Generate a new secure GUID
        /// </summary>
        Guid GenerateSecureGuid();

        /// <summary>
        /// Validate if a string is a valid GUID
        /// </summary>
        bool IsValidGuid(string value);

        /// <summary>
        /// Try parse a string to GUID
        /// </summary>
        bool TryParseGuid(string value, out Guid guid);
    }
}