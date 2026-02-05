using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Infrastructure.Persistence
{
    public class ChatDBContext : DbContext, IChatDBContext
    {
        public ChatDBContext(DbContextOptions<ChatDBContext> options) : base(options) { }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<UploadedFile> UploadedFile { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatUser> ChatUsers { get; set; }
        public DbSet<LoginAttempt> Attempts { get; set; }
        public DbSet<ApiTokenModel> ApiTokenModel { get; set; }
        public DbSet<Client> Client { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<UserClient> UserClients { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<UserContract> UserContracts { get; set; }
        public DbSet<UserProject> UserProjects { get; set; }
        public DbSet<UserClientAssignment> UserClientAssignments { get; set; }
        public DbSet<TicketHistory> TicketHistories { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }
        public DbSet<TicketMessage> TicketMessages { get; set; }
        public DbSet<TicketInteraction> TicketInteractions { get; set; }
        public DbSet<TicketRating> TicketRatings { get; set; }
        public DbSet<TicketUpload> TicketUploads { get; set; }
        public DbSet<EmailOtp> EmailOtp { get; set; }
        public DbSet<Applicant> Applicants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserAccount>(entity =>
            {
                // ✅ Optional relationship
                entity.HasOne(u => u.Client)
                      .WithMany()
                      .HasForeignKey(u => u.ClientId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

                entity.Property(u => u.ClientId)
                      .IsRequired(false);
            });
            #region TicketMessage
            modelBuilder.Entity<TicketMessage>(b =>
            {
                b.Property(tm => tm.DateCreated)
                 .HasDefaultValueSql("getutcdate()")
                 .IsRequired(false);
                // ✅ Composite index: (TicketId, DateCreated)
                b.HasIndex(tm => new { tm.TicketId, tm.DateCreated })
                 .HasDatabaseName("IX_TicketMessages_TicketId_DateCreated");
            });
            #endregion
            #region ChatMessage
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .HasPrincipalKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .HasPrincipalKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ⚠ ConversationId nullable to prevent FK conflict
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ChatMessages_ChatConversations");

            #endregion
            #region ChatParticipants
            modelBuilder.Entity<ChatConversation>()
                .HasMany(c => c.Participants)
                .WithOne(p => p.Conversation)
                .HasForeignKey(p => p.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatParticipant>()
                .HasKey(p => p.Id);
            #endregion

            #region Ticket
            modelBuilder.Entity<Ticket>(b =>
            {
                b.HasKey(t => t.Id);

                // Computed TicketNo
                b.Property(t => t.TicketNo)
                 .HasComputedColumnSql(
                     "'Ticket-' + CAST([Id] AS varchar(10)) + " +
                     "RIGHT('0' + CAST(MONTH([DateReceived]) AS varchar(2)), 2) + " +
                     "RIGHT('0' + CAST(DAY([DateReceived]) AS varchar(2)), 2) + " +
                     "RIGHT(CAST(YEAR([DateReceived]) AS varchar(4)), 2)",
                     stored: true);

                // Computed CaseNo
                b.Property(t => t.CaseNo)
                 .HasComputedColumnSql(
                     "'Case-' + CAST([Id] AS varchar(10)) + " +
                     "RIGHT('0' + CAST(MONTH([DateReceived]) AS varchar(2)), 2) + " +
                     "RIGHT('0' + CAST(DAY([DateReceived]) AS varchar(2)), 2) + " +
                     "RIGHT(CAST(YEAR([DateReceived]) AS varchar(4)), 2)",
                     stored: true);

                b.HasIndex(t => t.CaseNo).IsUnique();

                b.Property(t => t.DateReceived)
                 .HasDefaultValueSql("getutcdate()")
                 .IsRequired();

                b.Property(t => t.IssueTitle)
                 .HasMaxLength(250);

                b.Property(t => t.DueDate).IsRequired(false);
                b.Property(t => t.ResolvedDate).IsRequired(false);
                b.Property(t => t.IsBreached).HasDefaultValue(false);

                // Relationships
                b.HasOne(t => t.Project)
                 .WithMany()
                 .HasForeignKey(t => t.ProjectId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(t => t.RequestedBy)
                 .WithMany()
                 .HasForeignKey(t => t.RequestedById)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(t => t.Client)
                 .WithMany()
                 .HasForeignKey(t => t.ClientId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(t => t.SupportedBy)
                 .WithMany()
                 .HasForeignKey(t => t.SupportedById)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict);

                // Messages navigation
                b.HasMany(t => t.Messages)
                 .WithOne(m => m.Ticket)
                 .HasForeignKey(m => m.TicketId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
            #endregion

            #region Project
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.Property(e => e.DeleteFlag).HasDefaultValue(false);
                entity.Property(e => e.DateCreated).HasDefaultValueSql("getutcdate()");

                // Project belongs to Team
                entity.HasOne(p => p.Team)
                      .WithMany(t => t.Projects)
                      .HasForeignKey(p => p.TeamId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Project belongs to Client
                entity.HasOne(p => p.Client)
                      .WithMany()
                      .HasForeignKey(p => p.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Project belongs to Contract
                entity.HasOne(p => p.Contract)
                      .WithMany(c => c.Projects)
                      .HasForeignKey(p => p.ContractId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            #endregion

            #region Team
            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TeamName).HasMaxLength(200);
            });
            #endregion

            #region UserContract (many-to-many)
            modelBuilder.Entity<UserContract>()
                .HasKey(uc => new { uc.UserAccountId, uc.ContractId });

            modelBuilder.Entity<UserContract>()
                .HasOne(uc => uc.UserAccount)
                .WithMany(u => u.UserContracts)
                .HasForeignKey(uc => uc.UserAccountId);

            modelBuilder.Entity<UserContract>()
                .HasOne(uc => uc.Contract)
                .WithMany(c => c.UserContracts)
                .HasForeignKey(uc => uc.ContractId);
            #endregion

            #region UserProject (many-to-many)
            modelBuilder.Entity<UserProject>()
                .HasKey(up => new { up.UserAccountId, up.ProjectId });

            modelBuilder.Entity<UserProject>()
                .HasOne(up => up.UserAccount)
                .WithMany(u => u.UserProjects)
                .HasForeignKey(up => up.UserAccountId);

            modelBuilder.Entity<UserProject>()
                .HasOne(up => up.Project)
                .WithMany(p => p.UserProjects)
                .HasForeignKey(up => up.ProjectId);
            #endregion
            modelBuilder.Entity<Project>()
             .HasOne(p => p.Contract)
             .WithMany(c => c.Projects)
             .HasForeignKey(p => p.ContractId)
             .IsRequired(false)   // allows null
             .OnDelete(DeleteBehavior.Restrict);
            #region TicketUpload ✔ FIXED
            modelBuilder.Entity<TicketUpload>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasOne(x => x.Ticket)
                 .WithMany(t => t.TicketUploads)
                 .HasForeignKey(x => x.TicketId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.TicketMessage)
                 .WithMany(m => m.TicketUploads)
                 .HasForeignKey(x => x.TicketMessageId)
                 .IsRequired(false) // allow null
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.UploadedBy)
                 .WithMany()
                 .HasForeignKey(x => x.UploadedById)
                 .OnDelete(DeleteBehavior.Restrict);

                b.Property(x => x.Base64Content)
                 .HasColumnType("nvarchar(max)"); // prevent truncation
            });
            #endregion
            modelBuilder.Entity<UserProject>()
       .HasKey(up => up.Id);

            // Optional: configure relationships
            modelBuilder.Entity<UserProject>()
                .HasOne(up => up.UserAccount)
                .WithMany(u => u.UserProjects)
                .HasForeignKey(up => up.UserAccountId);

            modelBuilder.Entity<UserProject>()
                .HasOne(up => up.Project)
                .WithMany(p => p.UserProjects)
                .HasForeignKey(up => up.ProjectId);
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Ticket)
                .WithMany(t => t.History) // if you have collection in Ticket
                .HasForeignKey(th => th.TicketId)
                .IsRequired(); // or .IsRequired(false) if optional
            modelBuilder.Entity<Applicant>(entity =>
            {
                entity.Property(x => x.ApplicantName)
                      .HasMaxLength(150);

                entity.Property(x => x.Email)
                      .HasMaxLength(150);

                entity.Property(x => x.Status)
                      .HasMaxLength(50);

                // ✅ GLOBAL SOFT DELETE FILTER
                entity.HasQueryFilter(x => !x.IsDeleted);
            });
            modelBuilder.Entity<LoginHistory>()
    .HasIndex(x => x.UserId);

            modelBuilder.Entity<LoginHistory>()
                .HasIndex(x => x.IpAddress);

        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
