using Microsoft.EntityFrameworkCore;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Document Management
    public DbSet<DocumentNode> DocumentNodes { get; set; } = null!;
    public DbSet<NodePath> NodePaths { get; set; } = null!;

    // Project Management
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<TaskItem> TaskItems { get; set; } = null!;
    public DbSet<TaskComment> TaskComments { get; set; } = null!;
    public DbSet<TaskAttachment> TaskAttachments { get; set; } = null!;
    public DbSet<TaskDependency> TaskDependencies { get; set; } = null!;
    public DbSet<TaskLabel> TaskLabels { get; set; } = null!;
    public DbSet<TaskItemLabel> TaskItemLabels { get; set; } = null!;
    public DbSet<TimeEntry> TimeEntries { get; set; } = null!;
    
    // Notifications
    public DbSet<Notification> Notifications { get; set; } = null!;
    
    // Message Queue (Private Event Bus)
    public DbSet<MessageQueue> MessageQueues { get; set; } = null!;
    public DbSet<DeadLetterMessage> DeadLetterMessages { get; set; } = null!;
    
    // Monitoring
    public DbSet<SystemLog> SystemLogs { get; set; } = null!;
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; } = null!;
    public DbSet<MonitoringConfig> MonitoringConfigs { get; set; } = null!;

    // Email
    public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;
    public DbSet<EmailLog> EmailLogs { get; set; } = null!;
    public DbSet<UserEmailPreference> UserEmailPreferences { get; set; } = null!;

    // Users
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserProjectRole> UserProjectRoles { get; set; } = null!;

    // Storage
    public DbSet<StorageSettingsEntity> StorageSettings { get; set; } = null!;
    public DbSet<StoredFileEntity> StoredFiles { get; set; } = null!;

    // Sync
    public DbSet<SyncQueue> SyncQueues { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DocumentNode Configuration
        modelBuilder.Entity<DocumentNode>(entity =>
        {
            entity.HasKey(e => e.NodeId);
            entity.Property(e => e.NodeType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            
            // Unique constraint: same parent, same code
            entity.HasIndex(e => new { e.ParentNodeId, e.EntityCode }).IsUnique();
            
            // Self-referencing relationship
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentNodeId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.EntityCode);
            entity.HasIndex(e => e.MaterializedPath);
        });

        // NodePath Configuration (Closure Table)
        modelBuilder.Entity<NodePath>(entity =>
        {
            entity.HasKey(e => new { e.AncestorId, e.DescendantId });
            
            entity.HasOne(e => e.Ancestor)
                .WithMany(e => e.Descendants)
                .HasForeignKey(e => e.AncestorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Descendant)
                .WithMany(e => e.Ancestors)
                .HasForeignKey(e => e.DescendantId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.DescendantId);
        });

        // Project Configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.ProjectCode).IsUnique();
        });

        // TaskItem Configuration
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.TaskId);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Priority).HasConversion<string>();
            
            // Self-referencing for sub-tasks
            entity.HasOne(e => e.ParentTask)
                .WithMany(e => e.SubTasks)
                .HasForeignKey(e => e.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TaskDependency Configuration
        modelBuilder.Entity<TaskDependency>(entity =>
        {
            entity.HasKey(e => e.DependencyId);
            entity.Property(e => e.Type).HasConversion<string>();
            
            // Task -> Dependencies (this task depends on others)
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // DependsOnTask -> Dependents (other tasks depend on this)
            entity.HasOne(e => e.DependsOnTask)
                .WithMany()
                .HasForeignKey(e => e.DependsOnTaskId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete cycles
            
            // Prevent duplicate dependencies
            entity.HasIndex(e => new { e.TaskId, e.DependsOnTaskId }).IsUnique();
            
            // Query performance
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.DependsOnTaskId);
        });

        // TaskLabel Configuration
        modelBuilder.Entity<TaskLabel>(entity =>
        {
            entity.HasKey(e => e.LabelId);
            
            // Unique: Same project cannot have duplicate label names
            entity.HasIndex(e => new { e.ProjectId, e.Name }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationCode, e.Name });
            
            // Query performance
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.OrganizationCode);
            entity.HasIndex(e => e.IsActive);
            
            // Foreign key
            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.SetNull); // Keep labels when project deleted
        });

        // TaskItemLabel Configuration (Many-to-Many)
        modelBuilder.Entity<TaskItemLabel>(entity =>
        {
            entity.HasKey(e => new { e.TaskId, e.LabelId });
            
            // Foreign keys
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Label)
                .WithMany(e => e.TaskLabels)
                .HasForeignKey(e => e.LabelId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Query performance
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.LabelId);
        });

        // TimeEntry Configuration
        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.HasKey(e => e.TimeEntryId);
            entity.Property(e => e.WorkType).HasConversion<string>();
            
            // Foreign keys
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for common queries
            entity.HasIndex(e => new { e.UserId, e.StartTime }); // User's time entries
            entity.HasIndex(e => new { e.TaskId, e.StartTime }); // Task's time entries
            entity.HasIndex(e => new { e.UserId, e.EndTime }); // Running timers
            entity.HasIndex(e => new { e.StartTime, e.EndTime }); // Date range queries
            entity.HasIndex(e => e.IsBillable); // Billable reports
            entity.HasIndex(e => e.IsApproved); // Approval workflow
            entity.HasIndex(e => e.CreatedAt);
        });

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.OrganizationCode);
            
            // Authentication type
            entity.Property(e => e.AuthenticationType).HasConversion<string>();
            entity.Property(e => e.Role).HasConversion<string>();
            
            // Indexes for auth queries
            entity.HasIndex(e => new { e.Email, e.IsActive });
            entity.HasIndex(e => new { e.AuthenticationType, e.OrganizationCode });
            entity.HasIndex(e => e.RecoveryEmail);
            entity.HasIndex(e => e.ActiveDirectorySid);
            
            // For AD users, Email can be null but RecoveryEmail might be set
            entity.HasIndex(e => new { e.Username, e.AuthenticationType }).IsUnique();
        });

        modelBuilder.Entity<UserProjectRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ProjectId });
            entity.Property(e => e.Role).HasConversion<string>();
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.ProjectRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StorageSettings Configuration
        modelBuilder.Entity<StorageSettingsEntity>(entity =>
        {
            entity.HasKey(e => e.StorageId);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.HasIndex(e => e.IsDefault);
            entity.HasIndex(e => new { e.IsActive, e.Type });
        });

        // StoredFile Configuration
        modelBuilder.Entity<StoredFileEntity>(entity =>
        {
            entity.HasKey(e => e.FileId);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.StorageId);
            entity.HasIndex(e => e.IsDeleted);
            
            entity.HasOne<StorageSettingsEntity>()
                .WithMany()
                .HasForeignKey(e => e.StorageId);
        });

        // SyncQueue Configuration
        modelBuilder.Entity<SyncQueue>(entity =>
        {
            entity.HasKey(e => e.QueueId);
            entity.Property(e => e.Operation).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => new { e.DeviceId, e.Status });
            entity.HasIndex(e => new { e.OrganizationCode, e.Status });
            entity.HasIndex(e => e.CreatedAt).HasFilter("[Status] = 'Pending'");
        });

        // Notification Configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Indexes
            entity.HasIndex(e => new { e.RecipientUserId, e.IsRead });
            entity.HasIndex(e => new { e.RecipientUserId, e.CreatedAt });
            entity.HasIndex(e => new { e.OrganizationCode, e.Type });
        });

        // MessageQueue Configuration
        modelBuilder.Entity<MessageQueue>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Indexes for performance
            entity.HasIndex(e => new { e.QueueName, e.Status });
            entity.HasIndex(e => new { e.Status, e.ScheduledFor });
            entity.HasIndex(e => new { e.Priority, e.CreatedAt });
            entity.HasIndex(e => e.CorrelationId);
        });

        // DeadLetterMessage Configuration
        modelBuilder.Entity<DeadLetterMessage>(entity =>
        {
            entity.HasKey(e => e.DeadLetterId);
            entity.Property(e => e.FailedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasIndex(e => e.QueueName);
            entity.HasIndex(e => e.FailedAt);
        });

        // SystemLog Configuration
        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.Level).HasConversion<string>();
            entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => new { e.Timestamp, e.Level });
        });

        // PerformanceMetric Configuration
        modelBuilder.Entity<PerformanceMetric>(entity =>
        {
            entity.HasKey(e => e.MetricId);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasIndex(e => new { e.MetricName, e.Timestamp });
        });

        // MonitoringConfig Configuration
        modelBuilder.Entity<MonitoringConfig>(entity =>
        {
            entity.HasKey(e => e.ConfigId);
            entity.Property(e => e.MinimumLogLevel).HasConversion<string>();
        });

        // EmailTemplate Configuration
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId);
            entity.HasIndex(e => e.TemplateCode).IsUnique();
            entity.HasIndex(e => new { e.TemplateCode, e.LanguageCode }).IsUnique();
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        // EmailLog Configuration
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasKey(e => e.EmailLogId);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.ToEmail);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.TrackingId);
            entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId });
        });

        // UserEmailPreference Configuration
        modelBuilder.Entity<UserEmailPreference>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<UserEmailPreference>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
