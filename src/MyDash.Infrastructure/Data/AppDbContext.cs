using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyDash.Domain.Entities;

namespace MyDash.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Server> Servers => Set<Server>();
    public DbSet<Domain.Entities.Service> Services => Set<Domain.Entities.Service>();
    public DbSet<EnrollmentToken> EnrollmentTokens => Set<EnrollmentToken>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<PinChallenge> PinChallenges => Set<PinChallenge>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Server>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Tags).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            e.HasMany(x => x.Services).WithOne(x => x.Server).HasForeignKey(x => x.ServerId);
        });

        modelBuilder.Entity<Domain.Entities.Service>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Tags).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        modelBuilder.Entity<EnrollmentToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Tags).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        modelBuilder.Entity<AuditEntry>(e => e.HasKey(x => x.Id));
        modelBuilder.Entity<PinChallenge>(e => e.HasKey(x => x.Id));

        modelBuilder.Entity<UserPreferences>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasData(new UserPreferences { });
        });
    }
}
