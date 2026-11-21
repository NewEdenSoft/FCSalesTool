using FCSalesTool.Domain.Accounts;
using FCSalesTool.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FCSalesTool.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<LinkedCharacter> LinkedCharacters => Set<LinkedCharacter>();

    /// <summary>ASP.NET Core data protection keys (auth cookies, email tokens) survive restarts.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(user =>
        {
            user.Property(u => u.DisplayName).HasMaxLength(DisplayName.MaxLength).IsRequired();

            // Identity's EmailIndex is not unique; EVE-only accounts have no email, so
            // RequireUniqueEmail can't be used. Enforce uniqueness here (NULLs are distinct).
            user.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();

            user.HasMany(u => u.Characters)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LinkedCharacter>(character =>
        {
            character.HasKey(c => c.CharacterId);
            character.Property(c => c.CharacterId).ValueGeneratedNever();
            character.Property(c => c.Name).HasMaxLength(64).IsRequired();
            character.Property(c => c.OwnerHash).HasMaxLength(128);
            character.HasIndex(c => c.UserId);
        });
    }
}
