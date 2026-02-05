using ChatUp.Domain.Entities;
using ChatUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Common.Interfaces;

public interface IChatDBContext
{
    DbSet<UserAccount> UserAccounts { get; }
    DbSet<UploadedFile> UploadedFile { get; }
    DbSet<ChatMessage> ChatMessages { get; set; }
    DbSet<ChatUser> ChatUsers { get; set; }
    DbSet<LoginAttempt> Attempts { get; set; }
    DbSet<ApiTokenModel> ApiTokenModel { get; set; }
    DbSet<Client> Client { get; set; }
    DbSet<Ticket> Tickets { get; set; }
    DbSet<Project> Projects { get; set; }
    DbSet<Team> Teams { get; set; }
    DbSet<LoginHistory> LoginHistories { get; set; }
    DbSet<Contract> Contracts { get; set; }
    DbSet<UserContract> UserContracts { get; set; }
    DbSet<UserClient> UserClients { get; set; }
    DbSet<UserProject> UserProjects { get; set; }
    DbSet<UserClientAssignment> UserClientAssignments { get; set; }
    DbSet<TicketHistory> TicketHistories { get; set; }
    DbSet<ChatConversation> ChatConversations { get; set; }
    DbSet<ChatParticipant> ChatParticipants { get; set; }
    DbSet<TicketMessage> TicketMessages { get; set; }
    DbSet<TicketInteraction> TicketInteractions { get; set; }
    DbSet<TicketRating> TicketRatings { get; set; }
    DbSet<TicketUpload> TicketUploads { get; set; }
    DbSet<EmailOtp> EmailOtp { get; set; }
    DbSet<Applicant> Applicants { get; set; }
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
