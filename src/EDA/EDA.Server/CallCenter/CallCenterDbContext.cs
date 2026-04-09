using Microsoft.EntityFrameworkCore;

namespace EDA.Server.CallCenter;

public sealed class CallCenterDbContext(DbContextOptions<CallCenterDbContext> options) : DbContext(options)
{
    public DbSet<CallSession> CallSessions => Set<CallSession>();
    public DbSet<TranscriptSegment> TranscriptSegments => Set<TranscriptSegment>();
    public DbSet<AgentDashboardProjection> AgentDashboards => Set<AgentDashboardProjection>();
    public DbSet<SuggestionEntry> Suggestions => Set<SuggestionEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ChatMessageProjection> ChatMessages => Set<ChatMessageProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CallSession>()
            .HasKey(call => call.Id);

        modelBuilder.Entity<TranscriptSegment>()
            .HasKey(segment => segment.Id);

        modelBuilder.Entity<TranscriptSegment>()
            .HasIndex(segment => new { segment.CallId, segment.ReceivedAt });

        modelBuilder.Entity<AgentDashboardProjection>()
            .HasKey(projection => projection.CallId);

        modelBuilder.Entity<SuggestionEntry>()
            .HasKey(entry => entry.Id);

        modelBuilder.Entity<SuggestionEntry>()
            .HasIndex(entry => new { entry.CallId, entry.CreatedAt });

        modelBuilder.Entity<OutboxMessage>()
            .HasKey(message => message.Id);

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(message => new { message.Status, message.OccurredAt });
        modelBuilder.Entity<ChatMessageProjection>()
            .HasKey(message => message.Id);

        modelBuilder.Entity<ChatMessageProjection>()
            .HasIndex(message => new { message.CallId, message.ReceivedAt });
    }
}
