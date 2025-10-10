using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public class GuidService : IGuidService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GuidService> _logger;

        public GuidService(ApplicationDbContext context, ILogger<GuidService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EbillUser?> GetEbillUserByPublicIdAsync(Guid publicId)
        {
            try
            {
                return await _context.EbillUsers
                    .Include(e => e.OrganizationEntity)
                    .Include(e => e.OfficeEntity)
                    .Include(e => e.SubOfficeEntity)
                    .FirstOrDefaultAsync(e => e.PublicId == publicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving EbillUser by PublicId: {PublicId}", publicId);
                return null;
            }
        }

        public async Task<Organization?> GetOrganizationByPublicIdAsync(Guid publicId)
        {
            try
            {
                return await _context.Organizations
                    .FirstOrDefaultAsync(o => o.PublicId == publicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Organization by PublicId: {PublicId}", publicId);
                return null;
            }
        }

        public async Task<Office?> GetOfficeByPublicIdAsync(Guid publicId)
        {
            try
            {
                return await _context.Offices
                    .FirstOrDefaultAsync(o => o.PublicId == publicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Office by PublicId: {PublicId}", publicId);
                return null;
            }
        }

        public Guid GenerateSecureGuid()
        {
            // Use cryptographically secure GUID generation
            return Guid.NewGuid();
        }

        public bool IsValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        public bool TryParseGuid(string value, out Guid guid)
        {
            return Guid.TryParse(value, out guid);
        }
    }
}