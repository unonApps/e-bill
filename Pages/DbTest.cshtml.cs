using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TAB.Web.Data;

namespace TAB.Web.Pages
{
    [Authorize(Roles = "Admin")]
    public class DbTestModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbTestModel> _logger;

        public bool IsConnected { get; set; }
        public string? ErrorMessage { get; set; }
        public int TableCount { get; set; }
        public int UserCount { get; set; }
        public string ServerName { get; set; } = "";
        public string DatabaseName { get; set; } = "";

        public DbTestModel(IConfiguration configuration, ILogger<DbTestModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";

            // Parse connection string for display
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                ServerName = builder.DataSource;
                DatabaseName = builder.InitialCatalog;
            }
            catch
            {
                ServerName = "Unable to parse";
                DatabaseName = "Unable to parse";
            }

            try
            {
                _logger.LogInformation("Testing database connection...");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Test query - count tables
                using var cmd1 = new SqlCommand(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
                    connection);
                TableCount = (int)await cmd1.ExecuteScalarAsync();

                // Test query - count users
                using var cmd2 = new SqlCommand(
                    "SELECT COUNT(*) FROM [ebill].[AspNetUsers]",
                    connection);
                UserCount = (int)await cmd2.ExecuteScalarAsync();

                IsConnected = true;
                _logger.LogInformation("Database connection successful! Tables: {TableCount}, Users: {UserCount}",
                    TableCount, UserCount);
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ErrorMessage = ex.Message;
                _logger.LogError(ex, "Database connection failed");
            }
        }
    }
}
