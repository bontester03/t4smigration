using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApit4s.Identity;
using WebApit4s.Models;
namespace WebApit4s.DAL
{
    public class TimeContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public TimeContext(DbContextOptions<TimeContext> options)
            : base(options)
        {
        }

        public DbSet<PersonalDetails> PersonalDetails { get; set; }
        public DbSet<QuestionAnswer> QuestionAnswers { get; set; }
        public DbSet<HealthScore> HealthScores { get; set; }
        public DbSet<WeeklyMeasurements> WeeklyMeasurements { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Child> Children { get; set; }
        public DbSet<ReferralType> ReferralTypes { get; set; }
        public DbSet<Schools> Schools { get; set; }
        public DbSet<Classes> Classes { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<AdminNote> AdminNotes { get; set; }
        public DbSet<ConsentQuestion> ConsentQuestions { get; set; }
        public DbSet<ConsentAnswer> ConsentAnswers { get; set; }
        public DbSet<RegistrationReminder> RegistrationReminders { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<KpiReport> kpiReports { get; set; }
        public DbSet<GameTask> GameTasks { get; set; }
        public DbSet<ChildGameTask> ChildGameTasks { get; set; }
        public DbSet<CustomGoal> CustomGoals { get; set; }
        public DbSet<UserPointHistory> UserPointHistories { get; set; }
        public DbSet<GuestRegistrationLink> GuestRegistrationLinks { get; set; }
        public DbSet<ParentReward> ParentRewards { get; set; }
        public DbSet<ParentRewardRedemption> ParentRewardRedemptions { get; set; }
        public DbSet<VideoReward> VideoRewards { get; set; }
        public DbSet<VideoWatchLog> VideoWatchLogs { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserRefreshToken>(e =>
            {
                e.HasIndex(x => new { x.UserId, x.DeviceId });
                e.HasIndex(x => x.TokenHash).IsUnique();
                e.Property(x => x.TokenHash).IsRequired();
                e.Property(x => x.ExpiresUtc).IsRequired();
            });

            modelBuilder.Entity<GameTask>(e =>
            {
                e.Property(g => g.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                e.HasOne(g => g.CreatedByUser)
                 .WithMany(u => u.CreatedGameTasks)
                 .HasForeignKey(g => g.CreatedByUserId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CustomGoal>()
                .HasOne(g => g.AssignedBy)
                .WithMany()
                .HasForeignKey(g => g.AssignedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomGoal>()
                .HasOne(g => g.AssignedToChild)
                .WithMany()
                .HasForeignKey(g => g.AssignedToChildId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChildGameTask>()
                .HasOne(cgt => cgt.Child)
                .WithMany()
                .HasForeignKey(cgt => cgt.ChildId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChildGameTask>()
                .HasOne(cgt => cgt.GameTask)
                .WithMany()
                .HasForeignKey(cgt => cgt.GameTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserPointHistory>()
                .HasOne(uph => uph.Child)
                .WithMany()
                .HasForeignKey(uph => uph.ChildId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserPointHistory>()
                .HasOne(uph => uph.User)
                .WithMany()
                .HasForeignKey(uph => uph.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            NormalizeDateTimesToUtc();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override int SaveChanges()
        {
            NormalizeDateTimesToUtc();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            NormalizeDateTimesToUtc();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeDateTimesToUtc();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void NormalizeDateTimesToUtc()
        {
            foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime) && property.CurrentValue is DateTime dateTime)
                    {
                        property.CurrentValue = global::WebApit4s.Utilities.DateTimeUtils.EnsureUtc(dateTime);
                        continue;
                    }

                    if (property.Metadata.ClrType == typeof(DateTime?) && property.CurrentValue is DateTime nullableDateTime)
                    {
                        property.CurrentValue = global::WebApit4s.Utilities.DateTimeUtils.EnsureUtc(nullableDateTime);
                    }
                }
            }
        }
    }
}
