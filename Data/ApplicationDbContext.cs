using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Models;

namespace TAB.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Office> Offices { get; set; }
        public DbSet<SubOffice> SubOffices { get; set; }
        public DbSet<ClassOfService> ClassOfServices { get; set; }
        public DbSet<Models.ServiceProvider> ServiceProviders { get; set; }
        public DbSet<SimRequest> SimRequests { get; set; }
        public DbSet<SimRequestHistory> SimRequestHistories { get; set; }
        public DbSet<RefundRequest> RefundRequests { get; set; }
        public DbSet<RefundRequestHistory> RefundRequestHistories { get; set; }
        public DbSet<Ebill> Ebills { get; set; }
        public DbSet<EbillUser> EbillUsers { get; set; }

        /// <summary>
        /// DEPRECATED: Legacy call log table. Use Safaricom, Airtel, PSTN, or PrivateWire tables instead.
        /// </summary>
        [Obsolete("CallLogs table is deprecated. Use specific telecom tables (Safaricoms, Airtels, PSTNs, PrivateWires) instead.")]
        public DbSet<CallLog> CallLogs { get; set; }

        public DbSet<ImportAudit> ImportAudits { get; set; }
        public DbSet<PSTN> PSTNs { get; set; }
        public DbSet<PrivateWire> PrivateWires { get; set; }
        public DbSet<Safaricom> Safaricoms { get; set; }
        public DbSet<Airtel> Airtels { get; set; }
        public DbSet<UserPhone> UserPhones { get; set; }
        public DbSet<UserPhoneHistory> UserPhoneHistories { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }

        // Call Log Staging Tables
        public DbSet<CallLogStaging> CallLogStagings { get; set; }
        public DbSet<StagingBatch> StagingBatches { get; set; }
        public DbSet<CallRecord> CallRecords { get; set; }
        public DbSet<AnomalyType> AnomalyTypes { get; set; }
        public DbSet<ImportJob> ImportJobs { get; set; }

        // Billing Period Management
        public DbSet<BillingPeriod> BillingPeriods { get; set; }
        public DbSet<InterimUpdate> InterimUpdates { get; set; }
        public DbSet<CallLogReconciliation> CallLogReconciliations { get; set; }

        // Audit Trail
        public DbSet<AuditLog> AuditLogs { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; }

        // Call Log Verification System
        public DbSet<CallLogVerification> CallLogVerifications { get; set; }
        public DbSet<CallLogPaymentAssignment> CallLogPaymentAssignments { get; set; }

        /// <summary>
        /// DEPRECATED: Per-call documents are replaced by extension-level overage documents.
        /// Use PhoneOverageDocuments table instead.
        /// </summary>
        [Obsolete("CallLogDocuments is deprecated. Use PhoneOverageDocuments for extension-level justification instead.")]
        public DbSet<CallLogDocument> CallLogDocuments { get; set; }

        // Phone Overage Justification System (Extension-Level)
        public DbSet<PhoneOverageJustification> PhoneOverageJustifications { get; set; }
        public DbSet<PhoneOverageDocument> PhoneOverageDocuments { get; set; }

        // Call Log Recovery and Reporting System
        public DbSet<RecoveryLog> RecoveryLogs { get; set; }
        public DbSet<DeadlineTracking> DeadlineTracking { get; set; }
        public DbSet<RecoveryConfiguration> RecoveryConfigurations { get; set; }
        public DbSet<RecoveryJobExecution> RecoveryJobExecutions { get; set; }

        // Email Management System
        public DbSet<EmailConfiguration> EmailConfigurations { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<EmailAttachment> EmailAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Set default schema for all tables
            builder.HasDefaultSchema("ebill");

            // Configure Organization entity
            builder.Entity<Organization>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Office entity
            builder.Entity<Office>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasOne(e => e.Organization)
                      .WithMany(o => o.Offices)
                      .HasForeignKey(e => e.OrganizationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SubOffice entity
            builder.Entity<SubOffice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ContactPerson).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.HasOne(e => e.Office)
                      .WithMany(o => o.SubOffices)
                      .HasForeignKey(e => e.OfficeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ApplicationUser relationships
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(e => e.Organization)
                      .WithMany(o => o.Users)
                      .HasForeignKey(e => e.OrganizationId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Office)
                      .WithMany(o => o.Users)
                      .HasForeignKey(e => e.OfficeId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.SubOffice)
                      .WithMany(s => s.Users)
                      .HasForeignKey(e => e.SubOfficeId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure ClassOfService entity
            builder.Entity<ClassOfService>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Class).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Service).IsRequired().HasMaxLength(200);
                entity.Property(e => e.EligibleStaff).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AirtimeAllowance).HasMaxLength(50);
                entity.Property(e => e.DataAllowance).HasMaxLength(50);
                entity.Property(e => e.HandsetAllowance).HasMaxLength(50);
                entity.Property(e => e.HandsetAIRemarks).HasMaxLength(500);
                entity.Property(e => e.ServiceStatus).HasConversion<int>();
            });

            // Configure ServiceProvider entity
            builder.Entity<Models.ServiceProvider>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SPID).IsRequired().HasMaxLength(10);
                entity.Property(e => e.ServiceProviderName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SPMainCP).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SPMainCPEmail).IsRequired().HasMaxLength(300);
                entity.Property(e => e.SPOtherCPsEmail).HasMaxLength(1000);
                entity.Property(e => e.SPStatus).HasConversion<int>();
                entity.HasIndex(e => e.SPID).IsUnique();
            });

            // Configure ExchangeRate entity
            builder.Entity<ExchangeRate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Month).IsRequired();
                entity.Property(e => e.Year).IsRequired();
                entity.Property(e => e.Rate).IsRequired().HasColumnType("decimal(18,4)");
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(256);
                entity.Property(e => e.CreatedDate).IsRequired();
                // Ensure only one rate per month/year combination
                entity.HasIndex(e => new { e.Month, e.Year }).IsUnique();
            });

            // Configure SimRequest entity
            builder.Entity<SimRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.Property(e => e.IndexNo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Organization).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Office).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Grade).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FunctionalTitle).IsRequired().HasMaxLength(300);
                entity.Property(e => e.OfficeExtension).HasMaxLength(20);
                entity.Property(e => e.OfficialEmail).IsRequired().HasMaxLength(300);
                entity.Property(e => e.SimType).HasConversion<int>();
                entity.Property(e => e.Supervisor).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PreviouslyAssignedLines).HasMaxLength(1000);
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.RequestedBy).IsRequired().HasMaxLength(450);
                entity.Property(e => e.ProcessedBy).HasMaxLength(450);
                entity.Property(e => e.ProcessingNotes).HasMaxLength(500);
                entity.Property(e => e.SupervisorNotes).HasMaxLength(500);
                
                entity.HasOne(e => e.ServiceProvider)
                      .WithMany()
                      .HasForeignKey(e => e.ServiceProviderId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                // Note: Multiple SIM requests are allowed for the same IndexNo
                // as one staff member can have multiple SIM cards
                entity.HasIndex(e => e.IndexNo); // Non-unique index for performance
            });

            // Configure SimRequestHistory entity
            builder.Entity<SimRequestHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Comments).HasMaxLength(1000);
                entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(450);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PreviousStatus).HasMaxLength(50);
                entity.Property(e => e.NewStatus).HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.HasIndex(e => e.SimRequestId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasOne(e => e.SimRequest)
                    .WithMany(s => s.History)
                    .HasForeignKey(e => e.SimRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure RefundRequest entity (Mobile Device Reimbursement)
            builder.Entity<RefundRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.Property(e => e.PrimaryMobileNumber).IsRequired().HasMaxLength(9);
                entity.Property(e => e.IndexNo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.MobileNumberAssignedTo).IsRequired().HasMaxLength(200);
                entity.Property(e => e.OfficeExtension).HasMaxLength(20);
                entity.Property(e => e.Office).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MobileService).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ClassOfService).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DeviceAllowance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DevicePurchaseCurrency).IsRequired().HasMaxLength(3);
                entity.Property(e => e.DevicePurchaseAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Organization).IsRequired().HasMaxLength(200);
                entity.Property(e => e.UmojaBankName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Supervisor).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Remarks).HasMaxLength(200);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.RequestedBy).IsRequired().HasMaxLength(450);
                entity.Property(e => e.ProcessedBy).HasMaxLength(450);
                entity.Property(e => e.PurchaseReceiptPath).HasMaxLength(500);
            });

            // Configure Ebill entity
            builder.Entity<Ebill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(300);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Department).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.BillAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BillType).HasConversion<int>();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.AdditionalNotes).HasMaxLength(500);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.RequestedBy).IsRequired().HasMaxLength(450);
                entity.Property(e => e.ProcessedBy).HasMaxLength(450);
                entity.Property(e => e.ProcessingNotes).HasMaxLength(500);
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
                
                entity.HasOne(e => e.ServiceProvider)
                      .WithMany()
                      .HasForeignKey(e => e.ServiceProviderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure EbillUser entity
            builder.Entity<EbillUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IndexNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OfficialMobileNumber).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.HasIndex(e => e.IndexNumber).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure UserPhone entity for multiple phones per user
            builder.Entity<UserPhone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IndexNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PhoneType).HasMaxLength(50);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);

                // Indexes for performance
                entity.HasIndex(e => e.IndexNumber);
                entity.HasIndex(e => e.PhoneNumber);
                entity.HasIndex(e => new { e.IndexNumber, e.PhoneNumber, e.IsActive });

                // Relationship with EbillUser
                entity.HasOne(e => e.EbillUser)
                      .WithMany()
                      .HasPrincipalKey(u => u.IndexNumber)
                      .HasForeignKey(e => e.IndexNumber)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CallLog entity
            builder.Entity<CallLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AccountNo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SubAccountNo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SubAccountName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MSISDN).IsRequired().HasMaxLength(20);
                entity.Property(e => e.TaxInvoiceSummaryNo).HasMaxLength(50);
                entity.Property(e => e.InvoiceNo).HasMaxLength(50);
                entity.Property(e => e.NetAccessFee).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NetUsageLessTax).HasColumnType("decimal(18,2)");
                entity.Property(e => e.LessTaxes).HasColumnType("decimal(18,2)");
                entity.Property(e => e.VAT16).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Excise15).HasColumnType("decimal(18,2)");
                entity.Property(e => e.GrossTotal).HasColumnType("decimal(18,2)");
                
                // Create index on MSISDN for faster lookups when linking to EbillUsers
                entity.HasIndex(e => e.MSISDN);
                
                // Configure relationship with EbillUser
                entity.HasOne(e => e.EbillUser)
                      .WithMany()
                      .HasForeignKey(e => e.EbillUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure ImportAudit entity
            builder.Entity<ImportAudit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImportType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ImportedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.SummaryMessage).HasMaxLength(500);
                entity.HasIndex(e => e.ImportDate);
                entity.HasIndex(e => e.ImportType);
            });

            // Configure CallLogStaging entity
            builder.Entity<CallLogStaging>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BatchId);
                entity.HasIndex(e => e.VerificationStatus);
                entity.HasIndex(e => e.ResponsibleIndexNumber);
                entity.HasIndex(e => e.CallDate);
                entity.HasIndex(e => new { e.ExtensionNumber, e.CallDate, e.CallNumber });

                // Relationships
                entity.HasOne(e => e.Batch)
                      .WithMany(b => b.CallLogs)
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ResponsibleUser)
                      .WithMany()
                      .HasPrincipalKey(u => u.IndexNumber)
                      .HasForeignKey(e => e.ResponsibleIndexNumber)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.PayingUser)
                      .WithMany()
                      .HasPrincipalKey(u => u.IndexNumber)
                      .HasForeignKey(e => e.PayingIndexNumber)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure StagingBatch entity
            builder.Entity<StagingBatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BatchName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.BatchType).HasMaxLength(50);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.VerifiedBy).HasMaxLength(100);
                entity.Property(e => e.PublishedBy).HasMaxLength(100);
                entity.Property(e => e.SourceSystems).HasMaxLength(200);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.BatchStatus);
            });

            // Configure CallRecord entity (production table)
            builder.Entity<CallRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CallDate);
                entity.HasIndex(e => e.ExtensionNumber);
                entity.HasIndex(e => e.ResponsibleIndexNumber);
                entity.HasIndex(e => new { e.CallYear, e.CallMonth });

                // Relationships
                entity.HasOne(e => e.ResponsibleUser)
                      .WithMany()
                      .HasPrincipalKey(u => u.IndexNumber)
                      .HasForeignKey(e => e.ResponsibleIndexNumber)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.PayingUser)
                      .WithMany()
                      .HasPrincipalKey(u => u.IndexNumber)
                      .HasForeignKey(e => e.PayingIndexNumber)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure AnomalyType entity
            builder.Entity<AnomalyType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure CallLogVerification entity
            builder.Entity<CallLogVerification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VerifiedBy).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ApprovalStatus).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SupervisorIndexNumber).HasMaxLength(50);
                entity.Property(e => e.SupervisorEmail).HasMaxLength(256);
                entity.Property(e => e.SupervisorApprovalStatus).HasMaxLength(20);
                entity.Property(e => e.SupervisorApprovedBy).HasMaxLength(50);
                entity.Property(e => e.SupervisorComments).HasMaxLength(500);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);

                // Indexes for performance
                entity.HasIndex(e => e.VerifiedBy);
                entity.HasIndex(e => e.ApprovalStatus);
                entity.HasIndex(e => e.SupervisorIndexNumber);
                entity.HasIndex(e => e.SupervisorEmail);
                entity.HasIndex(e => new { e.CallRecordId, e.VerifiedBy });

                // Relationships
                entity.HasOne(e => e.CallRecord)
                      .WithMany()
                      .HasForeignKey(e => e.CallRecordId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ClassOfService)
                      .WithMany()
                      .HasForeignKey(e => e.ClassOfServiceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CallLogPaymentAssignment entity
            builder.Entity<CallLogPaymentAssignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AssignedFrom).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AssignedTo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AssignmentReason).IsRequired().HasMaxLength(500);
                entity.Property(e => e.AssignmentStatus).IsRequired().HasMaxLength(20);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);

                // Indexes for performance
                entity.HasIndex(e => e.AssignedTo);
                entity.HasIndex(e => e.AssignmentStatus);
                entity.HasIndex(e => new { e.AssignedFrom, e.AssignedTo });

                // Relationships
                entity.HasOne(e => e.CallRecord)
                      .WithMany()
                      .HasForeignKey(e => e.CallRecordId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CallLogDocument entity (DEPRECATED - Use PhoneOverageDocument instead)
            builder.Entity<CallLogDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.UploadedBy).IsRequired().HasMaxLength(50);

                // Indexes
                entity.HasIndex(e => e.CallLogVerificationId);

                // Relationships with CASCADE delete
                entity.HasOne(e => e.CallLogVerification)
                      .WithMany(v => v.Documents)
                      .HasForeignKey(e => e.CallLogVerificationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PhoneOverageJustification entity (Extension-Level Overage Management)
            builder.Entity<PhoneOverageJustification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.JustificationText).IsRequired();
                entity.Property(e => e.SubmittedBy).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ApprovalStatus).HasMaxLength(20);
                entity.Property(e => e.ApprovedBy).HasMaxLength(50);
                entity.Property(e => e.ApprovalComments).HasMaxLength(500);
                entity.Property(e => e.AllowanceLimit).HasColumnType("decimal(18,4)");
                entity.Property(e => e.TotalUsage).HasColumnType("decimal(18,4)");
                entity.Property(e => e.OverageAmount).HasColumnType("decimal(18,4)");

                // Indexes for performance
                entity.HasIndex(e => e.UserPhoneId);
                entity.HasIndex(e => new { e.Month, e.Year });
                entity.HasIndex(e => e.ApprovalStatus);
                entity.HasIndex(e => new { e.UserPhoneId, e.Month, e.Year }).IsUnique(); // One justification per phone per month

                // Relationships
                entity.HasOne(e => e.UserPhone)
                      .WithMany()
                      .HasForeignKey(e => e.UserPhoneId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure PhoneOverageDocument entity (Supporting Documents for Extension Overage)
            builder.Entity<PhoneOverageDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.UploadedBy).IsRequired().HasMaxLength(50);

                // Indexes
                entity.HasIndex(e => e.PhoneOverageJustificationId);

                // Relationships with CASCADE delete
                entity.HasOne(e => e.PhoneOverageJustification)
                      .WithMany(j => j.Documents)
                      .HasForeignKey(e => e.PhoneOverageJustificationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure RecoveryLog entity
            builder.Entity<RecoveryLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RecoveryType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RecoveryAction).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RecoveryReason).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.RecoveredFrom).HasMaxLength(100);
                entity.Property(e => e.ProcessedBy).HasMaxLength(100);

                // Indexes for performance
                entity.HasIndex(e => e.BatchId);
                entity.HasIndex(e => e.CallRecordId);
                entity.HasIndex(e => e.RecoveryDate);
                entity.HasIndex(e => e.RecoveredFrom);
                entity.HasIndex(e => e.RecoveryType);
                entity.HasIndex(e => e.RecoveryAction);
                entity.HasIndex(e => new { e.RecoveryDate, e.RecoveryType });
                entity.HasIndex(e => new { e.RecoveryDate, e.RecoveryAction });

                // Relationships
                entity.HasOne(e => e.CallRecord)
                      .WithMany()
                      .HasForeignKey(e => e.CallRecordId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.StagingBatch)
                      .WithMany()
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure DeadlineTracking entity
            builder.Entity<DeadlineTracking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DeadlineType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TargetEntity).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DeadlineStatus).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ExtensionReason).HasMaxLength(500);
                entity.Property(e => e.ExtensionApprovedBy).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(1000);

                // Indexes for performance
                entity.HasIndex(e => e.BatchId);
                entity.HasIndex(e => e.DeadlineDate);
                entity.HasIndex(e => e.TargetEntity);
                entity.HasIndex(e => e.DeadlineStatus);
                entity.HasIndex(e => new { e.DeadlineDate, e.DeadlineStatus });
                entity.HasIndex(e => new { e.DeadlineType, e.TargetEntity });

                // Relationships
                entity.HasOne(e => e.StagingBatch)
                      .WithMany()
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure RecoveryConfiguration entity
            builder.Entity<RecoveryConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RuleName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RuleType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                // Indexes
                entity.HasIndex(e => e.RuleName).IsUnique();
                entity.HasIndex(e => e.RuleType);
                entity.HasIndex(e => e.IsEnabled);
            });

            // Configure EmailConfiguration entity
            builder.Entity<EmailConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SmtpServer).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FromEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FromName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.Property(e => e.Password).HasMaxLength(500);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(500);

                // Indexes
                entity.HasIndex(e => e.IsActive);
            });

            // Configure EmailTemplate entity
            builder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.TemplateCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.HtmlBody).IsRequired();
                entity.Property(e => e.PlainTextBody);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.AvailablePlaceholders).HasMaxLength(2000);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                // Indexes
                entity.HasIndex(e => e.TemplateCode).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsSystemTemplate);
            });

            // Configure EmailLog entity
            builder.Entity<EmailLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ToEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CcEmails).HasMaxLength(1000);
                entity.Property(e => e.BccEmails).HasMaxLength(1000);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.PlainTextBody);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.TrackingId).HasMaxLength(100);
                entity.Property(e => e.RelatedEntityType).HasMaxLength(100);
                entity.Property(e => e.RelatedEntityId).HasMaxLength(100);

                // Indexes
                entity.HasIndex(e => e.ToEmail);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.SentDate);
                entity.HasIndex(e => e.TrackingId);
                entity.HasIndex(e => new { e.Status, e.CreatedDate });
                entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId });

                // Relationships
                entity.HasOne(e => e.EmailTemplate)
                      .WithMany(t => t.EmailLogs)
                      .HasForeignKey(e => e.EmailTemplateId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure EmailAttachment entity
            builder.Entity<EmailAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).HasMaxLength(100);

                // Indexes
                entity.HasIndex(e => e.EmailLogId);

                // Relationships
                entity.HasOne(e => e.EmailLog)
                      .WithMany()
                      .HasForeignKey(e => e.EmailLogId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed RefundRequests
            /*
            if (!context.RefundRequests.Any())
            {
                context.RefundRequests.AddRange(
                    new RefundRequest
                    {
                        FullName = "John Doe",
                        Email = "john.doe@example.com",
                        PhoneNumber = "1234567890",
                        Department = "IT Department",
                        DeviceType = "Smartphone",
                        DeviceModel = "iPhone 13",
                        SerialNumber = "ABC123456",
                        IMEINumber = "123456789012345",
                        RefundAmount = 800.00m,
                        RefundReason = "Device replacement due to damage",
                        AdditionalDetails = "Screen cracked after accidental drop",
                        Supervisor = "Jane Smith (jane.smith@example.com)",
                        Status = RefundRequestStatus.PendingSupervisor,
                        RequestDate = DateTime.UtcNow.AddDays(-5),
                        ProcessingNotes = "Initial review pending",
                        ApprovedAmount = null
                    },
                    new RefundRequest
                    {
                        FullName = "Alice Johnson",
                        Email = "alice.johnson@example.com",
                        PhoneNumber = "0987654321",
                        Department = "Finance Department",
                        DeviceType = "Tablet",
                        DeviceModel = "iPad Air",
                        SerialNumber = "XYZ789012",
                        IMEINumber = "987654321098765",
                        RefundAmount = 600.00m,
                        RefundReason = "Upgrade for work requirements",
                        AdditionalDetails = "Previous device no longer meets software requirements",
                        Supervisor = "Bob Wilson (bob.wilson@example.com)",
                        Status = RefundRequestStatus.PendingBudgetOfficer,
                        RequestDate = DateTime.UtcNow.AddDays(-3),
                        ProcessingNotes = "Approved by supervisor, pending budget review",
                        ApprovedAmount = 550.00m
                    }
                );
                context.SaveChanges();
            }
            */

            // Configure EbillUser to ApplicationUser relationship
            builder.Entity<EbillUser>(entity =>
            {
                entity.HasOne(e => e.ApplicationUser)
                    .WithOne(a => a.EbillUser)
                    .HasForeignKey<EbillUser>(e => e.ApplicationUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure ApplicationUser to EbillUser relationship
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(a => a.EbillUser)
                    .WithOne(e => e.ApplicationUser)
                    .HasForeignKey<ApplicationUser>(a => a.EbillUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Telecom Table Names (to match migration-created tables)
            builder.Entity<Safaricom>().ToTable("Safaricom");
            builder.Entity<Airtel>().ToTable("Airtel");
            builder.Entity<PSTN>().ToTable("PSTNs");
            builder.Entity<PrivateWire>().ToTable("PrivateWires");
        }
    }
} 